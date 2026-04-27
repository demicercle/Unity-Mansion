using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;
using Object = UnityEngine.Object;

public interface ICursorHandler
{
	void CursorOver();
}

[System.Serializable]
public class GameInput : MonoBehaviour
{
	public static int debugMode { get; set; }

	public const float MaxMouseDeltaToClick = 3.0f;
	public const float AutoAssignDestinationToCLickedPropDistance = 1.0f;
	
	public const int CursorRaycastResult_None = 0;
	public const int CursorRaycastResult_Navigation = 1;
	public const int CursorRaycastResult_Interaction = 2;
	
	public static bool IsConsoleVisible() => DebugLogManager.Instance.IsLogWindowVisible;
	public static void Focus(bool value) => instance.OnApplicationFocus(value);
	
	public static GameInput Instance => instance;

	//[SerializeField] private bool debugLog;

	[SerializeField] private CircleRenderer navigationTarget;
	[Header("Camera Control")]
	[SerializeField] [Range(0f, 360f)] private float orbitSpeedAngle = 180f;
	[SerializeField] [Min(0f)] private float orbitSpeedInput = 3.0f;
	[SerializeField] private bool orbitCameraFromCenterScreen = false;
	[SerializeField] private bool orbitCameraY = false;
	[Header("Character Control")]
	[Range(0.01f, 0.99f)] public float maxNavDistance = 0.50f;

	public UnityEvent onClick, onSelect;
	
	// Raycast result 0=None, 1=Navigation, 2=Character (or Prop)
	public int CursorRaycast(out RaycastHit outRaycast, Camera cam = null)
	{
		outRaycast = default(RaycastHit);
		
		if (!cam) cam = Camera.main;
		if (!cam)
		{
			Debug.LogWarning(this + "cannot do cursor raycast because no main camera");
			outRaycast = default(RaycastHit);
			return CursorRaycastResult_None;
		}
		
		raycastRay = cam.ScreenPointToRay(mousePosition);
		
		hitInteractions = Physics.Raycast(raycastRay, out interactionHit, 100f, Layers.MaskInteraction);
		if (hitInteractions && RoomTrigger.IsPositionVisible(interactionHit.point))
		{ 
			if (itemToUse)
			{
				if (!itemToUse.CanBeUsedOn(interactionHit.collider.gameObject, out var _))
					return CursorRaycastResult_None;
			}
			
			outRaycast = interactionHit;
			
			foreach (var cursorHandler in interactionHit.collider.GetComponents<ICursorHandler>()
				         .Concat(interactionHit.collider.GetComponentsInChildren<ICursorHandler>()))
			{
				cursorHandler.CursorOver();
			}
			
			return CursorRaycastResult_Interaction;
		}
		
		hitNavigation = Physics.Raycast(raycastRay, out navigationHit, 100f, Layers.MaskNavigation);
		if (hitNavigation && RoomTrigger.IsPositionVisible(navigationHit.point))
		{
			if (itemToUse)
			{
				if (!itemToUse.CanBeUsedOn(navigationHit.collider.gameObject, out var _))
					return CursorRaycastResult_None;
			}
			
			if (NavUtils.TryGetNearestNavPosition(navigationHit.point, maxNavDistance, out var navPosition))
			{
				hitWalls = Physics.Linecast(navPosition, raycastRay.origin, out wallsHit, Layers.MaskWall);
				if (!hitWalls)
				{
					outRaycast = navigationHit;
					outRaycast.point = navPosition;
					
					return CursorRaycastResult_Navigation;
				}
				else if (debugMode > 0)
				{
					DebugDrawer.Line(navPosition, wallsHit.point, Color.red);
					DebugDrawer.Sphere(wallsHit.point, wallsHit.normal, Vector3.one * 0.1f, Color.red);
				}
			}
		}
		
		return CursorRaycastResult_None;
	}

	public bool IsLeaderMode() => !ungroupMode && !gameTurnManager.IsTurnBasedMode();

	#region PRIVATE
	private static GameInput instance;
	
