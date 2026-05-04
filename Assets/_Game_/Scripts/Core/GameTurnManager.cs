using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Pathfinding;
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
	[SerializeField] [Range(0f,1f)] private float cameraStartZoom = .5f;
	[Space]
	[Header("Game Rules")]
	[SerializeField] private bool rollDiceWhenTurnStarts = true;
	[SerializeField] private bool rollDiceWhenTurnEnds = true;
	[SerializeField] private bool forceEnemyDetectParty = true;
	[Space]
	[Header("Timings")]
	[SerializeField] [Range(0.3f,3f)] private float realTimeTicks = 0.5f;
	[SerializeField] [Min(0.1f)] private float postLoadSceneDuration = 1.0f;
	[SerializeField] [Min(0.1f)] private float postLoadSaveDuration = 1.0f;
	[SerializeField] [Min(0.1f)] private float turnChangedSeconds = 3.0f;
	[SerializeField] [Min(0.1f)]  private  float focusCharacterSeconds = 1.5f;
	[SerializeField] [Min(0.1f)] private  float noActionsSeconds = 3.0f;
	[Space]
	[Header("Init")] 
	[SerializeField] private GameObject[] startupObjects = Array.Empty<GameObject>();

	public bool IsTurnBasedMode() => turnBasedMode;

	public bool PlayerCanSave() => !IsTurnBasedMode();
	
	public bool IsPlayerTurn() => playerTurn || !turnBasedMode;

	public bool AnyActiveRoutinesPlaying() => CharacterRoutines.AnyActiveRoutines();
	
	public bool PlayerCanEndTurn() => !AnyActiveRoutinesPlaying() && IsPlayerTurn();
	
	public bool PlayerShouldEndTurn() => PlayerCanEndTurn() 
	                               && !partyCharacters.Any(pc => !pc.IsDead() && pc.HasActions());
	
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

	public void TryEndPlayerTurn(bool checkShouldEnd)
	{
		if (!IsTurnBasedMode()) 
			return;
		
		if (IsPlayerTurn())
		{
			if (!checkShouldEnd || PlayerShouldEndTurn())
			{
				if(debugMode > 0) Debug.Log(this + " TryEndTurn");
				EndTurn();
			}
			else
			{
				if(debugMode > 0) Debug.Log(this + " TrySelectNext");
				CharacterSelection.SelectNextCharacter(true);
			}
		}
		else
		{
			if(debugMode > 0) Debug.Log(this + " TryEndPlayerTurn failed: not in player turn");
		}
	}

	public CharacterEntity[] GetLevelCharacters() => levelCharacters.ToArray();
	
	public bool IsLevelIndex(int index) => index == 0; // TODO

	public void SetWaitTime(float newTime)
	{
		waitTime = newTime;
	}

	public bool IsWaiting()
	{
		if (waitTime != 0f)
			return true;

		if (IsTurnBasedMode())
			return !IsPlayerTurn() || AnyActiveRoutinesPlaying();

		return false;
	}
	
	#region PRIVATE
	private CameraPivot cameraPivot => CameraPivot.Instance;
	private IEnumerable<CharacterEntity> partyCharacters => CharacterEntity.GetPartyCharacters();
	
	private CharacterEntity[] levelCharacters;
	private CharacterEntity currentAICharacter;
	private CharacterEntity focusCharacter;
	private int currentTurn;
	private bool playerTurn;
	private bool isLoading;
	private bool turnBasedMode;
	private bool leaveTurnBased;
	private bool nextFirstTurnIsPlayer;
	private float waitTime;
	
	private void Awake()
	{
		_instance = this;
		Debug.Log(this + " Awake");

		Utils.GetOrAddComponent<NavmeshRebuilder>(gameObject); // "isMainInstance"

		CharacterDetection.onAnyCharacterDetectionChanged += OnAnyDetectionChanged;
		CharacterEntity.onAnyCharacterDamaged += OnAnyCharacterDamaged;
		RoomTrigger.onChanged += OnRoomChanged;
	}

	private void OnDestroy()
	{
		CharacterDetection.onAnyCharacterDetectionChanged -= OnAnyDetectionChanged;
		CharacterEntity.onAnyCharacterDamaged -= OnAnyCharacterDamaged;
		RoomTrigger.onChanged -= OnRoomChanged;
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

			yield return new WaitForSeconds(postLoadSceneDuration);
		} while (SceneLoader.AnyInstanceActive);
		
		yield return WaitNavigationDirty();
		
		Debug.Log(" loading scene complete");
		
		RoomTrigger.activeRooms.ForEach(r => r.Refresh());
		yield return null; // for triggers

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

		if (GameSave.Current.ShouldLoad)
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
		
		yield return new WaitForSeconds(postLoadSaveDuration);
		
		Debug.Log("loading complete");
		isLoading = false;
		yield return null; // for graph events to init
		yield return null; // for graph events to init
		
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
				
				yield return YieldRoutines();
				yield return WaitNavigationDirty();
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
					
					yield return YieldRoutines();
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
				
				if (!otherCharacter.IsCharacterRevealed()) continue;
				
				yield return YieldRoutines();
				
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
				yield return YieldRoutines();
				yield return WaitNavigationDirty();
				
				if(debugMode > 0) Debug.Log(this + " start think " + otherBrain);
				do
				{
					if(!otherBrain.IsThinking)
						otherBrain.StartThink();
					
					yield return null;
				} while (!otherCharacter.IsDead() && otherBrain.IsThinking);
				
				otherCharacter.GetAgent().SetObstacle(true);
				yield return YieldRoutines();
				yield return WaitNavigationDirty();
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
		onPlayerChanged?.Invoke(true);
		playerTurn = false;
		
		foreach (var partyChar in partyCharacters)
		{
			if (partyChar.TryGetAnimation(out var anim))
				anim.SwitchWeapons(false);
		}
		
		PartyRest();
		SelectBestPlayerCharacter();
		CameraPivot.Instance.SetZoom(cameraStartZoom);

		do
		{
			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				if (otherCharacter.Routines.AnyActive()) continue;
				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain)) continue;
				
				if (!otherBrain.IsThinking)
					otherBrain.StartThink();
			}

			yield return new WaitForSeconds(realTimeTicks);
			yield return YieldRoutines();

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
			if(debugMode > 1) Debug.Log(this + " WaitRoutines WaitTime=" + waitTime.ToString("N2") + " " + CharacterRoutines.activeInstances.Count + "\n>" + Utils.ArrayStr(CharacterRoutines.activeInstances));
			
			yield return null;

			if (waitTime > 0)
				waitTime = Mathf.Max(waitTime - Time.deltaTime, 0f);
		}
		while ( (turnBasedMode && CharacterRoutines.AnyActiveRoutines()) || waitTime != 0f);
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

	private void OnRoomChanged()
	{
		
	}

	private void EndTurn()
	{
		if(debugMode > 0) Debug.Log(this + " EndTurn playerTurn=" + playerTurn);
		onTurnEnded?.Invoke();
		playerTurn = false;
		onPlayerChanged?.Invoke(playerTurn); 
		onTurnStarted?.Invoke(); // start other turn
		if(rollDiceWhenTurnEnds) DicePool.RollNewDice();
	}
	
	private void NewTurn()
	{
		if(debugMode > 0) Debug.Log(this + " EndTurn playerTurn=" + playerTurn);
		onTurnEnded?.Invoke();
		PartyRest();
		
		currentTurn += 1;
		playerTurn = true;
		onTurnStarted?.Invoke();
		if(debugMode > 0) Debug.Log(this + " NewTurn " + currentTurn);
		if(rollDiceWhenTurnStarts) DicePool.RollNewDice();
	}

	private void FirstTurn()
	{
		currentTurn = 1;
		playerTurn = nextFirstTurnIsPlayer;
		nextFirstTurnIsPlayer = false;
		if(debugMode > 0) Debug.Log(this + " FirstTurn playerTurn=" + playerTurn);

		levelCharacters = levelCharacters.OrderBy((character) => (int)character.GetCurrentFaction()
		                                                         + "_" + (character.TryGetComponent(out CharacterDetection detection) ? detection.GetLastTimeDetectedInterval() : 0).ToString("N3")
		                                                         + "_" + character.name)
			.ToArray();
		
		foreach (var character in levelCharacters)
		{
			if (character.IsDead() || !character.IsCharacterRevealed())
				continue;
			
			character.Routines.StopAllRoutines();
			character.StopAgent();
			character.GetAgent().UpdateObstacle();
			
			if (!character.InParty())
			{
				if (character.TryGetComponent(out CharacterBrain otherBrain))
					otherBrain.StopThink();

				if (forceEnemyDetectParty && character.IsEnemyToPlayer())
				{
					if (character.TryGetComponent(out CharacterDetection otherDetection))
					{
						foreach (var partyChar in partyCharacters)
						{
							otherDetection.SetCharacterDetected(partyChar, true);
						}
					}
				}
			}

			if (character.InParty() || character.HasDetectedAnyPartyCharacter())
			{
				if (character.TryGetAnimation(out var anim))
				{
					anim.SwitchWeapons(true);
				}
			}
		}

		if(debugMode > 0) Debug.Log(this + " FirstTurn: " + playerTurn);
		
		onPlayerChanged?.Invoke(playerTurn);
		onTurnStarted?.Invoke();
	}

	private void SelectBestPlayerCharacter()
	{
		var center = cameraPivot.GetFocusPosition();

		var bestCharacter = partyCharacters
			.Where(pc => !pc.IsDead())
			.OrderBy(pc => Vector3.Distance(pc.GetPosition(), center))
			.FirstOrDefault();
		
		if(debugMode > 0) Debug.Log(this + " SelectBestPlayerCharacter " + bestCharacter);
		CharacterSelection.SelectCharacter(bestCharacter);
	}

	private void PartyRest()
	{
		foreach (var character in partyCharacters)
		{
			character.Rest(debugMode > 1);
		}
	}

	private bool ShouldExitTurnBased()
	{
		return turnBasedMode && !levelCharacters.Any(levelCharacter => !levelCharacter.IsDead()
		                                                              && levelCharacter.IsCharacterRevealed()
		                                                              && levelCharacter.IsEnemyToPlayer()
		                                                              && levelCharacter.HasDetectedAnyPartyCharacter());
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
			if (!turnBasedMode && !leaveTurnBased) // only if real time
			{
				if (!char1.IsDead() && char1.IsEnemyToPlayer() && char2.IsPlayer())
				{
					focusCharacter = char1;
					leaveTurnBased = true;
					if (debugMode > 0) Debug.Log(this + " OnAnyDetectionChanged " + char1 + " sees " + char2 + " leaveTurnBased");
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
				if (playerTurn) GUILayout.Label("   should end? " + PlayerShouldEndTurn());
			}
			if(IsWaiting()) GUILayout.Label("    wait: " + waitTime.ToString("N2"));
			if (turnBasedMode)
			{
				if (CharacterRoutines.AnyActiveRoutines())
				{
					foreach (var routine in CharacterRoutines.GetActiveRoutines())
					{
						GUILayout.Label("    ______" + routine.name);
					}
				}
				
				foreach (var character in levelCharacters)
				{
					if (!character.IsCharacterRevealed()) continue;
					
					var label = character.name;
					if (character.IsDead()) label += " (DEAD)";
					else label += " (" + character.GetCurrentHealth().ToString("N2") + ")";
					
					var isCurrent = (playerTurn && character.InParty()) ||
					                (!playerTurn && currentAICharacter == character);
						
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