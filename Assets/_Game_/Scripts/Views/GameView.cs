using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEngine;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameView : MonoBehaviour, IView<GameController>
{
	static bool debugCursor = false;
	const int CURSOR_RAYCAST_RESULT_NONE = 0;
	const int CURSOR_RAYCAST_RESULT_NAVIGATION = 1;
	const int CURSOR_RAYCAST_RESULT_CHARACTER = 2;
	
	override public string ToString() => "GameView";

	
	static private GameView instance;
	static public GameView GetInstance()
	{
		return instance;
	}
	
	private GameController characterController;

	public GameController GetController()
	{
		if(!characterController) characterController = GetComponent<GameController>();
		return characterController;
	}
	
	void SelectionChanged()
	{
		var selectedCharacter = PlayerSelection.GetSelectedCharacter();
		if (selectedCharacter)
		{
			cameraPivot.Focus(selectedCharacter);
		}
	}
	
	void Awake()
	{
		if (instance != null)
		{
			Debug.LogError("duplicated game view!");
			return;
		}

		Debug.Log(this + " Awake");
		instance = this;
		DontDestroyOnLoad(gameObject);
		name = ToString();
		
		OnApplicationFocus(!Application.isEditor);
		//Debug.Log("hasFocus=" + hasFocus + " hasEditorFocus=" + hasEditorFocus);
		//hasEditorFocus = false;
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.RightClick.performed += ctx => RightClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => CameraZoom(-ctx.ReadValue<Vector2>().y);
		inputSystem.Player.RotateLeft.performed += ctx => CameraRotate(false);
		inputSystem.Player.RotateRight.performed += ctx => CameraRotate(true);
		
		PlayerSelection.selectionChanged += SelectionChanged;
	}

	void CameraRotate(bool left)
	{
		if(hasFocus)cameraPivot.Rotate(left ? -1 : 1);
	}
	
	void CameraZoom(float delta)
	{
		if (hasFocus) cameraPivot.Zoom(delta);
	}

	[SerializeField] private List<string> scenesToLoad = new List<string>();

	private bool isLoadingScenes;
	
	private IEnumerator Start()
	{
		Debug.Log(this + " Start");
		
		StartCoroutine(Coroutines());
		
		canvas.UpdateUI(this);
		
		SetInputEnabled(false);

		isLoadingScenes = true;
		yield return null;
		// TODO Fade In

		// load level
		for(int i = 0; i < scenesToLoad.Count; i++)
		{
			var scene = scenesToLoad[i];
			if (SceneManager.GetSceneByName(scene).isLoaded)
			{
				Debug.Log("load scene: " + scene + " already loaded");
				continue;
			}

			Debug.Log("load scene: " + scene);
			var async = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
			while (async.isDone == false) yield return null;
		}
		
		yield return null; //
		
		//Debug.Log(this + " rebuild navigation async");
		foreach (var progress in AstarPath.active.ScanAsync()) {
			Debug.Log("AstarPath.active.ScanAsync " + progress.ToString());
			yield return null;
		}
		
		yield return null; //
		
		var levelCharacters = Utils.FindObjects<CharacterController>()
			.OrderBy(c => c.name)
			.ToList();
		
		Debug.Log(this + " LevelLoaded: levelCharacters=" + levelCharacters.Count);
		
		var partyIndex = 0;
		foreach (var character in levelCharacters)
		{
			character.Spawn();
			if(character.CompareTag(Consts.TAG_PLAYER))
				character.SetPartyIndex(partyIndex++);
		}
		
		// TODO Fade Out
		yield return null;
		isLoadingScenes = false;
		
		GetController().NewTurn();
		
		PlayerSelection.SelectNextCharacter();
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

	public void ClickCharacter(CharacterView clickedCharacterView)
	{
		Debug.Log(this + " ClickCharacter " + clickedCharacterView);
		var selectedCharacter = PlayerSelection.GetSelectedCharacter();
		var clickedCharacter = clickedCharacterView.GetController();
	//	//var selectedCharacter = gameController.GetSelectedCharacter();
		if (clickedCharacter.InParty())
		{
			if(GetController().IsPlayerTurn())
				PlayerSelection.SelectCharacter(clickedCharacter); // ClickCharacter select party character
		}
		else if (clickedCharacter.IsEnemyToPlayer(clickedCharacter))
		{
			if (selectedCharacter != null)
			{
				// assign attack
				if (selectedCharacter.CanAttack(false))
				{
					AssignAttack(selectedCharacter, clickedCharacter, false, true);
				}
				else
				{
					DebugGUI.FloatingText(selectedCharacter.GetCenterPosition(), "Can't");
				}
			}
		}
		else
		{
			if (selectedCharacter != null)
			{
				Debug.Log("TODO " + selectedCharacter + " interact with " + clickedCharacter); //TODO
			}
		}
	}

	public void AssignAttack(CharacterController attacker, CharacterController target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log(this + " AssignAttack " + attacker + " " + target+ " " + canUseRunAction + " " + losCheck);
		QueueRoutine(WaitCharacterAttackCharacter(attacker, target, canUseRunAction, losCheck));
	}

	public void ClickProp(PropComponent clickedProp) // TODO Prop Controller
	{
		Debug.Log(this + " ClickProp " + clickedProp);
		var gameController = GetController();
		var selectedCharacter = PlayerSelection.GetSelectedCharacter();
		//var selectedCharacter = gameController.GetSelectedCharacter();
		if (selectedCharacter != null && CanControlCharacter(selectedCharacter))
		{
			AssignInteract(selectedCharacter, clickedProp);
		}
	}

	public void AssignInteract(CharacterController character, PropComponent clickedProp) // TODO Prop Controller
	{
		if (clickedProp.GetBestWaypoint(character.GetPosition(), out var bestWaypoint))
		{
			Debug.Log(this + " AssignInteract " + character + " " + clickedProp);
			QueueRoutine(WaitCharacterMoved(character, bestWaypoint.GetPosition(), () =>
			{
				var agent = character.GetAgent();
				if (agent.ReachedDestination(bestWaypoint.GetPosition()))
				{
					// waypoint reach
					if (character.CanInteract())
					{
						QueueRoutine(WaitCharacterInteract(character, clickedProp, bestWaypoint));
					}
				}
				else
				{
					var dist = Vector3.Distance(character.GetPosition(), bestWaypoint.GetPosition());
					Debug.Log(character + " not at reach: " + dist + " / " + agent.GetMinReachDistance() + " to " + bestWaypoint);
				}
			}));
		}
		else Debug.LogWarning(this + " AssignInteract " + character + " " + clickedProp + " no waypoint!");
	}

	public void ClickNavigation(Vector3 navPosition)
	{
		Debug.Log(this + " ClickNavigation " + navPosition);
		var selectedCharacter = PlayerSelection.GetSelectedCharacter();
		//var selectedCharacter = gameController.GetSelectedCharacter();
		if (selectedCharacter != null && CanControlCharacter(selectedCharacter))
		{
			AssignMove(selectedCharacter, navPosition);
		}
	}

	public void AssignMove(CharacterController controller, Vector3 navPosition)
	{
		Debug.Log(this + " AssignMove " + controller + " " + navPosition);
		QueueRoutine(WaitCharacterMoved(controller, navPosition));
	}
	
	public void AssignRunAction(CharacterController character)
	{
		Debug.Log(this + " AssignRunAction " + character);
		QueueRoutine(WaitRunAction(character));
	}
	
	public GameCanvas canvas => GameCanvas.Instance;
	
	public System.Action<bool> routinesStarted;

	public List<CharacterView> GetAllCharacters() =>  CharacterView.allInstances.ToList();

	public List<CharacterView> GetPartyCharacters() => CharacterView.allInstances.Where(c => c.GetController().InParty()).ToList();

	public int GetPartyCharacterIndex(CharacterView characterView) => GetPartyCharacters().IndexOf(characterView);
	public int GetCharacterIndex(CharacterView characterView) => GetAllCharacters().IndexOf(characterView);

	public bool HasCoroutines() => coroutines.Count > 0;

	public bool drawDebug = false;
	[Range(0f,1f)] public float debugGUI = 0f;
	//public float cameraSpeed = 1.0f;
	[SerializeField] private float cameraDeltaY = 1f;
	[Range(0.01f, 0.50f)] public float maxNavDistance = 0.50f;

	[Header("Debug")]
	[ReadOnly][SerializeField] private bool hasFocus;
	[ReadOnly][SerializeField] private Vector2 mousePosition;
	private Vector2 lastMousePosition;

	private CameraPivot cameraPivot => CameraPivot.Instance; // TODO
	
	private Vector2 middleClickMousePosition;

	private bool holdMiddleClick;
	public InputSystem_Actions inputSystem { get; private set; }
	
	private List<IEnumerator> coroutines = new  List<IEnumerator>();

	public void MouseMove(Vector2 pos)
	{
		mousePosition = pos;
	}

	public void RightClick(float value)
	{
		if (value > 0) // pressed
		{
			
			if (GetController().IsPlayerTurn() && !HasCoroutines())
			{
				PlayerSelection.SelectCharacter(null); // deselect all
			}
		}
	}

	public void LeftClick(float value)
	{
	//	Debug.Log("LeftClick: " + value);

		if (canvas.mouseOverViewport && value > 0f) // pressed
		{
			//hasEditorFocus = true;
			
			var raycastResult = CursorRaycast(out var hit);
			if (raycastResult == CURSOR_RAYCAST_RESULT_CHARACTER)
			{
				if (hit.collider.TryGetComponent(out CharacterView clickedCharacter)) // click character
				{
					ClickCharacter(clickedCharacter);
				}

				if (hit.collider.TryGetComponent(out PropComponent clickedProp)) // click prop
				{
					ClickProp(clickedProp);
				}
			}
			else if (raycastResult == CURSOR_RAYCAST_RESULT_NAVIGATION) // click navigation
			{
				ClickNavigation(hit.point);
			}
		}
	}

	public void QueueRoutine(IEnumerator coroutine)
	{
//		Debug.Log("QueueRoutine: " + coroutines.Count);
		coroutines.Add(coroutine);
		if (coroutines.Count == 1)
		{
			Debug.Log("Start Coroutines");
			routinesStarted?.Invoke(true);
		}
	}

	private IEnumerator WaitCharacterAttackCharacter(CharacterController attacker, CharacterController target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log("WaitCharacterAttackCharacter " + attacker + " " + target + " " + losCheck);

		var atAttackRange = false;
		while (!atAttackRange)
		{
			yield return null;

			atAttackRange = attacker.IsAtAttackRange(target, losCheck);
			if (atAttackRange)
			{
				Debug.Log(attacker + " atAttackRange of " + target + ", StopAgent");
				attacker.StopAgent();
			}
			
			else if (!attacker.IsMoving())
			{
				while (!attacker.HasMovePoints() && canUseRunAction && attacker.CanUseRunAction())
				{
					yield return StartCoroutine(WaitRunAction(attacker));
				}
				
				if (!attacker.HasMovePoints())
				{
					Debug.Log(attacker + " has no move points to reach target");
					yield break;
				}
				
				Debug.Log(attacker + " starts moving towards target");
				StartCoroutine(WaitCharacterMoved(attacker, target.GetPosition()));
				yield return null;
			}
			
			//else Debug.Log(attacker + " moves towards " + target);
		}
		
		yield return StartCoroutine(WaitCharacterPerformAttack(attacker, target));
	}

	private IEnumerator WaitCharacterPerformAttack(CharacterController character, CharacterController target)
	{
		Debug.Log("WaitCharacterPerformAttack " + character + " " + target );
		
		yield return StartCoroutine(WaitCharacterLookPosition(character, target.GetCenterPosition()));

		if (!character.CanAttack())
		{
			Debug.Log(character + " cannot attack!");
			DebugGUI.FloatingText(character.GetPosition(), "Can't!");
			yield return new WaitForSeconds(1.0f);
		}
		else
		{
			character.AttackAction();
			yield return new WaitForSeconds(1.0f);//TODO wait attack duration
			// apply attack damage, etc
			target.TakeDamage(character.GetLastAttackDamage());
		}

		Debug.Log("attack ends");
	}

	private IEnumerator WaitCharacterInteract(CharacterController character, PropComponent prop, Waypoint waypoint = null)
	{
		Debug.Log("WaitCharacterInteract " + character + " > " + prop + " @ " + waypoint);
		if (waypoint != null)
		{
			var wpDir = waypoint.GetDirection();

			while (!character.IsFacingDirection(wpDir))
			{
				character.LookDirection(wpDir, Time.deltaTime);
				yield return null;
			}
			
			if (prop.consumeActions > 0)
				character.ConsumeActions(prop.consumeActions);
			
			yield return new WaitForSeconds(prop.triggerDelay);

			prop.GetController().Interact();
		
			yield return new WaitForSeconds(prop.animDuration);
		}
	}

	private IEnumerator WaitCharacterLookPosition(CharacterController character, Vector3 pos) => WaitCharacterLookDirection(character, (pos - character.GetPosition()).normalized);
	private IEnumerator WaitCharacterLookDirection(CharacterController character, Vector3 dir)
	{
		character.StopAgent();
		
		while (!character.IsFacingDirection(dir))
		{
			character.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
	}

	public void MoveCharacterToWaypoint(CharacterController character, Waypoint waypoint,
		System.Action onComplete = null)
	{
		QueueRoutine(WaitCharacterMoved(character, waypoint.GetPosition(), () =>
		{
			if (character.WaypointReached(waypoint))
			{
				QueueRoutine(WaitCharacterLookDirection(character, waypoint.GetDirection()));
			}
		}));
	}

	private const float TIMED_OUT = 3.0f;
	private IEnumerator WaitCharacterMoved(CharacterController movingCharacter, Vector3 dest, System.Action onComplete = null)
	{
		Debug.Log("WaitCharacterMoved " + movingCharacter);
		
		var agent = movingCharacter.GetAgent();
		
		yield return StartCoroutine(agent.SearchPathAsync(dest));
		
		var path = agent.GetCurrentPath();
		if (path.Length > 1)
		{
			var firstPoint = path[1];
			var dir = (firstPoint - movingCharacter.GetPosition()).normalized;
			while (!movingCharacter.IsFacingDirection(dir))
			{
				movingCharacter.LookDirection(dir, Time.deltaTime);
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning(movingCharacter + " has wrong path: length=" + path.Length);
			yield break;
		}
		
		agent.StartMove();
		
		var timeout = 0.0f;
		while(true)
		{
			if (timeout > TIMED_OUT)
			{
				Debug.LogWarning(movingCharacter + " TIMED_OUT!");
				break;
			}

			if (!movingCharacter.IsMoving())
			{
				Debug.Log(movingCharacter + " something stopped movement");
				break;
			}
			
			if (!movingCharacter.HasMovePoints())
			{
				Debug.Log(movingCharacter + " !HasMovePoints");
				agent.StopMovement();
				break;
			}

			if (agent.ReachedDestination(dest))
			{
				Debug.Log(movingCharacter + " reached destination!");
				agent.StopMovement();
				break;
			}
			
			var oldPosition = movingCharacter.GetPosition();

			DebugGUI.Line(oldPosition, dest, Color.cyan, 0f);
			DebugGUI.WireArrow(oldPosition, oldPosition + agent.velocity, Vector3.one, Color.blue, 0f);
			DebugGUI.WireDisc(dest, Vector3.up, agent.GetRadius(), Color.cyan, 0f);
			yield return null;
			
			var newPosition = movingCharacter.GetPosition();
			var deltaPos = (newPosition -  oldPosition).magnitude; // TODO: use length squared and recalculate move points
			if (deltaPos <= 0f) timeout += Time.deltaTime;
			else timeout = 0f;
			
			movingCharacter.ConsumeMovePoints(deltaPos);
		}
		
		onComplete?.Invoke();
	}

	public bool CanEndTurn() => !HasCoroutines() && GetController().IsPlayerTurn();

	public bool IsCharacterVisible(CharacterController character)
	{
		if (RoomTrigger.activeRooms.Any(t => t.HasCharacter(character)))
			return false;

		return true;
	}

	public void ClickEndTurn()
	{
		GetController().EndTurn();
		StartCoroutine(EndTurnRoutine());
	}
	
	private IEnumerator EndTurnRoutine()
	{
		Debug.Log("Starts AI Routine: ");
		
		foreach (var otherCharacterView in GetAllCharacters())
		{
			var otherCharacter = otherCharacterView.GetController();
			if (otherCharacter.InParty()) continue;

			if (IsCharacterVisible(otherCharacter))
				cameraPivot.Focus(otherCharacter);
			else Debug.Log("Do not focus invisible character: " + otherCharacter);

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
		
		GetController().NewTurn();
		
		PlayerSelection.SelectNextCharacter();
	}

	public bool CanUseRunAction() =>
		!HasCoroutines() && GetController().IsPlayerTurn() && PlayerSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter.CanUseRunAction();

	public bool IsControlPanelVisible() =>
		GetController().IsPlayerTurn() && PlayerSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter.IsControllable();

	//public CharacterController GetPartyCharacter(int index) => CharacterController.GetPartyCharacter(index);
	
	public void ClickRunAction()
	{
		PlayerSelection.TryGetSelectedCharacter(out var selectedCharacter);
		if (selectedCharacter == null)
		{
			Debug.LogWarning("no selected character!");
			return;
		}
		
		if (selectedCharacter.CanUseRunAction())
		{
			AssignRunAction(selectedCharacter);
		}
		else
		{
			DebugGUI.FloatingText(selectedCharacter.GetCenterPosition(), "Can't");
		}
	}

	private bool inputEnabled;
	public void SetInputEnabled(bool value)
	{
		if (inputEnabled != value)
		{
			inputEnabled = value;
			Debug.Log(this + " inputEnabled=" + inputEnabled);
			
			if (inputEnabled)
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
	}

	public bool CanControlSelectedCharacter() => PlayerSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter && CanControlCharacter(selectedCharacter);
	public bool CanControlCharacter(CharacterController character)
	{
		return hasFocus && !HasCoroutines() && GetController().IsPlayerTurn() && character.IsControllable();
	}
	
	
	private IEnumerator WaitRunAction(CharacterController character)
	{
		Debug.Log(">> WaitRunAction: " + character);
		yield return new WaitForSeconds(1.0f);
		character.RunAction();
	}

//	private bool HasFocus() => hasFocus && (!Application.isEditor || hasEditorFocus);
	
	private void LateUpdate()
	{
		canvas.UpdateUI(this);
		lastMousePosition = mousePosition;
	}

	private void Update()
	{
		if (Application.isEditor)
		{
			if (hasFocus && inputSystem.Player.Escape.WasPressedThisFrame())
			{
				OnApplicationFocus(false);
			}
			
			if (!hasFocus && inputSystem.UI.Click.WasPressedThisFrame())
			{
				OnApplicationFocus(true);
			}
		}
		
		if (hasFocus)
		{
			SetInputEnabled(!isLoadingScenes && !IngameDebugConsole.DebugLogManager.Instance.IsLogWindowVisible);
			
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = !holdMiddleClick;
			Cursor.SetCursor(Resources.Load<Texture2D>("Cursors/cursor_none"), Vector2.zero, CursorMode.Auto);
		
			if (inputSystem.Player.Move.IsPressed())
			{
				cameraPivot.MoveFocus(inputSystem.Player.Move.ReadValue<Vector2>() * Time.unscaledDeltaTime, Space.Self);
			}

			if (inputSystem.Player.MiddleClick.WasReleasedThisFrame())
			{
				holdMiddleClick = false;
			}
			if (inputSystem.Player.MiddleClick.WasPressedThisFrame())
			{
				holdMiddleClick = true;
				middleClickMousePosition = mousePosition;
			}

			if (holdMiddleClick)
			{
				//cameraPivot.PanWithMouse(mousePosition.y, Time.unscaledDeltaTime);
				cameraPivot.MoveFocus((middleClickMousePosition - mousePosition) * Time.unscaledDeltaTime, Space.World);
				cameraPivot.Orbit((middleClickMousePosition - mousePosition).x * Time.unscaledDeltaTime, 
					(lastMousePosition - mousePosition).y * Time.unscaledDeltaTime);
			}
			else
			{
				cameraPivot.PanWithMouse(mousePosition, Time.unscaledDeltaTime);
			}
		}
		else
		{
			SetInputEnabled(false);
			
			Cursor.lockState = default;
			Cursor.visible = true;
			Cursor.SetCursor(null,default,default);
		}

		PlayerSelection.TryGetSelectedCharacter(out var selectedCharacter);
		if (selectedCharacter != null)
		{
			if (selectedCharacter.IsDead())
			{
				PlayerSelection.SelectNextCharacter();
			}
		}
		
		if (canvas.mouseOverViewport && !holdMiddleClick) // process mouse over
		{
			//var selectedCharacter = gameController.GetSelectedCharacter();
			var result = CursorRaycast(out var hit);
			if(debugCursor) Debug.Log("mouseOverViewport cursor result==" + result);
			if (result == CURSOR_RAYCAST_RESULT_NAVIGATION)
			{
				if (selectedCharacter != null)
				{
					if (CanControlCharacter(selectedCharacter))
					{
						selectedCharacter.GetAgent().AssignDestination(hit.point); // mouse over navigation
					}
					else if(debugCursor) Debug.Log("character not controllable");
				}
				else if(debugCursor) Debug.Log("no selected character");
			}
			else if (result == CURSOR_RAYCAST_RESULT_CHARACTER) 
			{
				if (hit.collider.TryGetComponent(out CharacterView overCharacterView))
				{
					var overCharacter = overCharacterView.GetController();
					if (selectedCharacter != null)
					{
						if (CanControlCharacter(selectedCharacter))
						{
							var agent = selectedCharacter.GetAgent();
							if (overCharacter.InParty())
							{
								agent.ClearDestination();
							}
							else
							{
								agent.AssignDestination(overCharacter.GetPosition()); // mouse over character
							}
						}
						else if(debugCursor) Debug.Log("character not controllable");
					}
					else if(debugCursor) Debug.Log("no selected character");
				}
				else if (hit.collider.TryGetComponent(out PropComponent overProp)) 
				{
					if (selectedCharacter != null)
					{
						if (CanControlCharacter(selectedCharacter)) // mouse over prop
						{
							if (overProp.GetBestWaypoint(selectedCharacter.GetPosition(), out var bestWaypoint))
							{
								var agent = selectedCharacter.GetAgent();
								agent.AssignDestination(bestWaypoint.GetPosition()); // mouse over prop
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
			else // mouse over raycast no result
			{
				if (selectedCharacter != null)
				{
					if (CanControlCharacter(selectedCharacter))
					{
						var agent = selectedCharacter.GetAgent();
						agent.ClearDestination();
					}
					else if(debugCursor) Debug.Log("character not controllable");
				}
				else if(debugCursor) Debug.Log("no selected character");
			}
		}
		else
		{
			if (selectedCharacter != null && CanControlCharacter(selectedCharacter))
			{
				selectedCharacter.GetAgent().ClearDestination();
			}
		}
	}

	public void OnApplicationFocus(bool value)
	{
		if (hasFocus != value)
		{
			Debug.Log("OnApplicationFocus: " + value);
			hasFocus = value;
		}
		//hasEditorFocus = value;
		////Debug.Log("hasFocus=" + hasFocus + " hasEditorFocus=" + hasEditorFocus);
	}

	public int CursorRaycast(out RaycastHit hit)
	{
		//TODO: if mouse over viewport
		hit = default(RaycastHit);
		var cam = Camera.main;
		var ray = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(Consts.LAYER_CHARACTER)))
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
		
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(Consts.LAYER_GROUND, Consts.LAYER_WALL)))
		{
			//DebugGUI.Log("Hit Navigation: " + hit.point);
			if(drawDebug) DbgDraw.WireDisc(hit.point, Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90,0,0),maxNavDistance, Color.blue);
			Utils.DrawDebugCircle(hit.point, Vector3.up, maxNavDistance);

			if (NavUtility.TryGetNearestNavPosition(hit.point, maxNavDistance, out var navPosition))
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
		/*const float space = 0.5f;
		GUILayout.FlexibleSpace();
		GUILayout.Label(GetLabel("hasFocus") + hasFocus);
		GUILayout.Label(GetLabel("coroutines") + coroutines.Count);
		if (selectedCharacter)
		{
			GUILayout.Space(space);
			GUILayout.BeginVertical("window");
			selectedCharacter?.OnGUIDebug();
			GUILayout.EndVertical();
		}*/
	}

	string GetLabel(string label)
	{
		#if UNITY_EDITOR
		return UnityEditor.ObjectNames.NicifyVariableName(label) + ": ";
		#else
		return label;
		#endif
	}
}

public interface ICursorHandler
{
	void CursorOver();
}