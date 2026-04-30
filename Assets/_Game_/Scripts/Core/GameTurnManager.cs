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

	public event System.Action<bool> onLoadingGame, onPlayerChanged, onTurnBasedChanged;
	public event System.Action onSceneLoaded, onTurnStarted, onTurnEnded;
	
	[SerializeField] private string nextLevelName = "Level_0";
	[SerializeField] private bool skipInvisibleCharacters = true;
	[SerializeField] [Range(0f, 6f)] private float restInterval = 3.0f;
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

	public bool PlayerCanSave() => !IsTurnBasedMode();
	
	public bool IsPlayerTurn() => playerTurn || !turnBasedMode;

	public bool AnyActiveRoutinesPlaying() => CharacterRoutines.AnyActiveRoutines();
	
	public bool PlayerCanEndTurn() => !AnyActiveRoutinesPlaying() && IsPlayerTurn();
	
	public bool PlayerShouldEndTurn() => PlayerCanEndTurn() 
	                               && partyCharacters.All(pc => !pc.HasActions());
	
	public bool SelectedCharacterCanUseAction(CharacterAction action) =>
		!CharacterRoutines.AnyActiveRoutines() && IsPlayerTurn() 
		                                       && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
		                                       && selectedCharacter.CanUseAction(action);

	public bool SelectedCharacterIsControllable() => IsPlayerTurn() 
	                                                 && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
	                                                 && selectedCharacter.IsCurrentTurn()
	                                                 && selectedCharacter.IsControllableByPlayer();
	
	public int GetCurrentTurn() => currentTurn;

	public bool IsLoading() => isLoading;

	public void TryEndPlayerTurn()
	{
		if (!IsTurnBasedMode()) 
			return;
		
		if (IsPlayerTurn())
		{
			if (PlayerShouldEndTurn())
			{
				if(debugMode > 0) Debug.Log(this + " TryEndTurn");
				EndTurn();
			}
			else
			{
				if(debugMode > 0) Debug.Log(this + " TrySelectNext");
				
				int maxCount = CharacterEntity.CountLivingPartyCharacters();
				while(maxCount-- > 0)
				{
					CharacterSelection.SelectNextCharacter();
					
					if (CharacterSelection.TryGetSelectedCharacter(out var newSelected)
					    && !newSelected.IsDead()
					    && newSelected.HasActions())
					{
						break;
					}
				}
			}
		}
		else
		{
			if(debugMode > 0) Debug.Log(this + " TryEndPlayerTurn failed: not in player turn");
		}
	}

	public CharacterEntity[] GetTurnBasedCharacters() => turnBasedCharacters.ToArray();
	public CharacterEntity[] GetLevelCharacters() => levelCharacters.ToArray();
	public bool IsLevelIndex(int index) => index == 0; // TODO
	
	#region PRIVATE
	private CameraPivot cameraPivot => CameraPivot.Instance;
	private IEnumerable<CharacterEntity> partyCharacters => CharacterEntity.GetPartyCharacters();
	
	private CharacterEntity[] levelCharacters;
	private CharacterEntity currentAICharacter;
	private CharacterEntity focusCharacter;
	private List<CharacterEntity> turnBasedCharacters = new List<CharacterEntity>();
	private int currentTurn;
	private bool playerTurn;
	private bool isLoading;
	private bool turnBasedMode;
	private bool leaveTurnBased;
	private bool nextFirstTurnIsPlayer;
	
	private void Awake()
	{
		_instance = this;
		Debug.Log(this + " Awake");

		Utils.GetOrAddComponent<NavmeshRebuilder>(gameObject); // "isMainInstance"

		CharacterDetection.onAnyCharacterDetectionChanged += OnAnyDetectionChanged;
		CharacterEntity.onAnyCharacterDamaged += OnAnyCharacterDamaged;
	}

	private void OnDestroy()
	{
		CharacterDetection.onAnyCharacterDetectionChanged -= OnAnyDetectionChanged;
		CharacterEntity.onAnyCharacterDamaged -= OnAnyCharacterDamaged;
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

		yield return WaitNavigationDirty();
		
		Debug.Log(" loading scene complete");
		
		yield return new WaitForSeconds(0.1f); // for triggers

		levelCharacters = CharacterEntity.AllCharacters
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

		if (!GameSave.Current.IsEmpty())
		{
			Debug.Log(this + " Load game save version=" + GameSave.Current.version);
			GameSave.Current.LoadGame();
			yield return new WaitForSeconds(0.3f);
			
			if (CharacterSelection.TryGetSelectedCharacter(out var previouslySelected)) // TODO hak selection on load
			{
				foreach (var partyCharacter in CharacterEntity.GetPartyCharacters())
				{
					CharacterSelection.SelectCharacter(partyCharacter, true);
				}
				
				CharacterSelection.SelectCharacter(previouslySelected);
			}
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
		onTurnBasedChanged?.Invoke(true);
		
		currentTurn = 0;
		
		while (turnBasedMode)
		{
			if (currentTurn == 0) // it is the first turn
			{
				FirstTurn();

				if (focusCharacter)
				{
					cameraPivot.Focus(focusCharacter);
					focusCharacter = null;
					
					yield return new WaitForSeconds(focusCharacterSeconds);
				}
				
				do
				{
					yield return null;
				} while (levelCharacters.Any(c => c.TryGetAnimation(out var anim) && anim.IsSwitchingWeapons));
			}
			else // it is NOT the first turn
			{
				NewTurn();
			}

			if (IsPlayerTurn())
			{
				if(!CharacterSelection.TryGetSelectedCharacter(out var _))
					SelectBestPlayerCharacter();
				
				if(CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
					cameraPivot.Focus(selectedCharacter);
				
				yield return new WaitForSeconds(focusCharacterSeconds); 
				
				while (IsPlayerTurn())
				{
					if (turnBasedMode && ShouldExitTurnBased())
					{
						turnBasedMode = false;
						Debug.Log(this + " Switch To RealTime");
						yield break;
					}
					
					yield return new WaitForSeconds(realTimeTicks);
					yield return WaitNavigationDirty();
				}

				CharacterSelection.SelectCharacter(null);
				yield return new WaitForSeconds(turnChangedSeconds);
			}
			
			yield return WaitNavigationDirty();
			
			yield return YieldRoutines();
		
			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				
				otherCharacter.Rest(); // rest not player character

				if (skipInvisibleCharacters && !otherCharacter.IsCharacterRevealed())
				{
					Debug.Log("skipInvisibleCharacters: " + otherCharacter);
					yield return null;
					continue;
				}
				
				currentAICharacter = otherCharacter;
				
				cameraPivot.Focus(otherCharacter);
				yield return new WaitForSeconds(focusCharacterSeconds);

				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain))
				{
					Debug.LogWarning(otherCharacter + " has no brain");
					yield return new WaitForSeconds(noActionsSeconds);
					continue;
				}

				otherCharacter.GetAgent().SetObstacle(false);
				yield return WaitNavigationDirty();
				
				if(debugMode > 0) Debug.Log(this + " start think " + otherBrain);
				do
				{
					if(!otherBrain.IsThinking)
						otherBrain.StartThink();
					
					yield return null;
				} while (!otherCharacter.IsDead() && otherBrain.IsThinking);

				if(debugMode > 0) Debug.Log(this + " yield");
				yield return YieldRoutines();
				if(debugMode > 0) Debug.Log(this + " end yield");
			}
			
			yield return new WaitForSeconds(turnChangedSeconds);
		}
	}

	private IEnumerator RealTimeCoroutine()
	{
		Debug.Log(this + " Enters RealTimeCoroutine");
		onTurnBasedChanged?.Invoke(false);

		currentTurn = 0;
		leaveTurnBased = false;
		
		foreach (var partyChar in partyCharacters)
		{
			if (partyChar.TryGetAnimation(out var anim))
				anim.TrySwitchWeapons(false); // relax after fight
		}
		
		SelectBestPlayerCharacter();
		CameraPivot.Instance.SetZoom(cameraStartZoom);

		do
		{
			PartyRest(true);

			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				if (Time.time - otherCharacter.lastTimeRested >= restInterval) otherCharacter.Rest(debugMode > 1);
				if (otherCharacter.Routines.AnyActive()) continue;
				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain)) continue;
				
				if (!otherBrain.IsThinking)
					otherBrain.StartThink();
			}

			yield return new WaitForSeconds(realTimeTicks);

			if (!turnBasedMode && (leaveTurnBased || IsAnyEnemyAroundCheck()))
			{
				turnBasedMode = true;
				Debug.Log(this + " Switch Turn Based Mode");
			}
			
			yield return WaitNavigationDirty();
			
		} while (!turnBasedMode);
	}
	
	private Coroutine YieldRoutines() => StartCoroutine(WaitRoutines());
	
	private IEnumerator WaitRoutines()
	{
		do
		{
			yield return null;
			if(debugMode > 1) Debug.Log(this + " WaitRoutines " + CharacterRoutines.activeInstances.Count + "\n>" + Utils.ArrayStr(CharacterRoutines.activeInstances));
		}
		while (CharacterRoutines.AnyActiveRoutines()) ;
	}

	private Coroutine WaitNavigationDirty()
	{
		return StartCoroutine(DoWaitNavigationDirty());
	}

	private IEnumerator DoWaitNavigationDirty()
	{
		do
		{
			yield return null;
		} while (NavmeshRebuilder.IsNavigationDirty());
	}
	
	private void EndTurn()
	{
		playerTurn = false;
		onPlayerChanged?.Invoke(false);
		Debug.Log(this + " EndTurn " + currentTurn);
		onTurnEnded?.Invoke();
	}
	
	private void NewTurn()
	{
		if(currentTurn >= 1) 
			PartyRest(false);
		
		currentTurn += 1;

		playerTurn = true;
		onPlayerChanged?.Invoke(true);
		
		Debug.Log(this + " NewTurn " + currentTurn);
	}

	private void FirstTurn()
	{
		currentTurn = 1;
		playerTurn = nextFirstTurnIsPlayer;
		nextFirstTurnIsPlayer = false;
		if(debugMode > 0) Debug.Log(this + " FirstTurn playerTurn=" + playerTurn);
		onPlayerChanged?.Invoke(playerTurn);
		(playerTurn ? onTurnStarted : onTurnEnded)?.Invoke();
				
		turnBasedCharacters.Clear();
		foreach (var character in levelCharacters.OrderBy(c => (int)c.GetCurrentFaction()))
		{
			if (character.IsDead())
				continue;
			
			if (!character.InParty() && !character.HasDetectedAnyPartyCharacter())
				continue;
			
			turnBasedCharacters.Insert(0, character);
			
			if(!character.InParty())
				character.Routines.StopAllRoutines();
					
			character.StopAgent();
			character.GetAgent().UpdateObstacle();

			if (character.TryGetAnimation(out CharacterAnimation otherAnimation))
				otherAnimation.TrySwitchWeapons(true);

			if (!character.InParty())
			{
				if (character.TryGetComponent(out CharacterBrain otherBrain))
					otherBrain.StopThink();
			}
		}

		Debug.Log(this + " found turnBasedCharacter=" + turnBasedCharacters.Count);
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

	private void PartyRest(bool checkInterval)
	{
		foreach (var character in partyCharacters)
		{
			if(!checkInterval || Time.time - character.lastTimeRested > restInterval)
				character.Rest(debugMode > 1);
		}
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

	private void OnAnyCharacterDamaged(CharacterEntity damaged, DamageInfo dmgInfo, object objectFrom)
	{
		if (!turnBasedMode)
		{
			if (damaged.IsEnemyToPlayer())
			{
				if (objectFrom is CharacterEntity && (objectFrom as CharacterEntity).InParty())
				{
					nextFirstTurnIsPlayer = true;
				}
			}
			
			if(debugMode > 0) Debug.Log(this + " OnAnyCharacterDamaged " + damaged + " by " + objectFrom + " => next first turn is player: " + nextFirstTurnIsPlayer);
		}
	}
	
	private void OnAnyDetectionChanged(CharacterEntity char1, CharacterEntity char2, bool detected)
	{
		if (detected)
		{
			if (!turnBasedMode) // only if real time
			{
				if (!char1.IsDead() && char1.IsEnemyToPlayer() && char2.IsPlayer())
				{
					focusCharacter = char1;
					leaveTurnBased = true;
					Debug.Log(this + " OnAnyDetectionChanged " + char1.name + " " + char2.name + " leaveTurnBased");
				}
			}
		}
	}
	#endregion

	[Header("Debug")]
	[SerializeField] private Rect debugGUIRect;
	private void OnGUI()
	{
		if (debugMode > 0)
		{
			if (GameInput.IsConsoleVisible()) return;
			if (isLoading) return;

			GUILayout.BeginArea(Utils.RectScale(debugGUIRect, new Rect(Screen.width,Screen.height,Screen.width,Screen.height)));
			
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