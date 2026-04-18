using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEngine;
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
		if (instance == null) instance = Utils.FindFirstObject<GameView>();
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
			TopDownCamera.Instance.Focus(selectedCharacter);
		}
	}
	
	void Awake()
	{
		instance = this;
		DontDestroyOnLoad(gameObject);
		name = ToString();
		
		hasFocus = !Application.isEditor;
		//Debug.Log("hasFocus=" + hasFocus + " hasEditorFocus=" + hasEditorFocus);
		hasEditorFocus = false;
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.RightClick.performed += ctx => RightClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => TopDownCamera.Instance.Zoom(-ctx.ReadValue<Vector2>().y);
		inputSystem.Player.RotateLeft.performed += ctx => TopDownCamera.Instance.RotateLR(false);
		inputSystem.Player.RotateRight.performed += ctx => TopDownCamera.Instance.RotateLR(true);
		
		PlayerSelection.selectionChanged += SelectionChanged;
	}

	[SerializeField] private List<string> scenesToLoad = new List<string>();
	
	private IEnumerator Start()
	{
		StartCoroutine(Coroutines());
		
		canvas = FindObjectsByType<GameCanvas>(default).First();
		canvas.UpdateUI(this);
		
		SetInputEnabled(false);
		// TODO Fade In
		yield return null;

		for(int i = 0; i < scenesToLoad.Count; i++)
		{
			var loadSceneMode = i > 0 ? UnityEngine.SceneManagement.LoadSceneMode.Additive : LoadSceneMode.Single;
			var async = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scenesToLoad[i], loadSceneMode);
			while (async.isDone == false) yield return null;
		}
		
		yield return null; //
		
		var levelCharacters = Utils.FindObjects<CharacterController>()
			.OrderBy(c => c.name)
			.ToList();
		
		Debug.Log(this + " LevelLoaded: levelCharacters=" + levelCharacters.Count);
		
		var partyIndex = 0;
		foreach (var character in levelCharacters)
		{
			if(character.CompareTag(Consts.TAG_PLAYER))
				character.SetPartyIndex(partyIndex++);
		}
		
		// TODO Fade Out
		yield return null;
		
		SetInputEnabled(true);
		
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
		else if (clickedCharacter.IsEnemyToPlayer())
		{
			if (selectedCharacter != null)
			{
				// assign attack
				if (selectedCharacter.CanAttack())
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
		Debug.Log(this + " AssignInteract " + character + " " + clickedProp);
		if (clickedProp.GetBestWaypoint(character.GetPosition(), out var bestWaypoint))
		{
			QueueRoutine(WaitCharacterMoved(character, bestWaypoint.GetPosition(), () =>
			{
				var agent = character.GetAgent();
				var dist = Vector3.Distance(character.GetPosition(), bestWaypoint.GetPosition());
				if (dist <= agent.reachDistance)
				{
					// waypoint reach
					if (character.CanInteract())
					{
						QueueRoutine(WaitCharacterInteract(character, clickedProp, bestWaypoint));
					}
				}
				else
				{
					Debug.Log(character + " not at reach: " + dist + " / " + agent.reachDistance + " to " + bestWaypoint);
				}
			}));
		}
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
	
	public System.Action<bool> routinesStarted;

	public List<CharacterView> GetAllCharacters() =>  CharacterView.allInstances.ToList();

	public List<CharacterView> GetPartyCharacters() => CharacterView.allInstances.Where(c => c.GetController().InParty()).ToList();

	public int GetPartyCharacterIndex(CharacterView characterView) => GetPartyCharacters().IndexOf(characterView);
	public int GetCharacterIndex(CharacterView characterView) => GetAllCharacters().IndexOf(characterView);

	public bool HasCoroutines() => coroutines.Count > 0;

	public bool drawDebug = false;
	[Range(0f,1f)] public float debugGUI = 0f;
	//public float cameraSpeed = 1.0f;
	[Range(0.01f, 0.50f)] public float maxNavDistance = 0.50f;

	[Header("Debug")]
	[ReadOnly][SerializeField] private bool hasFocus;
	[ReadOnly][SerializeField] private bool hasEditorFocus;
	[ReadOnly][SerializeField] private GameCanvas canvas;
	[ReadOnly][SerializeField] private Vector2 mousePosition;

	private Vector2 middleClickMousePosition;

	private bool holdMiddleClick;
	//private TopDownCamera _topDownCamera;
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
			hasEditorFocus = true;
			
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

	private IEnumerator WaitCharacterPerformAttack(CharacterController character, CharacterController target, bool rangeCheck = true, bool losCheck = true)
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
			character.AttackAction();
			yield return new WaitForSeconds(1.0f);
			// apply attack damage, etc
			target.TakeDamage(character.GetLastAttackDamage());
		}
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
		while (!character.IsFacingDirection(dir))
		{
			character.RotateTowards(dir, Time.deltaTime);
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
	
	private IEnumerator WaitCharacterMoved(CharacterController movingCharacter, Vector3 dest, System.Action onComplete = null, System.Action onUpdate = null)
	{
		Debug.Log("WaitCharacterMoved " + movingCharacter);

		var agent = movingCharacter.GetAgent();
		agent.AssignDestination(dest);

		do yield return null;
		while (agent.pathPending);
		
		var path = agent.GetCurrentPath();
		if (path.Length > 1)
		{
			var firstPoint = path[1];
			var dir = (firstPoint - movingCharacter.GetPosition()).normalized;
			while (!movingCharacter.IsFacingDirection(dir))
			{
				movingCharacter.RotateTowards(firstPoint, Time.deltaTime);
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning(movingCharacter + " has wrong path: length=" + path.Length);
			yield break;
		}
		
		agent.SetCanMove(true);
		
		do
		{
			if (movingCharacter.CanMove() == false)
			{
				agent.StopMovement();
				break;
			}
			
			var oldPosition = movingCharacter.GetPosition();
			
			yield return null;
			
			//var newRotation = movingCharacterView.GetRotation();
			var newPosition = movingCharacter.GetPosition();
			var deltaPos = (newPosition -  oldPosition).magnitude; // TODO: use length squared and recalculate move points
			
			movingCharacter.ConsumeMovePoints(deltaPos);
			//movingCharacter.SetPosition(newPosition);
			
			var newRotation = movingCharacter.GetRotation();
			//movingCharacter.SetRotation(newRotation);
			
			onUpdate?.Invoke();
			
		} while (agent.isMoving && !agent.reachedDestination);
		
		//movingCharacter.SetPosition(path.Last());
		//movingCharacter.SetRotation(movingCharacter.GetRotation());
		
		agent.SetCanMove(false);
		
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
				TopDownCamera.Instance.Focus(otherCharacter);
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

	public bool CanControlSelectedCharacter() => PlayerSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter && CanControlCharacter(selectedCharacter);
	public bool CanControlCharacter(CharacterController character)
	{
		return HasFocus() && !HasCoroutines() && GetController().IsPlayerTurn() && character.IsControllable();
	}
	
	
	private IEnumerator WaitRunAction(CharacterController character)
	{
		Debug.Log(">> WaitRunAction: " + character);
		yield return new WaitForSeconds(1.0f);
		character.RunAction();
	}

	private bool HasFocus() => hasFocus && (!Application.isEditor || hasEditorFocus);
	
	private void LateUpdate()
	{
		if (HasFocus())
		{
			TopDownCamera.Instance.PanWithMouse(mousePosition, Time.unscaledDeltaTime);
		}

		canvas.UpdateUI(this);
	}
	
	private void Update()
	{
		if (hasFocus && inputSystem.Player.Escape.WasPressedThisFrame())
		{
			hasFocus = hasEditorFocus = false;
			//Debug.Log("hasFocus=" + hasFocus + " hasEditorFocus=" + hasEditorFocus);
		}

		if (Application.isEditor)
		{
			if (!hasFocus)
				hasEditorFocus = false;
			
			if (!hasFocus && inputSystem.UI.Click.WasPressedThisFrame())
			{
				hasFocus = hasEditorFocus = true;
				//Debug.Log("hasFocus=" + hasFocus + " hasEditorFocus=" + hasEditorFocus);
			}
		}
		
		if (HasFocus())
		{
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.SetCursor(Resources.Load<Texture2D>("Cursors/cursor_none"), Vector2.zero, CursorMode.Auto);
		
			if (inputSystem.Player.Move.IsPressed())
			{
				TopDownCamera.Instance.Move(inputSystem.Player.Move.ReadValue<Vector2>() * Time.unscaledDeltaTime);
			}

			if (inputSystem.Player.MiddleClick.WasReleasedThisFrame()) holdMiddleClick = false;
			if (inputSystem.Player.MiddleClick.WasPressedThisFrame())
			{
				holdMiddleClick = true;
				middleClickMousePosition = mousePosition;
			}

			if (holdMiddleClick)
			{
				var delta = (middleClickMousePosition - mousePosition);
//				Debug.Log(delta);
				var topDownCamera = TopDownCamera.Instance;
				topDownCamera.Rotate(delta.x * Time.deltaTime / Mathf.Pow(Mathf.PI, 1));
				topDownCamera.Zoom(delta.y * Time.deltaTime / Mathf.Pow(Mathf.PI, 2));
			}
			//Debug.Log(delta);
		}
		else
		{
			Cursor.lockState = default;
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
		
		if (canvas.mouseOverViewport) // process mouse over
		{
			//var selectedCharacter = gameController.GetSelectedCharacter();
			var result = CursorRaycast(out var hit);
			if(debugCursor) Debug.Log("mouseOverViewport cursor result==" + result);
			if (result == CURSOR_RAYCAST_RESULT_NAVIGATION)
			{
				if (selectedCharacter != null)
				{
					if (CanControlCharacter(selectedCharacter)) // mouse over navigation
					{
						selectedCharacter.GetAgent().AssignDestination(hit.point);
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
						if (CanControlCharacter(selectedCharacter)) // mouse over character
						{
							var agent = selectedCharacter.GetAgent();
							if (overCharacter.InParty())
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
				else if (hit.collider.TryGetComponent(out PropComponent overProp)) // mouse over prop
				{
					if (selectedCharacter != null)
					{
						if (CanControlCharacter(selectedCharacter)) // mouse over prop
						{
							if (overProp.GetBestWaypoint(selectedCharacter.GetPosition(), out var bestWaypoint))
							{
								var agent = selectedCharacter.GetAgent();
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
	}

	private void OnApplicationFocus(bool value)
	{
		hasFocus = hasEditorFocus = value;
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