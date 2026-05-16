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
	public event System.Action onSceneLoaded, onTurnStarted, onTurnEnded, onGameOver;
	
	[SerializeField] private string firstLevelName = "Level_0";
	[Header("Camera")]
	[SerializeField] [Range(0f,1f)] private float cameraStartZoom = .5f;
	[SerializeField] private bool focusWhenCharacterUseAction = true;
	[Space]
	[Header("Game Rules")]
	[SerializeField] private bool rollDiceWhenTurnStarts = true;
	[SerializeField] private bool rollDiceWhenTurnEnds = true;
	[SerializeField] private bool forceEnemyDetectParty = true;
	[SerializeField] private bool forceNeutralDetectEnemies = true;
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
	
	private CharacterEntity[] levelCharacters = Array.Empty<CharacterEntity>();
	private CharacterEntity currentAICharacter;
	private CharacterEntity focusCharacter;
	private LevelData currentLevelData;
	private LevelComponent currentLevel;
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
		CharacterEntity.onAnyCharacterUsesAction += OnAnyActionUsed;
		CharacterEntity.onAnyCharacterDead += OnAnyCharacterDead;
		RoomTrigger.onChanged += OnRoomChanged;
	}

	private void OnDestroy()
	{
		CharacterDetection.onAnyCharacterDetectionChanged -= OnAnyDetectionChanged;
		CharacterEntity.onAnyCharacterDamaged -= OnAnyCharacterDamaged;
		CharacterEntity.onAnyCharacterUsesAction -= OnAnyActionUsed;
		CharacterEntity.onAnyCharacterDead -= OnAnyCharacterDead;
		RoomTrigger.onChanged -= OnRoomChanged;
	}

	private IEnumerator LoadLevel(LevelData newLevel)
	{
		Debug.Log(this + " LoadLevel " + newLevel);
		
		if (currentLevelData)
		{
			foreach (var sceneToUnload in currentLevelData.scenesToLoad)
			{
				if (!newLevel.scenesToLoad.Contains(sceneToUnload) && SceneManager.GetSceneByName(sceneToUnload).isLoaded)
				{
					Debug.Log(this + " UnloadSceneAsync " + sceneToUnload);
					yield return SceneManager.UnloadSceneAsync(sceneToUnload);
				}
			}
		}

		currentLevelData = newLevel;
		
		if (currentLevelData)
		{
			foreach (var sceneToLoad in currentLevelData.scenesToLoad)
			{
				if (!SceneManager.GetSceneByName(sceneToLoad).isLoaded)
				{
					Debug.Log(this + " LoadSceneAsync " + sceneToLoad);
					yield return SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
				}
			}
		}
		
		InitLevel();
	}

	private IEnumerator Start()
	{
		foreach (var go in startupObjects)
		{
			Debug.Log(this + " Instantiate startupObjects " + go);
			Instantiate(go);
		}
		
		isLoading = true;
		DisableAudio(true);
		FadeScreen.FadeIn(true);

		if (!currentLevelData)
			currentLevelData = LevelData.GetAllLevels().FirstOrDefault();
		
		yield return StartCoroutine(LoadLevel(currentLevelData));
		
		do // ensure that everything is properly loaded
		{
			do
			{
				yield return null;
			} while (SceneLoader.AnyInstanceActive);

			yield return new WaitForSeconds(postLoadSceneDuration);
		} while (SceneLoader.AnyInstanceActive);
		
		Debug.Log(this + " first loading...");
		onLoadingGame?.Invoke(true);
		
		yield return WaitNavigationDirty();
		
		Debug.Log(" loading complete");
		
		RoomTrigger.activeRooms.ForEach(r => r.Refresh());
		yield return null; // for triggers

		yield return StartCoroutine(SpawnParty());

		if (GameSave.Current.ShouldLoad())
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
			
			yield return new WaitForSeconds(postLoadSaveDuration);
		}
		
		Debug.Log("loading complete");
		isLoading = false;
		yield return null; // for graph events to init
		
		onLoadingGame?.Invoke(false);
		onSceneLoaded?.Invoke();
		
		DisableAudio(false);
		FadeScreen.FadeOut();
		
		StartCoroutine("MainCoroutine");
	}

	private IEnumerator MainCoroutine()
	{
		while (true)
		{
			yield return StartCoroutine(RealTimeCoroutine());
			yield return null;
			yield return StartCoroutine(TurnBasedCoroutine());
			yield return null; // safety for loop
		}
	}
	
	private Coroutine DisableAudio(bool disabled)
	{
		return StartCoroutine(DoAudioTween(disabled ? 0f : 1f));
	}

	private IEnumerator DoAudioTween(float targetValue)
	{
		if (targetValue <= 0f)
		{
			AudioListener.volume = 0f;
			yield break;
		}

		while (AudioListener.volume < targetValue)
		{
			AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, targetValue, Time.deltaTime);
			yield return null;
		}
	}

	private IEnumerator SpawnParty()
	{
		var playerStart = PlayerStart.GetPlayerStarts().FirstOrDefault();
		yield return StartCoroutine(playerStart.SpawnParty());
	}
	
	private void InitLevel()
	{
		currentLevel = Utils.FindFirstObject<LevelComponent>();
		
		levelCharacters = CharacterEntity.AllCharacters
			.OrderBy(c => c.transform.GetSiblingIndex())
			.ToArray();
		
		Debug.Log(this + " init level " + currentLevel + " found levelCharacters=" + levelCharacters.Length);
		
		foreach (var character in levelCharacters)
		{
			character.Spawn();
		}
		
		currentLevel.Init(levelCharacters);
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
		
			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				if (!otherCharacter.IsCharacterRevealed()) continue;
				
				yield return YieldRoutines(); // for possible scripts
				
				currentAICharacter = otherCharacter;
				
				otherCharacter.Rest(); // rest not player character
				
				cameraPivot.Focus(otherCharacter);
				yield return new WaitForSeconds(focusCharacterSeconds);

				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain))
				{
					Debug.LogWarning(otherCharacter + " has no brain");
					yield return new WaitForSeconds(noActionsSeconds);
					continue;
				}
				
				yield return WaitNavigationDirty();
				
				if(debugMode > 0) Debug.Log(this + " start think " + otherBrain);
				otherBrain.StartThink();
				do
				{
					yield return null;
				} while (!otherCharacter.IsDead() && otherBrain.IsThinking);
				
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
		
		foreach (var anyChar in levelCharacters)
		{
			if (anyChar.TryGetAnimation(out var anim))
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
		
		foreach (var anyCharacter in levelCharacters)
		{
			if (anyCharacter.IsDead())
				continue;
			
			anyCharacter.Routines.StopAllRoutines();
			anyCharacter.StopAgent();
			
			if (anyCharacter.TryGetComponent(out CharacterBrain otherBrain))
				otherBrain.StopThink();

			if (!anyCharacter.IsCharacterRevealed())
				continue;
			
			if (!anyCharacter.InParty())
			{
				if (anyCharacter.IsEnemyToPlayer())
				{
					if (forceEnemyDetectParty)
					{
						if (anyCharacter.TryGetComponent(out CharacterDetection enemyDetection))
						{
							foreach (var partyChar in partyCharacters)
							{
								enemyDetection.SetCharacterDetected(partyChar, true);
							}
						}
					}
				}
				else
				{
					if (forceNeutralDetectEnemies)
					{
						if (anyCharacter.TryGetComponent(out CharacterDetection neutralDetection))
						{
							foreach (var partyChar in partyCharacters)
							{
								if (neutralDetection.IsCharacterDetected(partyChar))
								{
									foreach (var enemyChar in partyChar.GetLivingEnemyCharacters())
									{
										if(enemyChar.TryGetComponent(out CharacterDetection enemyDetection) && enemyDetection.IsCharacterDetected(partyChar))
											neutralDetection.SetCharacterDetected(enemyChar, true);
									}

									break;
								}
							}
						}
					}
				}
			}

			if (anyCharacter.InParty() || anyCharacter.HasDetectedAnyPartyCharacter())
			{
				if (anyCharacter.TryGetAnimation(out var anim))
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

	private void OnAnyActionUsed(CharacterEntity character, CharacterAction action)
	{
		if (IsTurnBasedMode() && focusWhenCharacterUseAction)
		{
			cameraPivot.Focus(character);
		}
	}

	private void OnAnyCharacterDead(CharacterEntity character)
	{
		if (character.InParty())
		{
			foreach (var partyChar in partyCharacters)
			{
				if (!partyChar.IsDead())
					return;
			}
			
			onGameOver?.Invoke();
		}
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
	[SerializeField] private KeyCode speedUpTimeKey;
	private void OnGUI()
	{
		if (Debug.isDebugBuild || Application.isEditor)
		{
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == speedUpTimeKey)
				Time.timeScale = Mathf.PI;
			else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == speedUpTimeKey)
				Time.timeScale = 1.0f;
		}
		
		if (debugMode > 0)
		{
			if (GameInput.IsConsoleVisible()) return;
			if (isLoading) return;

			GUILayout.BeginArea(Utils.RectScale(debugGUIRect, new Rect(Screen.width,Screen.height,Screen.width,Screen.height)));
			
			GUILayout.BeginVertical("textfield");
			
			GUILayout.Label("GAME TURN MANAGER");
			GUILayout.Label("levelCharacters: " + levelCharacters.Length);
			
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
					var label = character.name;
					
					var isCurrent = (playerTurn && character.InParty()) ||
					                (!playerTurn && currentAICharacter == character);
					
					if (debugMode > 1)
					{
						if (!character.IsCharacterRevealed())
						{
							label += " (HIDDEN)";
							isCurrent = false;
						}
					}
					else
					{
						if (!character.IsCharacterRevealed()) 
							continue;
					}
					
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
	public bool IsCurrentCharacter(CharacterEntity entity) => IsPlayerTurn() ? 
																currentAICharacter == CharacterSelection.TryGetSelectedCharacter(out var selected) && selected == entity && selected.IsControllableByPlayer()
																: currentAICharacter == entity;

	public override string ToString() => "<b>GTM</b>";
}