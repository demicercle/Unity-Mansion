using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

public interface ICursorHandler
{
	void CursorOver();
}

[System.Serializable]
public class GameInput : MonoBehaviour
{
	public static int debugMode { get; set; }

	
	public const int CursorRaycastResult_None = 0;
	public const int CursorRaycastResult_Navigation = 1;
	public const int CursorRaycastResult_Character = 2;
	
	public static bool IsConsoleVisible() => DebugLogManager.Instance.IsLogWindowVisible;
	public static void Focus(bool value) => instance.OnApplicationFocus(value);
	
	public static GameInput Instance => instance;
	
	[Header("Camera Control")]
	[SerializeField] [Range(0f, 6f)] private float inputOrbitScale = 3.14f;
	[SerializeField] private bool orbitCameraFromCenterScreen = false;
	[SerializeField] private bool orbitCameraY = false;
	[SerializeField] private bool cameraCanRotateClamped = false;
	[Header("Character Control")]
	[Range(0.01f, 0.99f)] public float maxNavDistance = 0.50f;
	
	// Raycast result 0=None, 1=Navigation, 2=Character (or Prop)
	public int CursorRaycast(out RaycastHit raycastHit, Camera cam = null) 
	{
		raycastHit = default(RaycastHit);
		
		if (!cam) cam = Camera.main;
		if (!cam)
		{
			Debug.LogWarning(this + "cannot do cursor raycast because no main camera");
			return CursorRaycastResult_None;
		}
		
		raycastRay = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(raycastRay, out raycastHit, 100f, LayerMask.GetMask(Consts.LAYER_CHARACTER)))
		{ 
			//DebugGUI.Log("Hit Character: " + hit.collider);
			//DrawDebug.WireCapsule(hit.collider.transform.position, Quaternion.identity, 0.5f, 1.5f, Color.green);
			//Utils.DrawDebugCircle(hit.collider.transform.position, Vector3.up, 0.5f);

			foreach (var cursorHandler in raycastHit.collider.GetComponents<ICursorHandler>().Concat(raycastHit.collider.GetComponentsInChildren<ICursorHandler>()))
			{
				cursorHandler.CursorOver();
			}

			return CursorRaycastResult_Character;
		}
		
		if (Physics.Raycast(raycastRay, out raycastHit, 100f, LayerMask.GetMask(Consts.LAYER_GROUND, Consts.LAYER_WALL)))
		{
			//DebugGUI.Log("Hit Navigation: " + hit.point);
			//Utils.DrawDebugCircle(hit.point, Vector3.up, maxNavDistance);

			if (NavUtility.TryGetNearestNavPosition(raycastHit.point, maxNavDistance, out var navPosition))
			{
				raycastHit.point = navPosition;
				//DrawDebug.WireDisc(navPosition + Vector3.up * debugOffset, Vector3.up,maxNavDistance * 0.5f, Color.cyan);

				return CursorRaycastResult_Navigation;
			}
			
			// mouse over something else
			//DrawDebug.WireDisc(hit.point, Quaternion.LookRotation(hit.normal) * Vector3.up,maxNavDistance, Color.gray);
		}
		
