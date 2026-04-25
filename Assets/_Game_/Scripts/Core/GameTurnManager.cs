using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameTurnManager : MonoBehaviour
{
	private static GameTurnManager _instance;
	public static GameTurnManager Instance => _instance;
	
	public static int debugMode { get; set; }

	public System.Action<bool> onLoadingGame, onPlayerChanged;
	public System.Action onSceneLoaded, onTurnStarted, onTurnEnded;
	
	[SerializeField] private string nextLevelName = "Level_0";
	[SerializeField] private bool firstTurnIsPlayer = true;
	[SerializeField] private bool skipInvisibleCharacters = true;
	[SerializeField] private bool restCharacterAtFirstTurn;
	[SerializeField] [Range(0f,1f)] private float cameraStartZoom = .5f;
	[Header("Timings")]
	[SerializeField] [Range(0.3f,3f)] private float realTimeTicks = 0.5f;
	[SerializeField] [Min(0.1f)] private float postLoadDuration = 1.0f;
	[SerializeField] [Min(0.1f)] private float turnChangedSeconds = 3.0f;
	[SerializeField] [Min(0.1f)]  private  float focusCharacterSeconds = 1.5f;
	[SerializeField] [Min(0.1f)] private  float noActionsSeconds = 3.0f;
	[SerializeField] [Min(0.1f)] private float defaultActionsSeconds = 1.0f;
	[SerializeField] [Min(0.1f)] private  float firstTurnSeconds = 1.0f;
	[SerializeField] [Min(0.1f)] private  float aiThinksSeconds = 0.5f;
	[Space]
	[Header("Init")] 
	[SerializeField] private GameObject[] startupObjects = new GameObject[0];

	public bool IsTurnBasedMode() => turnBasedMode;
	
	public bool IsPlayerTurn() => playerTurn;

	public bool AnyActiveRoutinesPlaying() => CharacterRoutines.AnyActiveRoutines();
	
	public bool PlayerCanEndTurn() => !AnyActiveRoutinesPlaying() && IsPlayerTurn();
	
	public bool PlayerShouldEndTurn() => PlayerCanEndTurn() 
	                               && partyCharacters.All(pc =>
									{
										return !pc.HasActions() && pc.GetRemainingMovePoints() < 0.5f;
									});
	
	public bool SelectedCharacterCanUseRunAction() =>
		!CharacterRoutines.AnyActiveRoutines() && IsPlayerTurn() 
		                                       && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
		                                       && selectedCharacter.CanUseAction(CharacterAction.Run);

	public bool SelectedCharacterIsControllable() => IsPlayerTurn() 
	                                                 && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
	                                                 && selectedCharacter.IsControllableByPlayer();
	
	public int GetCurrentTurn() => currentTurn;

	public bool IsLoading() => isLoading;

	public void TryEndPlayerTurn()
	{
		if (IsPlayerTurn())
		{
			EndTurn();
		}
		else
		{
			Debug.Log(this + " TryEndPlayerTurn failed: not in player turn");
		}
	}

	public CharacterController[] GetTurnBasedCharacters() => turnBasedCharacters.ToArray();
	
	#region PRIVATE
	private CameraPivot cameraPivot => CameraPivot.Instance;
	private IEnumerable<CharacterController> partyCharacters => CharacterController.GetPartyCharacters();
	
	private CharacterController[] levelCharacters;
	private CharacterController currentAICharacter;
	private CharacterController focusCharacter;
	private List<CharacterController> turnBasedCharacters = new List<CharacterController>();
	private int currentTurn;
	private bool playerTurn;
	private bool isLoading;
	private bool turnBasedMode;
	
	private void Awake()
	{
		_instance = this;
		Debug.Log(this + " Awake");

		CharacterDetection.onAnyCharacterDetectionChanged += OnAnyDetectionChanged;
	}

	private void OnDestroy()
	{
		CharacterDetection.onAnyCharacterDetectionChanged -= OnAnyDetectionChanged;
	}

	private IEnumerator Start()
	{
		Debug.Log(this + " Starts");

		foreach (var go in startupObjects)
		{
			Debug.Log(this + " Instantiate startupObjects " + go);
			Instantiate(go);
		}
		
		FadeScreen.FadeIn(true);
		
		var sceneLoader = gameObject.AddComponent<SceneLoader>(); // load level
		sceneLoader.SetSceneToLoad(nextLevelName, LoadSceneMode.Additive);

		isLoading = true;
		Debug.Log(this + " loading...");
		onLoadingGame?.Invoke(true);
		
		do // ensure that everything is properly loaded
		{
			do
			{
				yield return null;
			} while (SceneLoader.AnyInstanceActive);

			yield return new WaitForSeconds(postLoadDuration);
		} while (SceneLoader.AnyInstanceActive);

		Debug.Log(" wait NavmeshRebuilder(s)");
		do yield return null;	while (NavmeshRebuilder.IsRebuildingAsync()); // wait rebuild
		
		Debug.Log(" loading scene complete");
		
		yield return new WaitForSeconds(0.1f); // for triggers

		levelCharacters = CharacterController.AllCharacters
			.OrderBy(c => c.transform.GetSiblingIndex())
			.ToArray();
		
		Debug.Log(this + " found levelCharacters=" + levelCharacters.Length);
		
		var partyIndex = 0;
		foreach (var character in levelCharacters)
		{
			character.Spawn();
			if(character.GetCurrentFaction() == CharacterFaction.Player)
				character.SetPartyIndex(partyIndex++);
		}
		
		Debug.Log("loading complete");
		isLoading = false;
		onLoadingGame?.Invoke(false);
		onSceneLoaded?.Invoke();
		
		FadeScreen.FadeOut();
		yield return new WaitForSeconds(FadeScreen.FadeDuration / 2f);
		
		while (true)
		{
			yield return StartCoroutine(RealTimeCoroutine());
			yield return null;
			yield return StartCoroutine(TurnBasedCoroutine());
			yield return null; // safety for loop
		}
	}

	private IEnumerator TurnBasedCoroutine()
	{
		Debug.Log(this + " Enters TurnBasedCoroutine");
		turnBasedMode = true;
		
		currentTurn = 0;
		
		while (turnBasedMode)
		{
			if (currentTurn == 0) 
			{
				Debug.Log(this + " init first turn");
				
				turnBasedCharacters.Clear();
				foreach (var character in levelCharacters)
				{
					if (character.IsDead())
						continue;
			
					if (!character.InParty() && !character.HasDetectedAnyPartyCharacter())
						continue;
			
					turnBasedCharacters.Add(character);
					if(restCharacterAtFirstTurn) character.Rest();
			
					character.Routines.StopAllRoutines();
					character.StopAgent();
					character.GetAgent().SetObstacle(!character.IsSelected());
					character.GetAgent().EnableAvoidance(false);

					if (character.TryGetAnimation(out CharacterAnimation otherAnimation))
						otherAnimation.TrySwitchWeapons(true);

					if (!character.InParty())
					{
						if (character.TryGetComponent(out CharacterBrain otherBrain))
							otherBrain.StopThink();
					}

					if (character.IsEnemyToPlayer())
					{
						if (focusCharacter == null || cameraPivot.GetDistanceTo(focusCharacter.GetPosition()) >
						    cameraPivot.GetDistanceTo(character.GetPosition()))
						{
							focusCharacter = character;
						}
					}
				}

				Debug.Log(this + " found turnBasedCharacter=" + turnBasedCharacters.Count);
				
				if (!playerTurn && firstTurnIsPlayer) // probably useless unless GD ("initiative")
				{
					playerTurn = true;
					onPlayerChanged?.Invoke(true);
				}
			}
			
			NewTurn();
			
			if (currentTurn == 1) // it is the first turn
			{
				if (focusCharacter)
				{
					Debug.Log(this + " focusCharacter=" + focusCharacter);
					cameraPivot.Focus(focusCharacter);
					focusCharacter = null;
					yield return new WaitForSeconds(focusCharacterSeconds);
				}
				
				do
				{
					yield return null;
				} while (levelCharacters.Any(c => c.TryGetAnimation(out var anim) && anim.IsSwitchingWeapons));
				
				yield return new WaitForSeconds(firstTurnSeconds); 
			}
			else // it is NOT the first turn
			{
				SelectBestPlayerCharacter();
				yield return new WaitForSeconds(focusCharacterSeconds); 
			}
			
			while (IsPlayerTurn())
			{
				if (turnBasedMode && ShouldExitTurnBased())
				{
					turnBasedMode = false;
					Debug.Log(this + " Switch To RealTime");
					yield break;
				}
				
				yield return new WaitForSeconds(realTimeTicks);
			}

			CharacterSelection.SelectCharacter(null);
			
			yield return new WaitForSeconds(turnChangedSeconds);
			
			yield return YieldRoutines();
		
			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				
				currentAICharacter = otherCharacter;
				otherCharacter.Rest(); // rest not player character

				if (skipInvisibleCharacters && !otherCharacter.IsCharacterRevealed())
				{
					Debug.Log("skipInvisibleCharacters: " + otherCharacter);
					yield return new WaitForSeconds(defaultActionsSeconds);
					continue;
				}
				
				cameraPivot.Focus(otherCharacter);
				//TODO "cameraPivot.Lock(true)"
				yield return new WaitForSeconds(focusCharacterSeconds);
				//TODO "cameraPivot.Lock(false)" ? if player can look around while another character is acting

				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain))
				{
					Debug.LogWarning(otherCharacter + " has no brain");
					yield return new WaitForSeconds(noActionsSeconds);
					continue;
				}

				otherCharacter.GetAgent().SetObstacle(false);
				
				do yield return null;
				while (NavmeshRebuilder.IsRebuildingAsync());
				
				do
				{
					if(!otherBrain.IsThinking)
						otherBrain.StartThink();
					
					yield return null;
				} while (!otherCharacter.IsDead() && otherBrain.IsThinking);
				
				Debug.Log(this + " YieldRoutines");
				yield return YieldRoutines();

				/*var startTime = Time.time;
				yield return YieldRoutines();
				while (Time.time - startTime < aiThinksSeconds)
					yield return null;*/
			}
		}
	}

	private IEnumerator RealTimeCoroutine()
	{
		Debug.Log(this + " Enters RealTimeCoroutine");
		turnBasedMode = false;

		foreach (var character in levelCharacters)
		{
			character.GetAgent().EnableAvoidance(true);
		}
		
		currentTurn = -1;
		NewTurn();
		
		SelectBestPlayerCharacter();
		CameraPivot.Instance.SetZoom(cameraStartZoom);

		do
		{
			PartyRest(false);

			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				otherCharacter.Rest(false);
				if (otherCharacter.Routines.AnyActive()) continue;
				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain)) continue;
				
				if (!otherBrain.IsThinking)
					otherBrain.StartThink();
			}

			yield return new WaitForSeconds(realTimeTicks);

			if (!turnBasedMode && IsAnyEnemyAroundCheck())
			{
				turnBasedMode = true;
				Debug.Log(this + " Switch Turn Based Mode");
			}
			
		} while (!turnBasedMode);
	}
	
	private Coroutine YieldRoutines() => StartCoroutine(WaitRoutines());
	private IEnumerator WaitRoutines()
	{
		while(CharacterRoutines.AnyActiveRoutines())
			yield return null;
	}
	
	private void EndTurn()
	{
		playerTurn = false;
		onPlayerChanged?.Invoke(false);
		PartyRest(true);
		Debug.Log(this + " EndTurn " + currentTurn);
		onTurnEnded?.Invoke();
	}
	
	private void NewTurn()
	{
		currentTurn += 1;
		playerTurn = true;
		onPlayerChanged?.Invoke(true);
		
		Debug.Log(this + " NewTurn " + currentTurn);

		if (currentTurn == 0) // turn based
		{
			foreach (var partyChar in partyCharacters)
			{
				if (partyChar.TryGetAnimation(out var anim))
					anim.TrySwitchWeapons(false); // relax after fight
			}
		}
		
		else if (currentTurn > 0)
		{
			onTurnStarted?.Invoke();
		}
	}

	private void SelectBestPlayerCharacter()
	{
		var center = cameraPivot.GetFocusPosition();

		var bestCharacter = partyCharacters
			.OrderBy(pc => Vector3.Distance(pc.GetPosition(), center))
			.FirstOrDefault();
		
		Debug.Log(this + " SelectBestPlayerCharacter " + bestCharacter);
		CharacterSelection.SelectCharacter(bestCharacter);
	}

	private void PartyRest(bool verbose)
	{
		foreach (var character in partyCharacters)
			character.Rest(verbose);
	}

	private bool ShouldExitTurnBased()
	{
		foreach (var enemyCharacter in turnBasedCharacters)
		{
			if (enemyCharacter.InParty()) 
				continue;

			if (enemyCharacter.IsDead())
				continue;
			
			if (!enemyCharacter.IsEnemyToPlayer())
				continue;

			if (enemyCharacter.HasDetectedAnyPartyCharacter())
			{
//				if(debugMode > 0) Debug.Log(this + " Should_not_ExitTurnBased: HasDetectedAnyPartyCharacter enemy " + enemyCharacter);
				return false;
			}
		}

		return true;
	}

	private bool IsAnyEnemyAroundCheck()
	{
		foreach (var enemyCharacter in levelCharacters)
		{
			if (!enemyCharacter.IsEnemyToPlayer()) continue;
			if (!enemyCharacter.IsDead() && enemyCharacter.IsCharacterRevealed())
			{
				foreach (var partyCharacter in partyCharacters)
				{
					if (enemyCharacter.HasDetectedOther(partyCharacter))
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	private void OnAnyDetectionChanged(CharacterController char1, CharacterController char2, bool detected)
	{
		if (detected)
		{
			if (!turnBasedMode) // only if real time
			{
				if (!char1.IsDead() && char1.IsEnemyToPlayer() && char2.IsPlayer())
				{
					turnBasedMode = true;
					Debug.Log(this + " OnAnyDetectionChanged " + char1.name + " " + char2.name + " switch turn based");
				}
			}
		}
	}
	#endregion

	private void OnGUI()
	{
		if (debugMode > 0)
		{
			GUILayout.BeginArea(new Rect(16,16,256f,Screen.height));
			
			GUILayout.BeginVertical("textfield");
			
			GUILayout.Label("GAME TURN MANAGER");
			GUILayout.Label("levelCharacters: " + levelCharacters?.Length);
			
			GUILayout.BeginVertical("textfield");
			GUILayout.Label("turnBasedMode: " + turnBasedMode);
			if (turnBasedMode)
			{
				GUILayout.Label("isPlayerTurn: " + playerTurn);
				foreach (var character in turnBasedCharacters)
				{
					var isCurrent = (playerTurn && character.InParty()) ||
					                (!playerTurn && currentAICharacter == character);

					var label = character.name;
					if (character.IsDead()) label += " (DEAD)";
					else label += " (" + character.GetCurrentHealth().ToString("N2") + ")";
					
					GUILayout.Toggle(isCurrent, label);
				}
			}
			GUILayout.EndVertical();
			
			GUILayout.EndVertical();
			
			GUILayout.EndArea();
		}
	}

	public override string ToString() => "<b>GTM</b>";
}