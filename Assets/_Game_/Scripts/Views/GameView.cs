using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class GameView : MonoBehaviour
{
	static private GameView instance;

	void Awake()
	{
		name = "GameView (Singleton)";
		instance = this;
		InitSingleton();
	}
	void OnDestroy(){}
	
	static public GameView GetInstance()
	{
		return instance;
	}
	
	public bool TryGetSelectedCharacter(out CharacterView view)
	{
		return (view = selectedCharacter) != null;
	}

	public System.Action<bool> routinesStarted;
	public System.Action<CharacterView, bool> selectionChanged;
	public System.Action<PropComponent, GameObject> propInteracted;
	
	static public string LAYER_GROUND = "Ground";
	static public string LAYER_WALL = "Wall";
	static public string LAYER_CHARACTER = "Character";
	public static string LAYER_IGNORE = "Ignore Raycast";
	
	public static bool TryGetNearestNavPosition(Vector3 position, float maxDistance, out Vector3 navPosition)
	{
		navPosition = position;

		if (AstarPath.active == null)
			return false;

		// Crée une contrainte pour nœuds walkables uniquement
		Pathfinding.NNConstraint constraint = Pathfinding.NNConstraint.Walkable;
		constraint.constrainWalkability = true;
		constraint.walkable = true;

		// Cherche le nœud le plus proche
		Pathfinding.NNInfo nearest = AstarPath.active.GetNearest(position, constraint);

		if (nearest.node != null && nearest.node.Walkable)
		{
			float distance = Vector3.Distance(position, (Vector3)nearest.position);
			if (distance <= maxDistance)
			{
				navPosition = (Vector3)nearest.position;
				return true;
			}
		}

		return false;
	}
	
	private List<CharacterView> allCharacters = new List<CharacterView>();
	private List<CharacterView> partyCharacters = new List<CharacterView>();

	public List<CharacterView> GetAllCharacters() => allCharacters.ToList();
	public List<CharacterView> GetPartyCharacters() => partyCharacters.ToList();
	public int GetPartyCharacterIndex(CharacterView characterView) => partyCharacters.IndexOf(characterView);
	public int GetCharacterIndex(CharacterView characterView) => allCharacters.IndexOf(characterView);
	

	public GameData gameData;
	public bool drawDebug = false;
	[Range(0f,1f)] public float debugGUI = 0f;
	public float cameraSpeed = 1.0f;
	[Range(0.01f, 0.50f)] public float maxNavDistance = 0.50f;

	public bool HasCoroutines() => coroutines.Count > 0;
	
	public bool mouseOverViewport { get; set; }
	
	public bool hasFocus { get; set; }
	
	private Vector2 mousePosition;
	private bool hasMousePosition;
	private AstarPath aStar;
	private MainCamera mainCamera;
	public InputSystem_Actions inputSystem { get; private set; }
	
	private CharacterView selectedCharacter;//TODO rename
	private List<IEnumerator> coroutines = new  List<IEnumerator>();

	public void MouseMove(Vector2 pos)
	{
		hasMousePosition = true;
		hasFocus = true;
		mousePosition = pos;
	}

	public void RightClick(float value)
	{
		if (value > 0) // pressed
		{
			SelectCharacter(); // deselect all
		}
	}

	public void LeftClick(float value)
	{
	//	Debug.Log("LeftClick: " + value);

		if (mouseOverViewport && value > 0f) // pressed
		{
			var raycastResult = CursorRaycast(out var hit);
			if (raycastResult == CURSOR_RAYCAST_RESULT_CHARACTER)
			{
				if (hit.collider.TryGetComponent(out CharacterView clickedCharacter)) // click character
				{
					if (CanSelectCharacter(clickedCharacter))
					{
						SelectCharacter(clickedCharacter);
					}
					else if (selectedCharacter != null && clickedCharacter != selectedCharacter)
					{
						Debug.Log("click other character: " + selectedCharacter + " > " + clickedCharacter);
						if (clickedCharacter.IsEnemy())
						{
							AssignAttack(selectedCharacter, clickedCharacter, false, true);
						}
					}
				}

				if (hit.collider.TryGetComponent(out PropComponent clickedProp)) // click prop
				{
					if (selectedCharacter != null && selectedCharacter.TryGetAgent(out var agent) && selectedCharacter.CanMove())
					{
						if (CanControlCharacter(selectedCharacter)) // assign interact
						{
							if (clickedProp.GetBestWaypoint(selectedCharacter.GetPosition(), out var bestWaypoint))
							{
								QueueRoutine(WaitCharacterMoved(selectedCharacter, bestWaypoint.GetPosition(), () =>
								{
									var dist = Vector3.Distance(selectedCharacter.GetPosition(), bestWaypoint.GetPosition());
									if (dist <= agent.reachDistance)
									{
										// waypoint reach
										if (selectedCharacter.CanInteract())
										{
											QueueRoutine(WaitCharacterInteract(selectedCharacter, clickedProp, bestWaypoint));
										}
									}
									else
									{
										Debug.Log(selectedCharacter + " not at reach");
									}
								}));
							}
						}
					}
				}
			}
			else if (raycastResult == CURSOR_RAYCAST_RESULT_NAVIGATION) // click navigation
			{
				if (selectedCharacter != null && selectedCharacter.TryGetAgent(out var agent) && selectedCharacter.CanMove())
				{
					if (CanControlCharacter(selectedCharacter)) // assign set nav destination (move)
					{
						QueueRoutine(WaitCharacterMoved(selectedCharacter, hit.point));
					}
				}
			}
		}
	}

	public void QueueRoutine(IEnumerator coroutine)
	{
		Debug.Log("QueueRoutine: " + coroutines.Count);
		coroutines.Add(coroutine);
		if (coroutines.Count == 1)
		{
			Debug.Log("Start Coroutines");
			routinesStarted?.Invoke(true);
		}
	}

	public void AssignAttack(CharacterView attacker, CharacterView target, bool canUseRunAction, bool losCheck)
	{
		QueueRoutine(WaitCharacterAttackCharacter(attacker, target, canUseRunAction, losCheck));
	}

	private IEnumerator WaitCharacterAttackCharacter(CharacterView attacker, CharacterView target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log("WaitCharacterAttackCharacter " + attacker + " " + target + " " + losCheck);
		
		while (attacker.CanMove() && !attacker.IsAtAttackRange(target, losCheck))
		{
			if (!attacker.IsAtAttackRange(target, losCheck))
			{
				yield return StartCoroutine(WaitCharacterMoved(attacker, target.GetPosition(), null, () =>
				{
					if (attacker.IsAtAttackRange(target, losCheck, 1.0f)) // at atk range
					{
						Debug.Log(attacker + " is at attack range");
						attacker.StopAgent();
					}
				}));
			}
				
			if (canUseRunAction && !attacker.CanMove() && !attacker.IsAtAttackRange(target, losCheck, 1.0f) && attacker.CanUseRunAction())
			{
				yield return StartCoroutine(WaitRunAction(attacker));
			}
			
			yield return null;
		}
		yield return StartCoroutine(WaitCharacterPerformAttack(attacker, target, true, losCheck));
	}

	private IEnumerator WaitCharacterPerformAttack(CharacterView character, CharacterView target, bool rangeCheck = true, bool losCheck = true)
	{
		Debug.Log("WaitCharacterPerformAttack " + character + " " + target + " " + losCheck);
		
		if (rangeCheck && !character.IsAtAttackRange(target, losCheck))
		{
			yield break;
		}
		
		yield return StartCoroutine(WaitCharacterLookPosition(character, target.GetPosition()));

		if (!character.CanAttack())
		{
			Debug.Log(character + " cannot attack!");
			DebugGUI.FloatingText(character.GetPosition(), "Can't!");
			yield return new WaitForSeconds(1.0f);
		}
		else
		{
			var characterData = character.GetCurrentData();
			var targetData = target.GetCurrentData();
			
			yield return character.StartCoroutine(character.PerformsAttack(target));
			yield return new WaitForSeconds(1.0f);
			// apply attack damage, etc
			targetData.TakeDamage(characterData.GetLastAttackDamage());
		}
	}

	private IEnumerator WaitCharacterInteract(CharacterView characterView, PropComponent prop, Waypoint waypoint = null)
	{
		Debug.Log("WaitCharacterInteract " + characterView + " > " + prop + " @ " + waypoint);
		if (waypoint != null)
		{
			var wpDir = waypoint.GetDirection();
			while (!characterView.IsFacingDirection(wpDir))
				characterView.LookDirection(wpDir, Time.deltaTime);
		}

		var data = characterView.GetCurrentData();
		data.SetRotation(characterView.GetRotation());
		if (prop.consumeActions > 0)
			data.ConsumeActions(prop.consumeActions);
		propInteracted?.Invoke(prop, characterView.gameObject);
		yield return prop.Interact(characterView);
	}

	private IEnumerator WaitCharacterLookPosition(CharacterView characterView, Vector3 pos) => WaitCharacterLookDirection(characterView, (pos - characterView.GetPosition()).normalized);
	private IEnumerator WaitCharacterLookDirection(CharacterView characterView, Vector3 dir)
	{
		while (!characterView.IsFacingDirection(dir))
		{
			characterView.RotateTowards(dir, Time.deltaTime);
			yield return null;
		}
		
		//TryGetCharacter(characterView, out var characterData);
		characterView.GetCurrentData().SetRotation(characterView.GetRotation());
	}

	public void MoveCharacterToWaypoint(CharacterView characterView, Waypoint waypoint,
		System.Action onComplete = null)
	{
		QueueRoutine(WaitCharacterMoved(characterView, waypoint.GetPosition(), () =>
		{
			if (characterView.WaypointReached(waypoint))
			{
				QueueRoutine(WaitCharacterLookDirection(characterView, waypoint.GetDirection()));
			}
		}));
	}
	
	private IEnumerator WaitCharacterMoved(CharacterView movingCharacterView, Vector3 dest, System.Action onComplete = null, System.Action onUpdate = null)
	{
		Debug.Log("WaitCharacterMoved " + movingCharacterView);
		
		if (!movingCharacterView.TryGetAgent(out var agent))
		{
			Debug.LogWarning(movingCharacterView + " has no agent");
			yield break;
		}
		
		agent.AssignDestination(dest);

		do yield return null;
		while (agent.pathPending);
		
		var path = agent.GetCurrentPath();
		if (path.Length > 1)
		{
			var firstPoint = path[1];
			var dir = (firstPoint - movingCharacterView.GetPosition()).normalized;
			while (!movingCharacterView.IsFacingDirection(dir))
			{
				movingCharacterView.RotateTowards(firstPoint, Time.deltaTime);
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning(movingCharacterView + " has path " + path.Length);
		}
		
		agent.SetCanMove(true);
		
		do
		{
		//	TryGetCharacter(movingCharacterView, out var characterData);
			
			var characterData = movingCharacterView.GetCurrentData();
			if (characterData.GetRemainingMovePoints() <= 0)
			{
				agent.StopMovement();
				break;
			}
			
			var oldPosition = movingCharacterView.GetPosition();
			
			yield return null;
			
			var newRotation = movingCharacterView.GetRotation();
			var newPosition = movingCharacterView.GetPosition();
			var deltaPos = (newPosition -  oldPosition).magnitude; // TODO: use length squared and recalculate move points
			
			characterData.ConsumeMovePoints(deltaPos);
			characterData.SetPosition(newPosition);
			characterData.SetRotation(newRotation);
			
			onUpdate?.Invoke();
			
		} while (agent.isMoving && !agent.reachedDestination);
		
		agent.ClearDestination();
		agent.SetCanMove(false);
		
		onComplete?.Invoke();
	}

	public bool IsCharacterSelected(int characterIndex) => IsCharacterSelected(allCharacters[characterIndex]);
	public bool IsCharacterSelected(CharacterView characterView) => characterView != null && characterView == selectedCharacter;

	public void SelectNextCharacter()
	{
		var index = 0;
		if( selectedCharacter != null )
			index = partyCharacters.IndexOf(selectedCharacter);

		int c = partyCharacters.Count;
		while (c-- > 0)
		{
			index = (index + 1) % partyCharacters.Count;
			var newCharacter = partyCharacters[index];
			if (CanControlCharacter(newCharacter))
			{
				SelectCharacter(newCharacter);
				return;
			}
		}
	}

	public void SelectCharacter(CharacterData characterData, bool focusCamera = false)
	{
		var dataIndex = characterData.GetCharacterIndex();
		if(dataIndex < 0 || dataIndex >= allCharacters.Count)
			Debug.LogError("SelectCharacter error invalid index " + characterData);
		SelectCharacter(allCharacters[dataIndex], focusCamera);
	}
	
	public void SelectCharacter(CharacterView characterView = null, bool focusCamera = false)
	{
		Debug.Log(this + " SelectCharacter " + characterView + " " + focusCamera);
		selectedCharacter = characterView;
		if (selectedCharacter != null)
		{
			if (focusCamera) mainCamera.Focus(selectedCharacter); /// todo Camera listen for event
		}
		selectionChanged?.Invoke(selectedCharacter, CanControlCharacter(selectedCharacter));
	}

	public void EndTurn()
	{
		gameData.EndTurn();
		StartCoroutine(EndTurnRoutine());
	}

	public bool CanSelectCharacter(CharacterView charView) => !HasCoroutines() && charView && !charView.IsDead() && (!gameData.IsPlayerTurn() || charView.IsInParty());

	public bool CanControlCharacter(CharacterView charView) => gameData.IsPlayerTurn() && !HasCoroutines() && charView && !charView.IsDead() && gameData.IsPlayerTurn() && charView.IsInParty();

	private IEnumerator EndTurnRoutine()
	{
		Debug.Log("Starts AI Routine: ");

		foreach (var otherCharacter in allCharacters)
		{
			if (partyCharacters.Contains(otherCharacter)) continue;
			SelectCharacter(otherCharacter, true);

			if (!otherCharacter.TryGetAIBrain(out var aiBrain))
			{
				Debug.LogWarning(otherCharacter + " has no aiBrain");
				yield return new WaitForSeconds(1.0f);
				continue;
			}

			Debug.Log("Think() " + aiBrain + " " + otherCharacter);
			yield return StartCoroutine(aiBrain.Think(otherCharacter));
			Debug.Log("End Think()");
		}
		
		yield return new WaitForSeconds(1.0f);
		gameData.NewTurn();
		
		SelectCharacter(partyCharacters.First(), true);
	}

	public void ClickRunAction() => RunAction(selectedCharacter, false);
	public void RunAction(CharacterView onCharacterView, bool force = false)
	{
		if (onCharacterView == null)
		{
			Debug.LogError(this + " no character!");
			return;
		}

		if (!force && !onCharacterView.CanUseRunAction())
		{
			Debug.LogWarning(onCharacterView + " has no actions");
			return;
		}

		QueueRoutine(WaitRunAction(onCharacterView));
	}

	public void SetInputEnabled(bool value)
	{
		if (value)
		{
			inputSystem.Player.Enable();
			inputSystem.UI.Enable();
		}
		else
		{
			inputSystem.Player.Disable();
			inputSystem.UI.Disable();
		}
	}
	
	
	private IEnumerator WaitRunAction(CharacterView characterView)
	{
		Debug.Log(">> WaitRunAction: " + characterView);
		DebugGUI.FloatingText(characterView.GetPosition(), "RUN!");
		yield return new WaitForSeconds(1.0f);
		characterView.GetCurrentData().RunAction();
	}
	
	private LevelView levelView;

	private IEnumerator Start()
	{
		SetInputEnabled(false);
		// TODO Fade In

		var currentLevel = gameData.GetCurrentLevel();
		yield return StartCoroutine(levelView.LoadLevel());
		gameData.SetCurrentLevel(gameData.GetLevelIndex(currentLevel));
		Debug.Log("level loaded " + gameData.GetCurrentLevelIndex());
		
		// TODO: load party
		
		// load from level
		var partyIndex = 0;
		foreach (var characterView in CharacterView.allInstances)
		{
			if(!characterView.HasSpawnData(out var spawnData))
			{
				Debug.LogWarning(characterView + " has no spawn data!");
				characterView.gameObject.SetActive(false);
				continue;
			}
			
			var characterData = Instantiate(spawnData);
			gameData.AddCharacter(characterData);
			characterView.Init(characterData);
			characterView.name += " (" + characterView.GetCharacterIndex() + ")";
			allCharacters.Add(characterView);
			
			if (characterView.IsPlayer())
			{
				gameData.SetCharacterInParty(characterData, partyIndex++);
				partyCharacters.Add(characterView);
			}
		}
		
		SetInputEnabled(true);
		// TODO Fade Out
		
		// first turn
		gameData.NewTurn();

		// todo fade
		yield return new WaitForSeconds(0.5f); // todo hak camera focus
		
		SelectNextCharacter();
	}
	
	private void InitSingleton()
	{
		Debug.Log(this + " Init()");
		if (gameData == null)
		{
			Debug.LogError(this + " has no game data!");
		}
		
		hasFocus = !Application.isEditor;
		
		mainCamera = FindObjectsByType<MainCamera>(default).First();
		aStar = FindObjectsByType<AstarPath>(default).First();
		
		levelView = Utils.GetOrAddComponent<LevelView>(gameObject);
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.RightClick.performed += ctx => RightClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => mainCamera.Zoom(ctx.ReadValue<Vector2>().y);

		gameData.newTurnStarted += TurnStarted;
		gameData.Init();

		StartCoroutine(Coroutines());
	}
	
	private IEnumerator Coroutines()
	{
		while (true)
		{
			if (coroutines.Count > 0)
			{
				while (coroutines.Count > 0)
				{
					var next =  coroutines[0];
					Debug.Log("StartCoroutine: " + coroutines.Count);
					yield return StartCoroutine(next);
					coroutines.RemoveAt(0);
				}
		
				routinesStarted?.Invoke(false);
				Debug.Log("End Coroutines");
			}
			
			yield return null;
		}
	}


	private void TurnStarted(int turn)
	{
		foreach (var characterView in GetAllCharacters())
		{
			characterView.TurnBegins(turn);
		}
	}

	private void LateUpdate()
	{
		if (hasFocus)
		{
			if (hasMousePosition) mainCamera.MoveCameraWithCursor(mousePosition);
		}
	}

	private void CharacterKilled(CharacterView character)
	{
		if (IsCharacterSelected(character))
		{
			SelectNextCharacter();
		}
	}
	
	private void Update()
	{
		if (inputSystem.UI.Cancel.WasPressedThisFrame())
		{
			hasFocus = false;
		}
		
		if (hasFocus)
		{
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.SetCursor(Resources.Load<Texture2D>("Cursors/cursor_none"), Vector2.zero, CursorMode.Auto);
		
			if (inputSystem.Player.Move.IsPressed())
			{
				mainCamera.Move(inputSystem.Player.Move.ReadValue<Vector2>() * cameraSpeed * Time.unscaledDeltaTime);
			}
		}
		else
		{
			Cursor.lockState = default;
			Cursor.SetCursor(null,default,default);
		}

		if (mouseOverViewport) // process mouse over
		{
			var debugCursor = false;
			var result = CursorRaycast(out var hit);
			if(debugCursor) Debug.Log("mouseOverViewport cursor result==" + result);
			if (result == CURSOR_RAYCAST_RESULT_NAVIGATION)
			{
				if (selectedCharacter != null)
				{
					selectedCharacter.TryGetAgent(out var agent);
					if (CanControlCharacter(selectedCharacter)) // mouse over navigation
					{
						agent.AssignDestination(hit.point);
					}
					else if(debugCursor) Debug.Log("character not controllable");
				}
				else if(debugCursor) Debug.Log("no selected character");
			}
			else if (result == CURSOR_RAYCAST_RESULT_CHARACTER) 
			{
				if (hit.collider.TryGetComponent(out CharacterView overCharacter))
				{
					if (selectedCharacter != null)
					{
						selectedCharacter.TryGetAgent(out var agent);
						if (CanControlCharacter(selectedCharacter)) // mouse over character
						{
							if (overCharacter.IsInParty())
							{
								agent.ClearDestination();
							}
							else
							{
								agent.AssignDestination(overCharacter.GetPosition());
							}
						}
						else if(debugCursor) Debug.Log("character not controllable");
					}
					else if(debugCursor) Debug.Log("no selected character");
				}
				
				if (hit.collider.TryGetComponent(out PropComponent overProp)) // click prop
				{
					if (selectedCharacter != null)
					{
						selectedCharacter.TryGetAgent(out var agent);
						if (CanControlCharacter(selectedCharacter)) // mouse over prop
						{
							if (overProp.GetBestWaypoint(selectedCharacter.GetPosition(), out var bestWaypoint))
							{
								agent.AssignDestination(bestWaypoint.GetPosition());
							}
							else
							{
								Debug.LogWarning("no waypoint found");
							}
						}
						else if(debugCursor) Debug.Log("character not controllable");
					}
					else if(debugCursor) Debug.Log("no selected character");
				}
			}
			else
			{
				if (selectedCharacter != null)
				{
					selectedCharacter.TryGetAgent(out var agent);
					if (CanControlCharacter(selectedCharacter)) // mouse over raycast no result
					{
						agent.ClearDestination();
					}
					else if(debugCursor) Debug.Log("character not controllable");
				}
				else if(debugCursor) Debug.Log("no selected character");
			}
		}
	}

	private void OnApplicationFocus(bool value)
	{
		hasFocus = value;
	}

	private const int CURSOR_RAYCAST_RESULT_NONE = 0;
	private const int CURSOR_RAYCAST_RESULT_NAVIGATION = 1;
	private const int CURSOR_RAYCAST_RESULT_CHARACTER = 2;
	public int CursorRaycast(out RaycastHit hit)
	{
		//TODO: if mouse over viewport
		hit = default(RaycastHit);
		var cam = Camera.main;
		var ray = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(LAYER_CHARACTER)))
		{
			//DebugGUI.Log("Hit Character: " + hit.collider);
			if(drawDebug) DbgDraw.WireCapsule(hit.collider.transform.position, Quaternion.identity, 0.5f, 1.5f, Color.green);
			Utils.DrawDebugCircle(hit.collider.transform.position, Vector3.up, 0.5f);

			foreach (var cursorHandler in hit.collider.GetComponents<ICursorHandler>().Concat(hit.collider.GetComponentsInChildren<ICursorHandler>()))
			{
				cursorHandler.CursorOver();
			}

			return CURSOR_RAYCAST_RESULT_CHARACTER;
		}
		
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(LAYER_GROUND, LAYER_WALL)))
		{
			//DebugGUI.Log("Hit Navigation: " + hit.point);
			if(drawDebug) DbgDraw.WireDisc(hit.point, Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90,0,0),maxNavDistance, Color.blue);
			Utils.DrawDebugCircle(hit.point, Vector3.up, maxNavDistance);

			if (TryGetNearestNavPosition(hit.point, maxNavDistance, out var navPosition))
			{
				hit.point = navPosition;
				if(drawDebug) DbgDraw.WireDisc(navPosition, Quaternion.identity,maxNavDistance * 0.5f, Color.cyan);

				return CURSOR_RAYCAST_RESULT_NAVIGATION;
			}
		}

		return CURSOR_RAYCAST_RESULT_NONE;
	}

	public void OnGUIDebug()
	{
		const float space = 0.5f;
		GUILayout.FlexibleSpace();
		GUILayout.Label(GetLabel("hasFocus") + hasFocus);
		GUILayout.Label(GetLabel("coroutines") + coroutines.Count);
		if (selectedCharacter)
		{
			GUILayout.Space(space);
			GUILayout.BeginVertical("window");
			selectedCharacter?.OnGUIDebug();
			GUILayout.EndVertical();
		}
	}
	
	string GetLabel(string label) => ObjectNames.NicifyVariableName(label) + ": ";
}

public interface ICursorHandler
{
	void CursorOver();
}