		return CursorRaycastResult_None;
	}

	#region PRIVATE
	private static GameInput instance;
	
	private bool isLoading => gameTurnManager.IsLoading();
	private GameCanvas canvas => GameCanvas.Instance;
	private CameraPivot cameraPivot => CameraPivot.Instance;
	private bool isDebugMode => debugMode > 0;
	private GameTurnManager gameTurnManager;
	private bool mouseDown, orbitWithInputs;
	private Vector2 orbitAxis;
	private Vector2 orbitDelta;
	private float leftClickValue, rightClickValue, ctxValue;
	private bool hasNoActionsCursor;
	private bool hasFocus;
	private Vector2 mousePosition;
	private Vector2 lastMousePosition;
	private Vector2 middleClickMousePosition;
	private bool holdMiddleClick, mouseHasMoved, mouseOverViewport;
	private InputSystem_Actions inputSystem;
	private bool inputEnabled;
	private CursorType currentCursor;
	private Ray raycastRay;
	private int lastCursorResult;
	private string debugString;

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogError(this + " duplicated!");
			return;
		}

		Debug.Log(this + " Awake");
		instance = this;

		gameTurnManager = GameTurnManager.Instance;
		if (!gameTurnManager) gameTurnManager = Utils.FindFirstObject<GameTurnManager>();
		if (!gameTurnManager) Debug.LogError(this + " cannot find GameTurnManager!");

		hasNoActionsCursor = CursorData.HasCursor(CursorType.NoActions);
	}
	
	private void OnEnable()
	{
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += OnInputLeftClick;
		inputSystem.UI.RightClick.performed += OnInputRightClick;
		inputSystem.UI.Point.performed += OnInputMouseMove;
		inputSystem.UI.ScrollWheel.performed += OnInputCameraZoom;
		inputSystem.Player.RotateLeft.performed += OnInputCameraRotateLeft;
		inputSystem.Player.RotateRight.performed += OnInputCameraRotateRight;
		inputSystem.Player.EndTurn.performed += OnInputEndTurn;
		inputSystem.Player.SelectNext.performed += OnInputSelectNext;
		inputSystem.Player.FocusSelected.performed += OnInputFocusSelected;
		inputSystem.Player.Action_Run.performed += OnInputAction_Run; // TODO other actions inputs?
		
		CharacterSelection.selectionChanged += SelectionChanged;

		DebugLogManager.Instance.OnLogWindowShown += OnLogWindowShown;
		DebugLogManager.Instance.OnLogWindowHidden += OnLogWindowHidden;

		canvas.onViewportClicked += OnClickedViewport;
		canvas.onViewportMouseOver += OnMouseOverViewport;
	}

	private void OnDisable()
	{
		if (!Application.isPlaying) return;
		
		inputSystem.UI.Click.performed -= OnInputLeftClick;
		inputSystem.UI.RightClick.performed -= OnInputRightClick;
		inputSystem.UI.Point.performed -= OnInputMouseMove;
		inputSystem.UI.ScrollWheel.performed -= OnInputCameraZoom;
		inputSystem.Player.RotateLeft.performed -= OnInputCameraRotateLeft;
		inputSystem.Player.RotateRight.performed -= OnInputCameraRotateRight;
		inputSystem.Player.EndTurn.performed -= OnInputEndTurn;
		inputSystem.Player.Action_Run.performed -= OnInputAction_Run;
		inputSystem.Player.SelectNext.performed -= OnInputSelectNext;
		inputSystem.Player.FocusSelected.performed -= OnInputFocusSelected;
		
		CharacterSelection.selectionChanged -= SelectionChanged;

		DebugLogManager.Instance.OnLogWindowShown -= OnLogWindowShown;
		DebugLogManager.Instance.OnLogWindowHidden -= OnLogWindowHidden;
		
		canvas.onViewportClicked -= OnClickedViewport;
		canvas.onViewportMouseOver -= OnMouseOverViewport;
	}

	private IEnumerator Start()
	{
		if (!cameraPivot) Debug.LogError(this + " has no camera pivot!");
		if (!canvas) Debug.LogError(this + " has no canvas!");
		
		if (!Application.isEditor)
		{
			OnApplicationFocus(true);
		}
		
		SetInputEnabled(false);

		do
		{
			yield return null;
		} while (isLoading);

		SetInputEnabled(true);
	}

	private void OnApplicationFocus(bool value)
	{
		if (/*DebugGUI.IsEnabled || */DebugLogManager.Instance.IsLogWindowVisible)
			value = false;
		
		if (hasFocus != value)
		{
			Debug.Log("OnApplicationFocus: " + value);
			hasFocus = value;
			SetInputEnabled(hasFocus && !IsConsoleVisible());
		}
	}
	
	private void Update()
	{
		if (isLoading)
			return;
		
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
			mouseDown = inputSystem.UI.Click.IsPressed();
			
			Cursor.lockState = CursorLockMode.Confined;
			Cursor.visible = !holdMiddleClick;
			
			SetCurrentCursorType(CursorType.DefaultArrow);
		
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
				cameraPivot.Orbit(orbitDelta.x, orbitDelta.y, inputOrbitScale);
			}
			else if (holdMiddleClick)
			{
				if (orbitCameraFromCenterScreen)
				{
					orbitDelta.x = Screen.width / 2f - mousePosition.x;
					orbitDelta.y = Screen.height / 2f - mousePosition.y;
				}
				else
				{
					orbitDelta.x = (middleClickMousePosition - mousePosition).x;
					orbitDelta.y = (middleClickMousePosition - mousePosition).y;
				}
				
				if (orbitCameraY)
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
				cameraPivot.Orbit(orbitDelta.x, orbitDelta.y, 1f);
				
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

		debugString = mousePosition.ToString() + " " + (int)leftClickValue + (int)(holdMiddleClick?1:0) + (int)rightClickValue;
		if (hasFocus && mouseOverViewport && !holdMiddleClick) // mouse over
		{
			lastCursorResult = CursorRaycast(out var hit);

			debugString += "\nresult==" + lastCursorResult + " " + (hit.collider ? hit.collider.name : "no hit");
			
			if (lastCursorResult == CursorRaycastResult_Navigation)
			{
				if (selectedCharacter != null)
				{
					if (CanControlCharacter(selectedCharacter))
					{
						SetCurrentCursorType(CursorType.Navigate);
						selectedCharacter.GetAgent().AssignDestination(hit.point, mouseHasMoved); // mouse over navigation
						debugString += "\n>Navigate";
					}
					//else if(isDebugMode) Debug.Log("character not controllable");
				}
				//else if(isDebugMode) Debug.Log("no selected character");
			}
			
			else if (lastCursorResult == CursorRaycastResult_Character) 
			{
				if (hit.collider.TryGetComponent(out CharacterController overCharacter))
				{
					debugString += " (Character)";
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
								agent.AssignDestination(overCharacter.GetPosition(), true); // mouse over enemy
								SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions : CursorType.Attack);
							}
							else
							{
								agent.AssignDestination(overCharacter.GetPosition(), true); // mouse over character
								SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions : CursorType.Talk);
							}
							debugString += "\n>" + currentCursor;
						}
						//else if(isDebugMode) Debug.Log("character not controllable");
					}
					//else if(isDebugMode) Debug.Log("no selected character");
				}
				else if (hit.collider.TryGetComponent(out PropComponent overProp)) 
				{
					debugString += " (Prop)";
					if (selectedCharacter != null)
					{
						if (CanControlCharacter(selectedCharacter)) // mouse over prop
						{
							if (overProp.GetBestWaypoint(selectedCharacter.GetPosition(), out var bestWaypoint))
							{
								var agent = selectedCharacter.GetAgent();
								agent.AssignDestination(bestWaypoint.GetPosition(), true); // mouse over prop
								SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions
									: overProp.GetCursorType());
							}
							debugString += "\n>" + currentCursor;
							//else Debug.LogWarning("no waypoint found");
						}
						//else if(isDebugMode) Debug.Log("character not controllable");
					}
					//else if(isDebugMode) Debug.Log("no selected character");
				}
			}
			
			else // mouse over raycast no result
			{
				if (selectedCharacter)
				{
					if (CanControlCharacter(selectedCharacter))
					{
						selectedCharacter.GetAgent().ClearDestination();
						debugString += "\n>ClearDestination()";
					}
					//else if(isDebugMode) Debug.Log("character not controllable");
				}
				//else if(isDebugMode) Debug.Log("no selected character");
			}
			
			//if (debugMode) Debug.Log(debugString);
		}
		else
		{
			if (selectedCharacter && CanControlCharacter(selectedCharacter))
			{
				selectedCharacter.GetAgent().ClearDestination(); // to hide path drawing
			}
		}

		if (CharacterRoutines.AnyActiveRoutines())
		{
			SetCurrentCursorType(CursorType.Waiting);
		}

		if (isLoading)
		{
			SetCurrentCursorType(CursorType.Loading);
		}

		if (Cursor.lockState != CursorLockMode.None)
		{
			CursorData.GetCursorData(currentCursor).Apply(mouseDown);
		}
		
		if (debugMode > 1) Debug.Log(debugString);
	}
	
	private void LateUpdate()
	{
		lastMousePosition = mousePosition;
		mouseHasMoved = false;
		mouseDown = false;
	}

	// KEY INPUT
	
	private void OnInputEndTurn(CallbackContext ctx) => gameTurnManager.TryEndPlayerTurn();

	private void OnInputAction_Run(CallbackContext ctx)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
		{
			selectedCharacter.TryUseAction(CharacterAction.RunAction, true);
		}
	}

	private void OnInputCameraRotateLeft(CallbackContext ctx)
	{
		if(hasFocus && cameraCanRotateClamped)cameraPivot.ClampedRotation(-1);
	}

	private void OnInputCameraRotateRight(CallbackContext ctx)
	{
		if(hasFocus && cameraCanRotateClamped)cameraPivot.ClampedRotation(1);
	}
	
	private void OnInputCameraZoom(CallbackContext ctx)
	{
		if (hasFocus) cameraPivot.Zoom(-ctx.ReadValue<Vector2>().y);
	}
	
	private void OnInputFocusSelected(CallbackContext ctx)
	{
		if (gameTurnManager.IsPlayerTurn())
		{
			if(CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
				cameraPivot.Focus(selectedCharacter);
			else Debug.Log("no selected character");
		}
		else Debug.Log("not in player turn");
	}
	
	private void OnInputSelectNext(CallbackContext ctx)
	{
		if (gameTurnManager.IsPlayerTurn())
		{
			CharacterSelection.SelectNextCharacter();
		}
		else Debug.Log("not in player turn");
	}
	
	// MOUSE INPUT
	
	private void OnInputMouseMove(CallbackContext ctx)
	{
		mousePosition = ctx.ReadValue<Vector2>();
		mouseHasMoved = true;
	}

	private void OnInputRightClick(CallbackContext ctx)
	{
		ctxValue = ctx.ReadValue<float>();
		
		if (ctxValue > 0) // right click PRESSED
		{
			if (!hasFocus || !mouseOverViewport)
				return;
		}
		
		if (ctxValue <= 0 && rightClickValue > 0) // right click RELEASED
		{
			if (gameTurnManager.IsPlayerTurn() && !CharacterRoutines.AnyActiveRoutines())
			{
				CharacterSelection.SelectCharacter(null); // deselect all
			}
		}
		
		rightClickValue = ctxValue;
	}
	
	private void OnInputLeftClick(CallbackContext ctx)
	{
		ctxValue = ctx.ReadValue<float>();

		if (ctxValue > 0) // left click PRESSED
		{
			if (!hasFocus || !mouseOverViewport)
				return;
		}

		if (hasFocus && ctxValue <= 0 && leftClickValue > 0) // left click RELEASED
		{
			if (!mouseOverViewport) return;
			
			var raycastResult = CursorRaycast(out var hit);
			if (raycastResult == CursorRaycastResult_Character)
			{
				if (hit.collider.TryGetComponent(out CharacterController clickedCharacter)) // click character
				{
					ClickCharacter(clickedCharacter);
				}

				if (hit.collider.TryGetComponent(out PropComponent clickedProp)) // click prop
				{
					ClickProp(clickedProp);
				}
			}
			else if (raycastResult == CursorRaycastResult_Navigation) // click navigation
			{
				ClickNavigation(hit.point);
			}
		}

		leftClickValue = ctxValue;
	}

	private void OnMouseOverViewport(bool value) => mouseOverViewport = value;
	
	private void OnClickedViewport() => OnApplicationFocus(true);
	
	// Callbacks
	
	private void OnLogWindowShown() => SetInputEnabled(false);
	
	private void OnLogWindowHidden() => SetInputEnabled(hasFocus);

	private void SelectionChanged()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
		{
			cameraPivot.Focus(selectedCharacter);
		}
	}
	
	// others
	
	private void SetInputEnabled(bool value)
	{
		if (inputEnabled != value)
		{
			inputEnabled = value;
			Debug.Log(" inputEnabled=" + inputEnabled);
			
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

	private void ClickCharacter(CharacterController clickedCharacter)
	{
		Debug.Log(this + " ClickCharacter " + clickedCharacter);
		
		var selectedCharacter = CharacterSelection.GetSelectedCharacter();
		
		if (clickedCharacter.InParty())
		{
			if (gameTurnManager.IsPlayerTurn())
			{
				CharacterSelection.SelectCharacter(clickedCharacter); // ClickCharacter select party character
				//return;
			}
			else if(isDebugMode) Debug.Log(clickedCharacter + " InParty not in player turn");
		}
		
		else if (clickedCharacter.IsEnemyToPlayer(clickedCharacter))
		{
			if (selectedCharacter != null)
			{
				// assign attack
				if (selectedCharacter.CanAttack(false))
				{
					if(hasNoActionsCursor && !selectedCharacter.CanUseAction(CharacterAction.AttackAction))
						selectedCharacter.FloatingText_Cant();
					else
						selectedCharacter.Routines.AssignAttack(clickedCharacter, false, true);
					return;
				}
				
				selectedCharacter.FloatingText_Cant();
			}
			else if(isDebugMode)  Debug.Log(clickedCharacter + " IsEnemyToPlayer no selected character");
		}
		
		else
		{
			if (selectedCharacter)
			{
				Debug.LogWarning("//TODO " + selectedCharacter + " interact with " + clickedCharacter); //TODO
			}
			else if(isDebugMode)  Debug.Log(clickedCharacter + " !IsEnemyToPlayer no selected character");
		}
	}

	private void ClickProp(PropComponent clickedProp) // TODO Prop Controller
	{
		Debug.Log(this + " ClickProp " + clickedProp);

		if (CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
		{
			if (CanControlCharacter(selectedCharacter))
			{
				if(hasNoActionsCursor && !selectedCharacter.CanUseAction(CharacterAction.InteractAction))
					selectedCharacter.FloatingText_Cant();
				else
					selectedCharacter.Routines.AssignInteract(clickedProp);
			}
			else if(isDebugMode)  Debug.Log(" selected character not controllable");
		}
		else if(isDebugMode)  Debug.Log(" no character selected");
	}

	private void ClickNavigation(Vector3 navPosition)
	{
		Debug.Log(this + " ClickNavigation " + navPosition);

		if (CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
		{
			if (CanControlCharacter(selectedCharacter))
			{
				selectedCharacter.Routines.AssignMoveToDestination(navPosition);
			}
			else if(isDebugMode)  Debug.Log(" selected character not controllable");
		}
		
		else if(isDebugMode)  Debug.Log(" no character selected");
	}

	private List<CharacterAnimation> GetAllCharacters() =>  CharacterAnimation.activeInstances.ToList();
	private List<CharacterAnimation> GetPartyCharacters() => CharacterAnimation.activeInstances.Where(c => c.GetController().InParty()).ToList();
	private int GetPartyCharacterIndex(CharacterAnimation characterAnimation) => GetPartyCharacters().IndexOf(characterAnimation);
	private int GetCharacterIndex(CharacterAnimation characterAnimation) => GetAllCharacters().IndexOf(characterAnimation);
	private bool CanControlSelectedCharacter() => CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter && CanControlCharacter(selectedCharacter);
	private bool CanControlCharacter(CharacterController character)
	{
		return hasFocus && !CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsPlayerTurn() && character.IsControllableByPlayer();
	}
	private void SetCurrentCursorType(CursorType cursorType)
	{
		currentCursor = cursorType;
	}

	private void OnGUI()
	{
		if (!isDebugMode) return;
		var guiContent = new GUIContent(debugString);
		var guiStyle = new GUIStyle("textfield");
		var guiRect = GUILayoutUtility.GetRect(guiContent, guiStyle);
		GUI.Box(new Rect(Event.current.mousePosition, guiRect.size), guiContent);
	}

	string GetLabel(string label)
	{
		#if UNITY_EDITOR
		return UnityEditor.ObjectNames.NicifyVariableName(label) + ": ";
		#else
		return label;
		#endif
	}
	#endregion
}
