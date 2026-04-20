using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IngameDebugConsole;
using NUnit.Framework;
using Oddworm.Framework;
using UnityEngine;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameView : MonoBehaviour
{
	static public void Focus(bool value) => GetInstance().OnApplicationFocus(value);
	
	static bool debugCursor = false;
	public const int CURSOR_RAYCAST_RESULT_NONE = 0;
	public const int CURSOR_RAYCAST_RESULT_NAVIGATION = 1;
	public const int CURSOR_RAYCAST_RESULT_CHARACTER = 2;
	
	override public string ToString() => "GameView";

	
	static private GameView instance;
	static public GameView GetInstance()
	{
		return instance;
	}
	
	private GameTurnManager gameTurnManager;
	
	void SelectionChanged()
	{
		var selectedCharacter = CharacterSelection.GetSelectedCharacter();
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
		name = ToString();
		
		gameTurnManager = GameTurnManager.Instance;
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.RightClick.performed += ctx => RightClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => CameraZoom(-ctx.ReadValue<Vector2>().y);
		inputSystem.Player.RotateLeft.performed += ctx => CameraRotate(false);
		inputSystem.Player.RotateRight.performed += ctx => CameraRotate(true);
		inputSystem.Player.EndTurn.performed += ctx => ClickEndTurn();
		inputSystem.Player.Action_Run.performed += ctx => ClickRunAction();
		inputSystem.Player.SelectNext.performed += ctx => ClickSelectNext();
		inputSystem.Player.FocusSelected.performed += ctx => ClickFocusSelected();
		
		CharacterSelection.selectionChanged += SelectionChanged;
		
		FadeScreen.FadeIn(true);
	}

	void CameraRotate(bool left)
	{
		if(hasFocus)cameraPivot.ClampedRotation(left ? -1 : 1);
	}
	
	void CameraZoom(float delta)
	{
		if (hasFocus) cameraPivot.Zoom(delta);
	}

	[SerializeField] private List<string> scenesToLoad = new List<string>();

	private bool isLoadingScenes;
	
	private void Start()
	{
		if (!Application.isEditor) OnApplicationFocus(true);
		canvas.UpdateUI(this);
		SetInputEnabled(false);
	}

	public void ClickCharacter(CharacterAnimation clickedCharacterAnimation)
	{
		Debug.Log(this + " ClickCharacter " + clickedCharacterAnimation);
		var selectedCharacter = CharacterSelection.GetSelectedCharacter();
		var clickedCharacter = clickedCharacterAnimation.GetController();
	//	//var selectedCharacter = gameController.GetSelectedCharacter();
		if (clickedCharacter.InParty())
		{
			if (gameTurnManager.IsPlayerTurn())
			{
				CharacterSelection.SelectCharacter(clickedCharacter); // ClickCharacter select party character
				return;
			}
			
			Debug.Log("ClickCharacter: not in player turn");
			return;
		}
		else if (clickedCharacter.IsEnemyToPlayer(clickedCharacter))
		{
			if (selectedCharacter != null)
			{
				// assign attack
				if (selectedCharacter.CanAttack(false))
				{
					selectedCharacter.Routines.AssignAttack(clickedCharacter, false, true);
					return;
				}
				
				selectedCharacter.FloatingText_Cant();
				return;
			}
		}
		else
		{
			if (selectedCharacter != null)
			{
				Debug.Log("TODO " + selectedCharacter + " interact with " + clickedCharacter); //TODO
				return;
			}
		}
		
		Debug.Log("ClickCharacter: ?");
	}

	public void ClickProp(PropComponent clickedProp) // TODO Prop Controller
	{
		Debug.Log(this + " ClickProp " + clickedProp);
		var selectedCharacter = CharacterSelection.GetSelectedCharacter();
		//var selectedCharacter = gameController.GetSelectedCharacter();
		if (selectedCharacter != null && CanControlCharacter(selectedCharacter))
		{
			selectedCharacter.Routines.AssignInteract(clickedProp);
			return;
		}
		
		Debug.Log("ClickProp: no character selected");
	}

	public void ClickNavigation(Vector3 navPosition)
	{
		Debug.Log(this + " ClickNavigation " + navPosition);
		var selectedCharacter = CharacterSelection.GetSelectedCharacter();
		//var selectedCharacter = gameController.GetSelectedCharacter();
		if (selectedCharacter != null && CanControlCharacter(selectedCharacter))
		{
			selectedCharacter.Routines.AssignMoveToDestination(navPosition);
			return;
		}
		
		Debug.Log("ClickNavigation: no character selected");
	}

	public GameCanvas canvas => GameCanvas.Instance;

	public List<CharacterAnimation> GetAllCharacters() =>  CharacterAnimation.activeInstances.ToList();

	public List<CharacterAnimation> GetPartyCharacters() => CharacterAnimation.activeInstances.Where(c => c.GetController().InParty()).ToList();

	public int GetPartyCharacterIndex(CharacterAnimation characterAnimation) => GetPartyCharacters().IndexOf(characterAnimation);
	public int GetCharacterIndex(CharacterAnimation characterAnimation) => GetAllCharacters().IndexOf(characterAnimation);

	//public bool drawDebug { get; set; }
	
	//[UnityEngine.Range(0f,1f)] public float debugGUI = 0f;
	//public float cameraSpeed = 1.0f;
	//[SerializeField] private float cameraDeltaY = 1f;
	[UnityEngine.Range(0.01f, 0.50f)] public float maxNavDistance = 0.50f;

	[Header("Debug")]
	[ReadOnly][SerializeField] private bool hasFocus;
	[ReadOnly][SerializeField] private Vector2 mousePosition;
	private Vector2 lastMousePosition;

	private CameraPivot cameraPivot => CameraPivot.Instance; // TODO
	
	private Vector2 middleClickMousePosition;

	private bool holdMiddleClick, mouseHasMoved;
	public InputSystem_Actions inputSystem { get; private set; }

	public void MouseMove(Vector2 pos)
	{
		mousePosition = pos;
		mouseHasMoved = true;
	}

	public void RightClick(float value)
	{
		if (value > 0) // pressed
		{
			
			if (gameTurnManager.IsPlayerTurn() && !CharacterRoutines.AnyActiveRoutines())
			{
				CharacterSelection.SelectCharacter(null); // deselect all
			}
		}
	}

	public void LeftClick(float value)
	{
	//	Debug.Log("LeftClick: " + value);

		if (hasFocus && canvas.mouseOverViewport && value > 0f) // pressed
		{
			//hasEditorFocus = true;
			
			var raycastResult = CursorRaycast(out var hit);
			if (raycastResult == CURSOR_RAYCAST_RESULT_CHARACTER)
			{
				if (hit.collider.TryGetComponent(out CharacterAnimation clickedCharacter)) // click character
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

	

	public bool ShouldEndTurn() => CanEndTurn() && CharacterController.GetPartyCharacters().All(pc =>
	{
		return !pc.HasActions() && pc.GetRemainingMovePoints() < 0.5f;
	});
	
	public bool CanEndTurn() => !CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsPlayerTurn();

	

	public void ClickEndTurn()
	{
		if (gameTurnManager.IsPlayerTurn())
		{
			gameTurnManager.EndTurn();
		}
		else Debug.Log("not in player turn");
	}
	

	public bool CanUseRunAction() =>
		!CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsPlayerTurn() && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter.CanUseRunAction();

	public bool IsControlPanelVisible() =>
		gameTurnManager.IsPlayerTurn() && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter.IsControllableByPlayer();

	//public CharacterController GetPartyCharacter(int index) => CharacterController.GetPartyCharacter(index);

	public void ClickFocusSelected()
	{
		if (gameTurnManager.IsPlayerTurn())
		{
			if(CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
				cameraPivot.Focus(selectedCharacter);
			else Debug.Log("no selected character");
		}
		else Debug.Log("not in player turn");
	}
	
	public void ClickSelectNext()
	{
		if (gameTurnManager.IsPlayerTurn())
		{
			CharacterSelection.SelectNextCharacter();
		}
		else Debug.Log("not in player turn");
	}
	
	public void ClickRunAction()
	{
		CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter);
		if (selectedCharacter == null)
		{
			Debug.Log("no selected character!");
			return;
		}
		
		if (selectedCharacter.CanUseRunAction())
		{
			selectedCharacter.Routines.AssignRunAction();
		}
		else
		{
			selectedCharacter.FloatingText_Cant();
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

	public bool CanControlSelectedCharacter() => CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter && CanControlCharacter(selectedCharacter);
	public bool CanControlCharacter(CharacterController character)
	{
		return hasFocus && !CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsPlayerTurn() && character.IsControllableByPlayer();
	}
	
	

//	private bool HasFocus() => hasFocus && (!Application.isEditor || hasEditorFocus);
	
	private void LateUpdate()
	{
		canvas.UpdateUI(this); //TODO remove
		lastMousePosition = mousePosition;
		mouseHasMoved = false;
		mouseDown = false;
	}

	[ReadOnly] [SerializeField] private bool mouseDown, orbitWithInputs;
	[ReadOnly][SerializeField] private Vector2 orbitAxis;
	[ReadOnly][SerializeField] private Vector2 orbitDelta;
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

		if (DebugGUI.IsEnabled)
		{
			OnApplicationFocus(false);
		}
		
		if (hasFocus)
		{
			mouseDown = inputSystem.UI.Click.IsPressed();
			
			SetInputEnabled(!isLoadingScenes && !IngameDebugConsole.DebugLogManager.Instance.IsLogWindowVisible);
			
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = !holdMiddleClick;
			SetCurrentCursorType(CursorType.DefaultArrow);
			//Cursor.SetCursor(Resources.Load<Texture2D>("Cursors/cursor_none"), Vector2.zero, CursorMode.Auto);
		
			if (inputSystem.Player.Move.IsPressed())
			{
				cameraPivot.MoveFromInput(inputSystem.Player.Move.ReadValue<Vector2>() * Time.unscaledDeltaTime);
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

			orbitAxis = inputSystem.Player.Orbit.ReadValue<Vector2>();
			orbitWithInputs = orbitAxis.magnitude >= Consts.MinMagnitude;
			
			if (orbitWithInputs)
			{
				orbitDelta = orbitAxis;
				orbitDelta *= Time.unscaledDeltaTime;
				cameraPivot.Orbit(orbitDelta.x, orbitDelta.y, true);
			}
			else if (holdMiddleClick)
			{
				if (cameraPivot.OrbitFromScreenCenter())
				{
					orbitDelta.x = Screen.width / 2f - mousePosition.x;
					orbitDelta.y = Screen.height / 2f - mousePosition.y;
				}
				else
				{
					orbitDelta.x = (middleClickMousePosition - mousePosition).x;
					orbitDelta.y = (middleClickMousePosition - mousePosition).y;
				}
				
				if (cameraPivot.OrbitCanZoom())
				{
//						Debug.Log(this + " try orbit " + deltaX + " " + deltaY);
					if (Mathf.Abs(orbitDelta.x) > Mathf.Abs(orbitDelta.y))
					{
						orbitDelta.x = 0;
						orbitDelta.y = (lastMousePosition - mousePosition).y;
					}
					else
					{
						orbitDelta.y = 0;
					}
				}
				else
				{
					orbitDelta.y = (lastMousePosition - mousePosition).y;
				}
				
				orbitDelta.x /= Screen.width;
				orbitDelta.y /= Screen.height;
				cameraPivot.Orbit(orbitDelta.x, orbitDelta.y, false);
				
				mouseHasMoved = true;
				cameraPivot.OrbitDrag((middleClickMousePosition - mousePosition) * Time.unscaledDeltaTime);
			}
			else
			{
				cameraPivot.TryPanWithMouse(mousePosition, Time.unscaledDeltaTime);
			}
		}
		else
		{
			SetInputEnabled(false);
			
			Cursor.lockState = default;
			Cursor.visible = true;
			Cursor.SetCursor(null,default,default);
		}

		CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter);
		if (selectedCharacter != null)
		{
			if (selectedCharacter.IsDead())
			{
				CharacterSelection.SelectNextCharacter();
			}
		}
		
		if (hasFocus && canvas.mouseOverViewport && !holdMiddleClick) // mouse over
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
						SetCurrentCursorType(CursorType.Navigate);
						selectedCharacter.GetAgent().AssignDestination(hit.point, mouseHasMoved); // mouse over navigation
					}
					else if(debugCursor) Debug.Log("character not controllable");
				}
				else if(debugCursor) Debug.Log("no selected character");
			}
			else if (result == CURSOR_RAYCAST_RESULT_CHARACTER) 
			{
				if (hit.collider.TryGetComponent(out CharacterAnimation overCharacterView))
				{
					var overCharacter = overCharacterView.GetController();
					if (selectedCharacter != null)
					{
						if (CanControlCharacter(selectedCharacter))
						{
							var agent = selectedCharacter.GetAgent();
							if (overCharacter.InParty())
							{
								agent.ClearDestination();// mouse over another party member
								SetCurrentCursorType(CursorType.Select);
							}
							else if (overCharacter.IsEnemyToPlayer())
							{
								agent.AssignDestination(overCharacter.GetPosition(), mouseHasMoved); // mouse over enemy
								SetCurrentCursorType(CursorType.Attack);
							}
							else
							{
								agent.AssignDestination(overCharacter.GetPosition(), mouseHasMoved); // mouse over character
								SetCurrentCursorType(CursorType.Talk);
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
								agent.AssignDestination(bestWaypoint.GetPosition(), mouseHasMoved); // mouse over prop
								SetCurrentCursorType(overProp.GetCursorType());
							}
							//else Debug.LogWarning("no waypoint found");
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

		if (CharacterRoutines.AnyActiveRoutines())
		{
			SetCurrentCursorType(CursorType.WaitArrow);
		}

		if (isLoadingScenes)
		{
			SetCurrentCursorType(CursorType.Waiting);
		}
		
		if(Cursor.lockState != CursorLockMode.None)
			CursorData.GetCursorData(currentCursor).Apply(mouseDown);
	}

	private CursorType currentCursor;
	void SetCurrentCursorType(CursorType cursorType)
	{
		currentCursor = cursorType;
	}

	public void OnApplicationFocus(bool value)
	{
		if (DebugGUI.IsEnabled || DebugLogManager.Instance.IsLogWindowVisible)
			value = false;
		
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
		//const float debugOffset = 0.1f;
		
		//TODO: if mouse over viewport
		hit = default(RaycastHit);
		var cam = Camera.main;
		var ray = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(Consts.LAYER_CHARACTER)))
		{ 
			//DebugGUI.Log("Hit Character: " + hit.collider);
			//DrawDebug.WireCapsule(hit.collider.transform.position, Quaternion.identity, 0.5f, 1.5f, Color.green);
			//Utils.DrawDebugCircle(hit.collider.transform.position, Vector3.up, 0.5f);

			foreach (var cursorHandler in hit.collider.GetComponents<ICursorHandler>().Concat(hit.collider.GetComponentsInChildren<ICursorHandler>()))
			{
				cursorHandler.CursorOver();
			}

			return CURSOR_RAYCAST_RESULT_CHARACTER;
		}
		
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(Consts.LAYER_GROUND, Consts.LAYER_WALL)))
		{
			//DebugGUI.Log("Hit Navigation: " + hit.point);
			//Utils.DrawDebugCircle(hit.point, Vector3.up, maxNavDistance);

			if (NavUtility.TryGetNearestNavPosition(hit.point, maxNavDistance, out var navPosition))
			{
				hit.point = navPosition;
				//DrawDebug.WireDisc(navPosition + Vector3.up * debugOffset, Vector3.up,maxNavDistance * 0.5f, Color.cyan);

				return CURSOR_RAYCAST_RESULT_NAVIGATION;
			}
			
			// mouse over something else
			//DrawDebug.WireDisc(hit.point, Quaternion.LookRotation(hit.normal) * Vector3.up,maxNavDistance, Color.gray);
		}

		return CURSOR_RAYCAST_RESULT_NONE;
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