	private bool isLoading => gameTurnManager.IsLoading();
	private GameCanvas canvas => GameCanvas.Instance;
	private CameraPivot cameraPivot => CameraPivot.Instance;
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;
	private bool isDebugMode => debugMode > 0;
	private CharacterEntity selectedCharacter;
	private CharacterAgent selectedAgent;
	private Collider lastMouseOverObject;
	private bool mouseOverChanged;
	private bool mouseDown, orbitWithInputs;
	private Vector2 orbitAxis;
	private Vector2 orbitDelta;
	private float leftClickValue, rightClickValue, ctxValue;
	private bool hasNoActionsCursor;
	private bool hasFocus;
	private Vector2 mousePosition;
	private Vector2 lastClickPosition;
	private Vector2 lastMousePosition;
	private Vector2 middleClickMousePosition;
	private bool holdMiddleClick, mouseHasMoved, mouseOverViewport;
	private InputSystem_Actions inputSystem;
	private bool inputEnabled;
	private CursorType currentCursor;
	private Ray raycastRay;
	private RaycastHit navigationHit, wallsHit, interactionHit;
	private bool hitNavigation, hitWalls, hitInteractions;
	private int lastCursorResult;
	private string debugString;
	private bool ungroupMode;
	private bool hasConsoleEvents;

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogError(this + " duplicated!");
			return;
		}

		Debug.Log(this + " Awake");
		instance = this;

		hasNoActionsCursor = CursorData.HasCursor(CursorType.NoActions);
		
		navigationTarget.gameObject.SetActive(false);
	}
	
	private void OnEnable()
	{
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += OnInputLeftClick;
		inputSystem.UI.RightClick.performed += OnInputRightClick;
		inputSystem.UI.Point.performed += OnInputMouseMove;
		inputSystem.UI.ScrollWheel.performed += OnInputCameraZoom;
		inputSystem.Player.SelectNext.performed += OnInputSelectNext;
		inputSystem.Player.FocusSelected.performed += OnInputFocusSelected;
		inputSystem.Player.ToggleGroup.performed += OnInputToggleGroup;
		inputSystem.Player.Action_Run.performed += OnInputAction_Run; // TODO other actions inputs?
		
		CharacterSelection.selectionChanged += SelectionChanged;

		if (DebugLogManager.Instance)
		{
			hasConsoleEvents = true;
			DebugLogManager.Instance.OnLogWindowShown += OnLogWindowShown;
			DebugLogManager.Instance.OnLogWindowHidden += OnLogWindowHidden;
		}

		canvas.onViewportClicked += OnClickedViewport;
		canvas.onViewportMouseOver += OnMouseOverViewport;
		
		onClick.AddListener(TryRestSelected);
	}

	private void OnDisable()
	{
		#if UNITY_EDITOR
		if (!Application.isPlaying) return;
		#endif
		
		inputSystem.UI.Click.performed -= OnInputLeftClick;
		inputSystem.UI.RightClick.performed -= OnInputRightClick;
		inputSystem.UI.Point.performed -= OnInputMouseMove;
		inputSystem.UI.ScrollWheel.performed -= OnInputCameraZoom;
		inputSystem.Player.EndTurn.performed -= OnInputEndTurn;
		inputSystem.Player.SelectNext.performed -= OnInputSelectNext;
		inputSystem.Player.FocusSelected.performed -= OnInputFocusSelected;
		inputSystem.Player.ToggleGroup.performed -= OnInputToggleGroup;
		inputSystem.Player.Action_Run.performed -= OnInputAction_Run;
		
		CharacterSelection.selectionChanged -= SelectionChanged;

		if (DebugLogManager.Instance && hasConsoleEvents)
		{
			hasConsoleEvents = false;
			DebugLogManager.Instance.OnLogWindowShown -= OnLogWindowShown;
			DebugLogManager.Instance.OnLogWindowHidden -= OnLogWindowHidden;
		}
		
		canvas.onViewportClicked -= OnClickedViewport;
		canvas.onViewportMouseOver -= OnMouseOverViewport;
		
		onClick.RemoveListener(TryRestSelected);
		
		Cursor.SetCursor(null, default, default);
		Cursor.lockState = default;
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
			if (hasFocus && inputSystem.Player.EscapeEditor.WasPressedThisFrame())
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

			if (inputSystem.Player.Escape.WasPressedThisFrame())
			{
				PauseCanvas.Instance.TogglePaused();
			}
		
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
				if(debugMode > 0) Debug.Log(this + " middleClickMousePosition=" + middleClickMousePosition);
			}

			if (inputSystem.Player.HighlightObjects.IsPressed())
			{
				OutlineRenderer.ForceHightlight();
			}

			orbitAxis = inputSystem.Player.Orbit.ReadValue<Vector2>();
			orbitWithInputs = (orbitAxis.magnitude+orbitDelta.magnitude) >= Consts.MinMagnitude;
			
			if (orbitWithInputs)
			{
				orbitDelta.x = orbitSpeedInput <= 0 ? orbitAxis.x : Mathf.Lerp(orbitDelta.x, orbitAxis.x, Time.unscaledDeltaTime * orbitSpeedInput);
				orbitDelta.y = orbitSpeedInput <= 0 ? orbitAxis.y :Mathf.Lerp(orbitDelta.y, orbitAxis.y, Time.unscaledDeltaTime * orbitSpeedInput);
				
				if(debugMode > 1)
					Debug.Log(this + " orbitDelta=" + orbitDelta + " orbitAxis=" + orbitAxis);
				
				if (!holdMiddleClick)
				{
					cameraPivot.Orbit(orbitDelta.x, orbitDelta.y, Time.unscaledDeltaTime * orbitSpeedAngle);
				}
			}
			
			if (holdMiddleClick)
			{
				if (orbitCameraFromCenterScreen)
				{
					orbitDelta.x = Screen.width / 2f - mousePosition.x;
				}
				else
				{
					orbitDelta.x = (middleClickMousePosition - mousePosition).x;
				}
				
				orbitDelta.y = 0;
				
				if (orbitCameraY)
				{
//						Debug.Log(this + " try orbit " + deltaX + " " + deltaY);
					if (Mathf.Abs(orbitDelta.x) > Mathf.Abs(orbitDelta.y))
					{
						orbitDelta.x = 0;

						if (orbitCameraFromCenterScreen)
						{
							orbitDelta.y = Screen.height / 2f - mousePosition.y;
						}
						else
						{
							orbitDelta.y = (middleClickMousePosition - mousePosition).y;
						}
					}
				}
				
				orbitDelta.x /= Screen.width;
				orbitDelta.y /= Screen.height;
				cameraPivot.Orbit(orbitDelta.x, orbitDelta.y);
				
				mouseHasMoved = true;
				//cameraPivot.OrbitDrag((middleClickMousePosition - mousePosition) * Time.unscaledDeltaTime);
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

		CharacterSelection.TryGetSelectedCharacter(out selectedCharacter);
		
		debugString = mousePosition.ToString() + " " + (int)leftClickValue + (int)(holdMiddleClick?1:0) + (int)rightClickValue;
		if (hasFocus && mouseOverViewport && !holdMiddleClick) ///////////////////////////////////////////////////////////////////////////// mouse over
		{
			lastCursorResult = CursorRaycast(out var hit);
			
			mouseOverChanged = lastMouseOverObject != hit.collider;
			lastMouseOverObject = hit.collider;

			debugString += "\nresult==" + lastCursorResult + " " + (hit.collider ? hit.collider.name : "no hit");
			
			if (lastCursorResult == CursorRaycastResult_Navigation)
			{
				if (selectedCharacter)
				{
					SetCurrentCursorType(CursorType.Navigate);
					debugString += "\n>Navigate";
					
					if (CanAssignOnMouseOver(selectedCharacter) && mouseHasMoved)
					{
						selectedCharacter.GetAgent().AssignDestination(hit.point, mouseHasMoved); // mouse over navigation
					}
				}
				//else if(isDebugMode) Debug.Log("no selected character");
			}
			
			else if (lastCursorResult == CursorRaycastResult_Interaction) 
			{
				if (hit.collider.TryGetComponent(out CharacterEntity overCharacter))
				{
					if (debugMode > 1) Debug.Log(this + " overCharacter=" + overCharacter);
					debugString += " (Character)";
					if (selectedCharacter != null)
					{
						var agent = selectedCharacter.GetAgent();
						if (overCharacter.InParty())
						{
							SetCurrentCursorType(CursorType.Select);
							
							if (CanAssignOnMouseOver(selectedCharacter))
								agent.ClearDestination();// mouse over another party member
							
						}
						else if (overCharacter.IsEnemyToPlayer())
						{
							SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions : CursorType.Attack);
							
							if (CanAssignOnMouseOver(selectedCharacter))
								agent.AssignDestination(overCharacter.GetPosition(), true); // mouse over enemy
						}
						else
						{
							SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions : CursorType.Talk);
							
							if (CanAssignOnMouseOver(selectedCharacter) && mouseOverChanged)
								agent.AssignDestination(overCharacter.GetPosition(), true); // mouse over character
						}
						debugString += "\n>" + currentCursor;
					}
					//else if(isDebugMode) Debug.Log("no selected character");
				} // TODO ISelectable, ITalkable
				else if (hit.collider.TryGetComponent(out PropComponent overProp))  // TODO IInteractable
				{
					if (debugMode > 1) Debug.Log(this + " overProp=" + overProp);
					debugString += " (Prop)";
					if (selectedCharacter != null)
					{
						SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions
							: overProp.GetCursorType());
							
						if (CanAssignOnMouseOver(selectedCharacter)) // mouse over prop
						{
							if (mouseOverChanged) // assign waypoint destination only if it changed
							{
								selectedAgent = selectedCharacter.GetAgent();
								selectedAgent.AssignWaypointDestinationAsync(overProp);
							}
						}
						debugString += "\n>" + currentCursor;
					}
					//else if(isDebugMode) Debug.Log("no selected character");
				}
				else if (hit.collider.TryGetComponent(out IAttackable overAttackable))
				{
					if (debugMode > 1) Debug.Log(this + " overAttackable=" + overAttackable);
					debugString += " (Attackable)";
					if (selectedCharacter != null)
					{
						SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions
							: CursorType.Attack);
							
						if (CanAssignOnMouseOver(selectedCharacter) && mouseOverChanged) // mouse over prop
						{
							selectedCharacter.GetAgent().AssignDestination(overAttackable.GetPosition(), true);
						}
						debugString += "\n>" + currentCursor;
					}
				}
			}
			
			else // mouse over raycast no result
			{
				if (selectedCharacter)
				{
					debugString += "\n>ClearDestination()";
					
					if (CanAssignOnMouseOver(selectedCharacter))
					{
						selectedCharacter.GetAgent().ClearDestination();
					}
				}
			}
		}
		else
		{
			if (selectedCharacter && CanAssignOnMouseOver(selectedCharacter))
			{
				selectedCharacter.GetAgent().ClearDestination(); // to hide path drawing
			}
		}

		if (mouseOverViewport)
		{
			if (lastCursorResult == CursorRaycastResult_Navigation && selectedCharacter &&
			    !selectedCharacter.GetAgent().isPathValid)
			{
				SetCurrentCursorType(CursorType.Blocked);
			}

			if (itemToUse)
			{
				if (itemToUse.CanBeUsedOn(lastMouseOverObject?.gameObject, out var _))
					SetCurrentCursorType(CursorType.UseItem);
				else
					SetCurrentCursorType(CursorType.Blocked);
			}

			if (CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsTurnBasedMode())
			{
				SetCurrentCursorType(CursorType.Waiting);
			}
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

	private void OnMouseEnter(Collider overCollider)
	{
		
	}

	private void OnMouseExit(Collider overCollider)
	{
		
	}
	
	private void LateUpdate()
	{
		navigationTarget.gameObject.SetActive(lastCursorResult == CursorRaycastResult_Navigation && mouseOverViewport && !isLoading);
		if(navigationTarget.gameObject.activeSelf) 
			navigationTarget.transform.position = navigationHit.point;
		
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
			if(selectedCharacter.CanUseAction(CharacterAction.Run))
				selectedCharacter.Routines.AssignRunAction();
		}
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
		else if(debugMode>0) Debug.Log("not in player turn");
	}

	private void OnInputToggleGroup(CallbackContext ctx)
	{
		if (gameTurnManager.IsTurnBasedMode()) ungroupMode = true;
		else if (ctx.ReadValueAsButton()) ungroupMode = !ungroupMode;
		if(debugMode > 0) Debug.Log(this + " ungroupMode=" + ungroupMode);
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
		if(debugMode > 1) Debug.Log(this + " MouseClickDelta=" + MouseClickDelta.magnitude);
	}

	private void OnInputRightClick(CallbackContext ctx)
	{
		ctxValue = ctx.ReadValue<float>();
		
		if (ctxValue > 0) // right click PRESSED
		{
			if (!hasFocus || !mouseOverViewport)
				return;
			
			//onClick?.Invoke();
		}
		
		if (ctxValue <= 0 && rightClickValue > 0) // right click RELEASED
		{
			if (itemToUse)
			{
				SetItemToUse(null);
			}
			
			else if (gameTurnManager.IsPlayerTurn())
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

			lastClickPosition = mousePosition;
			if(debugMode > 1) Debug.Log(this + " lastClickPosition=" + lastClickPosition);
			
			//onClick?.Invoke();
		}

		if (hasFocus && ctxValue <= 0 && leftClickValue > 0 && MouseClickDelta.magnitude <= MaxMouseDeltaToClick) // left click RELEASED
		{
			if (!mouseOverViewport) return;
			
			var raycastResult = CursorRaycast(out var hit);
			if (raycastResult == CursorRaycastResult_Interaction)
			{
				if (selectedCharacter && itemToUse && itemToUse.CanBeUsedOn(hit.collider.gameObject, out var objectTo))
				{
					selectedCharacter.Routines.AssignUseItem(itemToUse, objectTo);
					SetItemToUse(null);
				}
				
				else if (hit.collider.TryGetComponent(out CharacterEntity clickedCharacter)) // click character
				{
					ClickCharacter(clickedCharacter);
				}

				else if (hit.collider.TryGetComponent(out PropComponent clickedProp)) // click prop
				{
					ClickProp(clickedProp);
				}
				
				else if (hit.collider.TryGetComponent(out IAttackable attackable))
				{
					ClickAttackable(attackable);
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
		SetItemToUse(null);
		
		if (CharacterSelection.TryGetSelectedCharacter(out var newSelected) && newSelected != selectedCharacter)
		{
			onSelect?.Invoke();
			cameraPivot.Focus(newSelected);
		}
	}
	
	// others

	private Vector2 MouseClickDelta => mousePosition - lastClickPosition;
	
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

	private void TryRestSelected()
	{
		if(selectedCharacter && !gameTurnManager.IsTurnBasedMode() && !selectedCharacter.HasActions()) 
			selectedCharacter.Rest();
	}

	private void ClickCharacter(CharacterEntity clickedCharacter)
	{
		Debug.Log(this + " ClickCharacter " + clickedCharacter);

		CharacterSelection.TryGetSelectedCharacter(out selectedCharacter);
		
		if (clickedCharacter.InParty())
		{
			if (gameTurnManager.IsPlayerTurn())
			{
				CharacterSelection.SelectCharacter(clickedCharacter); // ClickCharacter select party character
				//return;
			}
			else if(isDebugMode) Debug.Log("> InParty not in player turn");
		}
		
		else if (clickedCharacter.IsEnemyToPlayer(clickedCharacter))
		{
			if (selectedCharacter)
			{
				onClick?.Invoke();
				
				// assign attack
				if (selectedCharacter.CanAttack(false))
				{
					if(hasNoActionsCursor && !selectedCharacter.CanUseAction(CharacterAction.Attack))
						selectedCharacter.FloatingText_Cant();
					else
					{
						if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Attack " + clickedCharacter);
						if (!IsLeaderMode())
							selectedCharacter.Routines.AssignAttack(clickedCharacter, false, false);
						else
							selectedCharacter.Routines.Leader_AssignAttack(clickedCharacter, false);
						//selectedCharacter.Routines.AssignAttack(clickedCharacter, false, false);
					}
					return;
				}
					
				selectedCharacter.FloatingText_Cant();
			}
			else if(isDebugMode)  Debug.Log("> IsEnemyToPlayer no selected character");
		}
		
		else
		{
			if (selectedCharacter)
			{
				onClick?.Invoke();
				
				if(hasNoActionsCursor && !selectedCharacter.CanUseAction(CharacterAction.Talk))
					selectedCharacter.FloatingText_Cant();
				else
				{
					if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Talk " + clickedCharacter);
					if (!IsLeaderMode())
						selectedCharacter.Routines.AssignTalk(clickedCharacter);
					else
						selectedCharacter.Routines.Leader_AssignTalk(clickedCharacter);
				}
					
				//Debug.LogWarning("//TODO " + selectedCharacter + " interact with " + clickedCharacter); //TODO
			}
			else if(isDebugMode)  Debug.Log("> !IsEnemyToPlayer no selected character");
		}
	}

	private void ClickProp(PropComponent clickedProp) 
	{
		Debug.Log(this + " ClickProp " + clickedProp);

		if (CharacterSelection.TryGetSelectedCharacter(out selectedCharacter))
		{
			onClick?.Invoke();
			
			if (CanAssignOnClick(selectedCharacter))
			{
				if (!selectedCharacter.CanUseAction(CharacterAction.Interact))
				{
					if(debugMode > 0) Debug.Log(this + " " + selectedCharacter + " !CanUseAction Interact");
					selectedCharacter.FloatingText_Cant();
				}
				else
				{
					if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Interact " + clickedProp);
					if (!IsLeaderMode())
						selectedCharacter.Routines.AssignInteract(clickedProp);
					else
						selectedCharacter.Routines.Leader_AssignInteract(clickedProp);
					//selectedCharacter.Routines.AssignInteract(clickedProp);
				}
			}
			else if(isDebugMode)  Debug.Log(" selected character not controllable");
		}
		else if(isDebugMode)  Debug.Log("> no character selected");
	}

	private void ClickAttackable(IAttackable attackable)
	{
		Debug.Log(this + " ClickAttackable " + attackable);
		
		if (selectedCharacter)
		{
			onClick?.Invoke();
			
			// assign attack
			if (selectedCharacter.CanAttack(false))
			{
				if(hasNoActionsCursor && !selectedCharacter.CanUseAction(CharacterAction.Attack))
					selectedCharacter.FloatingText_Cant();
				else
				{
					if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Attack " + attackable);
					if (!IsLeaderMode())
						selectedCharacter.Routines.AssignAttack(attackable, false, false);
					else
						selectedCharacter.Routines.Leader_AssignAttack(attackable, false);
					//selectedCharacter.Routines.AssignAttack(clickedCharacter, false, false);
				}
				return;
			}
				
			selectedCharacter.FloatingText_Cant();
		}
		else if(isDebugMode)  Debug.Log("> IsEnemyToPlayer no selected character");
	}

	private void ClickNavigation(Vector3 navPosition)
	{
		Debug.Log(this + " ClickNavigation " + navPosition);

		if (CharacterSelection.TryGetSelectedCharacter(out selectedCharacter))
		{
			onClick?.Invoke();
			
			if (CanAssignOnClick(selectedCharacter))
			{
				if (!IsLeaderMode())
					selectedCharacter.Routines.AssignMoveToDestination(navPosition);
				else
					selectedCharacter.Routines.Leader_AssignMoveToDestination(navPosition);
			}
			else if(isDebugMode)  Debug.Log(" !CanAssignOnClick");
		}
		
		else if(isDebugMode)  Debug.Log("> no character selected");
	}
	
	private bool CanAssignOnClickSelected() => CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
	                                              && CanAssignOnClick(selectedCharacter);
	
	private bool CanAssignOnClick(CharacterEntity character)
	{
		return hasFocus 
		       && (!gameTurnManager.IsTurnBasedMode() || (gameTurnManager.IsPlayerTurn() && !CharacterRoutines.AnyActiveRoutines()))
		       && character.IsControllableByPlayer();
	}

	private bool CanAssignOnMouseOver(CharacterEntity character)
	{
		return CanAssignOnClick(character) && !character.Routines.AnyActive() && !character.IsMoving();
	}
	
	private void SetCurrentCursorType(CursorType cursorType)
	{
		if(debugMode>1) Debug.Log(this + " SetCurrentCursorType " + cursorType);
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

	private ItemData itemToUse;
	
	public void SetItemToUse(ItemData itemData)
	{
		itemToUse = itemData;
	}

	public ItemData GetItemToUse() => itemToUse;

	public bool IsBlocked() => currentCursor == CursorType.Blocked;

	public Vector3 GetMousePosition()
	{
		return mousePosition;
	}
}
