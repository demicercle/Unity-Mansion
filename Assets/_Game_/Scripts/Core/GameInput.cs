using System;
using System.Collections;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using CallbackContext = UnityEngine.InputSystem.InputAction.CallbackContext;

public interface ICursorHandler
{
	void CursorOver();
}

[System.Serializable]
public class GameInput : MonoBehaviour
{
	public static int debugMode { get; set; }

	private const float MinDelayBetweenAssignTarget = 0.5f;
	public const float MaxMouseDeltaToClick = 3.0f;
	
	public const int CursorRaycastResult_None = 0;
	public const int CursorRaycastResult_Navigation = 1;
	public const int CursorRaycastResult_Interaction = 2;
	public const int CursorRaycastResult_Aim = 3;
	
	public static bool IsConsoleVisible() => DebugLogManager.Instance.IsLogWindowVisible;
	public static void Focus(bool value) => instance.OnApplicationFocus(value);
	
	public static GameInput Instance => instance;

	//[SerializeField] private bool debugLog;

	[SerializeField] private CircleRenderer navigationTarget;
	[Header("Camera Control")]
	[SerializeField] [Range(0f, 360f)] private float orbitSpeedAngle = 180f;

	private float OrbitInertia => CameraSettings.DefaultSettings.orbitInertia;
	[SerializeField] private bool orbitCameraFromCenterScreen = false;
	[SerializeField] private bool orbitCameraY = false;
	[Range(0.01f, 0.99f)] public float maxNavDistance = 0.50f;
	[Min(0f)] public float nodeSize = 0.25f;

	public UnityEvent onClick, onSelect;

	private LayerMask raycastLayers;
	
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

		if (selectedCharacter && selectedCharacter.IsAiming())
		{
			raycastLayers = Layers.MaskAiming;
			hitAim = Physics.Raycast(raycastRay, out aimHit, 100f, raycastLayers);
			if (hitAim && RoomTrigger.IsPositionVisible(aimHit.point))
			{
				outRaycast = aimHit;
				return CursorRaycastResult_Aim;
			}
		}

		hitAim = false;
		
		raycastLayers = itemToUse ? Layers.MaskItem(itemToUse, true, false) : Layers.MaskInteraction;
		hitInteractions = (!selectedCharacter || !selectedCharacter.IsAiming())
						&& Physics.Raycast(raycastRay, out interactionHit, 100f, raycastLayers);
		
		if (hitInteractions && RoomTrigger.IsPositionVisible(interactionHit.point))
		{ 
			outRaycast = interactionHit;
			
			foreach (var cursorHandler in interactionHit.collider.GetComponents<ICursorHandler>()
				         .Concat(interactionHit.collider.GetComponentsInChildren<ICursorHandler>()))
			{
				cursorHandler.CursorOver();
			}
			
			return CursorRaycastResult_Interaction;
		}
		
		raycastLayers = itemToUse ? Layers.MaskItem(itemToUse, false, true) : Layers.MaskNavigation;
		hitNavigation = (!itemToUse || itemToUse.RaycastNavigation())
					&& Physics.Raycast(raycastRay, out navigationHit, 100f, raycastLayers);
		
		if (hitNavigation && RoomTrigger.IsPositionVisible(navigationHit.point))
		{
			if (itemToUse)
			{
				if (!itemToUse.CanBeUsedAt(navigationHit.point))
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

	public bool TryGetNavigationTarget(out Transform navTarget)
	{
		navTarget = null;
		
		if (navigationTarget.gameObject.activeSelf)
		{
			navTarget = navigationTarget.transform;
		}

		else if (lastMouseOverObject)
		{
			if (lastMouseOverObject.TryGetComponent(out CharacterEntity other))
				navTarget = lastMouseOverObject.transform;
		}

		return navTarget;
	}

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
	private Collider lastAimedObject;
	private Vector3 lastNavPosition;
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
	private RaycastHit navigationHit, wallsHit, interactionHit, aimHit;
	private bool hitNavigation, hitWalls, hitInteractions, hitAim;
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
		inputSystem.Player.Action_Run.performed += OnInputAction_Run;
		inputSystem.Player.Action_Defend.performed += OnInputAction_Defend;
		
		CharacterSelection.selectionChanged += SelectionChanged;

		if (DebugLogManager.Instance)
		{
			hasConsoleEvents = true;
			DebugLogManager.Instance.OnLogWindowShown += OnLogWindowShown;
			DebugLogManager.Instance.OnLogWindowHidden += OnLogWindowHidden;
		}

		canvas.onViewportClicked += OnClickedViewport;
		canvas.onViewportMouseOver += OnMouseOverViewport;
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
		inputSystem.Player.SelectNext.performed -= OnInputSelectNext;
		inputSystem.Player.FocusSelected.performed -= OnInputFocusSelected;
		inputSystem.Player.ToggleGroup.performed -= OnInputToggleGroup;
		inputSystem.Player.Action_Run.performed -= OnInputAction_Run;
		inputSystem.Player.Action_Defend.performed -= OnInputAction_Defend;
		
		CharacterSelection.selectionChanged -= SelectionChanged;

		if (DebugLogManager.Instance && hasConsoleEvents)
		{
			hasConsoleEvents = false;
			DebugLogManager.Instance.OnLogWindowShown -= OnLogWindowShown;
			DebugLogManager.Instance.OnLogWindowHidden -= OnLogWindowHidden;
		}
		
		canvas.onViewportClicked -= OnClickedViewport;
		canvas.onViewportMouseOver -= OnMouseOverViewport;
		
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

		CharacterSelection.TryGetSelectedCharacter(out selectedCharacter);
		
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

			if (inputSystem.Player.EndTurn.WasPressedThisFrame())
			{
				gameTurnManager.TryEndPlayerTurn(true);
			}
			
			if (inputSystem.Player.SelectParty1.WasPressedThisFrame()) CharacterSelection.SelectCharacterAt(0);
			if (inputSystem.Player.SelectParty2.WasPressedThisFrame()) CharacterSelection.SelectCharacterAt(1);
			if (inputSystem.Player.SelectParty3.WasPressedThisFrame()) CharacterSelection.SelectCharacterAt(2);

			if (selectedCharacter)
			{
				if (inputSystem.Player.ToggleAim.WasPressedThisFrame()) selectedCharacter.SetAiming(true);
				if (inputSystem.Player.ToggleAim.WasReleasedThisFrame()) selectedCharacter.SetAiming(false);
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
				orbitDelta.x = OrbitInertia <= 0 ? orbitAxis.x : Mathf.Lerp(orbitDelta.x, orbitAxis.x, Time.unscaledDeltaTime * OrbitInertia);
				orbitDelta.y = OrbitInertia <= 0 ? orbitAxis.y :Mathf.Lerp(orbitDelta.y, orbitAxis.y, Time.unscaledDeltaTime * OrbitInertia);
				
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
		
		debugString = mousePosition.ToString() + " " + (int)leftClickValue + (int)(holdMiddleClick?1:0) + (int)rightClickValue;

		if (hasFocus && mouseOverViewport && !holdMiddleClick)
		{
			lastCursorResult = CursorRaycast(out var hit);
			mouseOverChanged = lastMouseOverObject != hit.collider;
			lastMouseOverObject = hit.collider;
			lastAimedObject = hit.collider;
//			if(CharacterEntity.debugAim > 1) Debug.Log(this +"  lastAimedObject=" + lastAimedObject);
			mouseHasMoved = mouseHasMoved || mouseOverChanged || NavUtils.NavDistance(hit.point, lastNavPosition) >= nodeSize;
			lastNavPosition = hit.point;
		}
		else
		{
			lastCursorResult = CursorRaycastResult_None;
			mouseOverChanged = lastMouseOverObject != null;
			lastMouseOverObject = null;
			lastAimedObject = null;
		}
		
		debugString += "\nresult==" + lastCursorResult + " " + (lastMouseOverObject ? lastMouseOverObject.name : "no hit");

		if (lastCursorResult == CursorRaycastResult_Aim)
		{
			if (selectedCharacter)
			{
				SetCurrentCursorType(CursorType.Aim);
				SetMouseOverItem(null);
				SetDraggedItem(null);
				debugString += "\n>Aim";
				
				if (CanAssignOnMouseOver(selectedCharacter))
				{
					if (aimHit.collider)
					{
						if (aimHit.collider.gameObject.layer == Layers.LayerGround)
						{
							aimHit.point += selectedCharacter.GetEyesPosition(true);
						}
					}
					
					selectedCharacter.SetAimingPosition(aimHit.point);
					
					if (selectedCharacter.GetLastShootHits(out var shoots))
					{
						foreach (var shoot in shoots)
						{
							if (shoot.collider && shoot.collider.TryGetComponent(out IAttackable attackable))
							{
								attackable.AimedBy(selectedCharacter);
							}
						}
					}
				}
				
			}
		}
		else if (lastCursorResult == CursorRaycastResult_Navigation)
		{
			if (selectedCharacter)
			{
				SetCurrentCursorType(CursorType.Navigate);
				debugString += "\n>Navigate";
				
				if (CanAssignOnMouseOver(selectedCharacter) && (mouseOverChanged || mouseHasMoved))
				{
					selectedCharacter.GetAgent().AssignDestination(lastNavPosition); // mouse over navigation
				}

				if (itemToUse)
				{
					SetCurrentCursorType(CursorType.UseItem);

					if (itemToUse.IsProjectile())
					{
						selectedCharacter.LookWorldPosition(lastNavPosition, Time.deltaTime);
						foreach (var projectileInHand in selectedCharacter.GetComponentsInChildren<Projectile>()) // TODO Optim
						{
							projectileInHand.Simulate(selectedCharacter.GetCenterPosition(), lastNavPosition);
						}
					}
				}
			}
			//else if(isDebugMode) Debug.Log("no selected character");
		}
		
		else if (lastMouseOverObject && lastCursorResult == CursorRaycastResult_Interaction) 
		{
			if (lastMouseOverObject.TryGetComponent(out CharacterEntity overCharacter))
			{
				if (debugMode > 1) Debug.Log(this + " overCharacter=" + overCharacter);
				debugString += " (Character)";
				if (selectedCharacter != null)
				{
					var agent = selectedCharacter.GetAgent();
					if (overCharacter.InParty())
					{
						SetCurrentCursorType(CursorType.Select);

						if (itemToUse)
							SetCurrentCursorType(itemToUse.CanBeUsedOn(overCharacter) ? CursorType.UseItem : CursorType.Blocked);
						
						else if (CanAssignOnMouseOver(selectedCharacter) && mouseOverChanged)
							agent.ClearDestination();// mouse over another party member
						
					}
					else if (overCharacter.IsEnemyToPlayer())
					{
						SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions : CursorType.Attack);
						
						if (itemToUse)
							SetCurrentCursorType(itemToUse.CanBeUsedOn(overCharacter) ? CursorType.UseItem : CursorType.Blocked);
						
						else if (CanAssignOnMouseOver(selectedCharacter) && mouseOverChanged)
							agent.AssignDestination(overCharacter.GetPosition()); // mouse over enemy
					}
					else
					{
						SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions : CursorType.Talk);
						
						if (itemToUse)
							SetCurrentCursorType(itemToUse.CanBeUsedOn(overCharacter) ? CursorType.UseItem : CursorType.Blocked);
						else if (CanAssignOnMouseOver(selectedCharacter) && mouseOverChanged)
							agent.AssignDestination(overCharacter.GetPosition()); // mouse over character
					}
					debugString += "\n>" + currentCursor;
				}
				//else if(isDebugMode) Debug.Log("no selected character");
			} // TODO ISelectable, ITalkable
			else if (lastMouseOverObject.TryGetComponent(out PropComponent overProp))  // TODO IInteractable
			{
				if (debugMode > 1) Debug.Log(this + " overProp=" + overProp);
				debugString += " (Prop)";
				if (selectedCharacter != null)
				{
					SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions
						: overProp.GetCursorType());
						
					if (itemToUse)
						SetCurrentCursorType(itemToUse.CanBeUsedOn(overCharacter) ? CursorType.UseItem : CursorType.Blocked);
					
					else if (CanAssignOnMouseOver(selectedCharacter)) 
					{
						if (mouseOverChanged) // assign waypoint destination only if it changed
						{
							selectedAgent = selectedCharacter.GetAgent();
							selectedAgent.AssignWaypointDestinationAsync(overProp); // mouse over prop
						}
					}
					debugString += "\n>" + currentCursor;
				}
				//else if(isDebugMode) Debug.Log("no selected character");
			}
			else if (lastMouseOverObject.TryGetComponent(out IAttackable overAttackable))
			{
				if (debugMode > 1) Debug.Log(this + " overAttackable=" + overAttackable);
				debugString += " (Attackable)";
				if (selectedCharacter != null)
				{
					SetCurrentCursorType(hasNoActionsCursor && !selectedCharacter.HasActions() ? CursorType.NoActions
						: CursorType.Attack);
					
					
					if (itemToUse)
						SetCurrentCursorType(itemToUse.CanBeUsedOn(overCharacter) ? CursorType.UseItem : CursorType.Blocked);
					
					else if (CanAssignOnMouseOver(selectedCharacter) && mouseOverChanged) 
					{
						selectedCharacter.GetAgent().AssignDestination(overAttackable.GetPosition()); // mouse over prop attackable
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

				if (itemToUse)
					SetCurrentCursorType(CursorType.Blocked);
				
				if (CanAssignOnMouseOver(selectedCharacter))
				{
					selectedCharacter.GetAgent().ClearDestination();
					foreach (var projectileInHand in selectedCharacter.GetComponentsInChildren<Projectile>())
					{
						projectileInHand.StopSimulate();
					}
				}
			}
		}

		if (CharacterEntity.TryGetPartyCharacter(mouseOverPortrait, out CharacterEntity portraitChar))
		{
			if (itemToUse && itemToUse.CanBeUsedOn(portraitChar))
			{
				SetCurrentCursorType(CursorType.UseItem);
			}
			else
			{
				SetCurrentCursorType(CursorType.Select);
			}
		}

		if (mouseOverItem)
		{
			SetCurrentCursorType(CursorType.Select);
		}

		if (draggedItem)
		{
			SetCurrentCursorType(CursorType.Drag);
		}

		if (mouseOverViewport)
		{
			if (gameTurnManager.IsWaiting())
			{
				SetCurrentCursorType(CursorType.Waiting);
				SetItemToUse(null, false);
				SetDraggedItem(null);
				SetMouseOverItem(null);
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
		navigationTarget.gameObject.SetActive(lastCursorResult == CursorRaycastResult_Navigation && mouseOverViewport && !isLoading && !gameTurnManager.IsWaiting() 
		                                      && (!selectedCharacter || !selectedCharacter.IsAiming()));
		
		if (navigationTarget.gameObject.activeSelf)
		{
			navigationTarget.transform.position = navigationHit.point;
		}
		
		foreach (var character in CharacterEntity.GetPartyCharacters())
		{
			if (character.IsSelected() && gameTurnManager.IsTurnBasedMode() && gameTurnManager.IsPlayerTurn())
			{
				character.GetAgent().DrawPath();
			}
			else
			{
				character.GetAgent().ClearPath();
			}
		}
		
		lastMousePosition = mousePosition;
		mouseHasMoved = false;
		mouseDown = false;
	}

	// KEY INPUT

	private void OnInputAction_Run(CallbackContext ctx)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out selectedCharacter) && selectedCharacter.CanUseAction(CharacterAction.Run))
				selectedCharacter.Routines.AssignUseAction(CharacterAction.Run);
	}
	
	private void OnInputAction_Defend(CallbackContext ctx)
	{
		if (CharacterSelection.TryGetSelectedCharacter(out selectedCharacter) && selectedCharacter.CanUseAction(CharacterAction.Defend))
			selectedCharacter.Routines.AssignUseAction(CharacterAction.Defend);
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
			SetMouseOverItem(null);
			SetDraggedItem(null);
			
			if (itemToUse)
			{
				SetItemToUse(null, true);
			}
			
			else if (gameTurnManager.IsPlayerTurn() || !gameTurnManager.IsTurnBasedMode())
			{
				if (selectedCharacter && selectedCharacter.IsControllableByPlayer())
				{
					if (selectedCharacter.IsAiming()) 
						selectedCharacter.SetAiming(false);
					else if (selectedCharacter.IsActing())
						selectedCharacter.Stop();
					else
						CharacterSelection.SelectCharacter(null);
				}
				else
				{
					CharacterSelection.SelectCharacter(null);
				}
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
			if (raycastResult == CursorRaycastResult_Aim)
			{
				if (selectedCharacter && CanAssignOnClick(selectedCharacter))
				{
					selectedCharacter.Routines.AssignShootAt(aimHit.point, true);
				}
			}
			else if (raycastResult == CursorRaycastResult_Interaction)
			{
				if (hit.collider.TryGetComponent(out CharacterEntity clickedCharacter)) // click character
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
				if (selectedCharacter && itemToUse)
				{
					if (itemToUse.CanBeUsedAt(hit.point) && selectedCharacter.CanUseItem(itemToUse))
					{
						selectedCharacter.Routines.AssignUseItemAt(itemToUse, hit.point);
					}
					
					SetItemToUse(null, false);
				}

				else
				{
					ClickNavigation(hit.point);
				}
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
		SetItemToUse(null, true);//selection changed
		SetDraggedItem(null);
		SetMouseOverItem(null);
		
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

	private void ClickCharacter(CharacterEntity clickedCharacter)
	{
		Debug.Log(this + " ClickCharacter " + clickedCharacter);

		CharacterSelection.TryGetSelectedCharacter(out selectedCharacter);
		
		if (clickedCharacter.InParty() && !itemToUse)
		{
			if (gameTurnManager.IsPlayerTurn())
			{
				CharacterSelection.SelectCharacter(clickedCharacter); // ClickCharacter select party character
				//return;
			}
			else if(isDebugMode) Debug.Log("> InParty not in player turn");
		}
		
		else if (selectedCharacter)
		{
			onClick?.Invoke();
			
			if (CanAssignOnClick(selectedCharacter))
			{
				if (itemToUse)
				{
					if (itemToUse.CanBeUsedOn(clickedCharacter) && selectedCharacter.CanUseItem(itemToUse))
					{
						selectedCharacter.Routines.AssignUseItemOn(itemToUse, clickedCharacter); // use item on character
					}
					else
					{
						selectedCharacter.FloatingText_Cant();
					}
					
					SetItemToUse(null, false);
				}

				else if (clickedCharacter.IsEnemyToPlayer(clickedCharacter))
				{
					// assign attack
					if (selectedCharacter.CanAttack())
					{
						if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Attack " + clickedCharacter);
						if (!IsLeaderMode())
							selectedCharacter.Routines.AssignMoveAndAttack(clickedCharacter, selectedCharacter.IsUsingRangeWeapon());
						else
							selectedCharacter.Routines.Leader_AssignAttack(clickedCharacter);
					}
					else
					{
						selectedCharacter.FloatingText_Cant();
					}
				}

				else
				{
					if (selectedCharacter.CanUseAction(CharacterAction.Talk))
					{
						if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Talk " + clickedCharacter);
						if (!IsLeaderMode())
							selectedCharacter.Routines.AssignTalk(clickedCharacter);
						else
							selectedCharacter.Routines.Leader_AssignTalk(clickedCharacter);
					}
					else
					{
						selectedCharacter.FloatingText_Cant();
					}
				}
			}
		}
		
		else if (clickedCharacter.IsEnemyToPlayer(clickedCharacter))
		{
			if (selectedCharacter)
			{
				onClick?.Invoke();

				if (CanAssignOnClick(selectedCharacter))
				{
					// assign attack
					if (selectedCharacter.CanAttack())
					{
						if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Attack " + clickedCharacter);
						if (!IsLeaderMode())
							selectedCharacter.Routines.AssignMoveAndAttack(clickedCharacter, selectedCharacter.IsUsingRangeWeapon());
						else
							selectedCharacter.Routines.Leader_AssignAttack(clickedCharacter);
					}
				}
			}
			else if(isDebugMode)  Debug.Log("> IsEnemyToPlayer no selected character");
		}
		
		else
		{
			if (selectedCharacter)
			{
				onClick?.Invoke();
				
				if(!selectedCharacter.CanUseAction(CharacterAction.Talk))
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
			
			if (CanAssignOnClick(selectedCharacter)) // interact with prop
			{
				if (itemToUse)
				{
					if (itemToUse.CanBeUsedOn(clickedProp) && selectedCharacter.CanUseItem(itemToUse))
					{
						selectedCharacter.Routines.AssignUseItemOn(itemToUse, clickedProp); // use item on prop
					}
					else
					{
						if(debugMode > 0) Debug.Log(this + " " + selectedCharacter + " !CanUseAction UseItem");
						selectedCharacter.FloatingText_Cant();
					}
					
					SetItemToUse(null, false);
				}
				
				else if (!selectedCharacter.CanUseAction(CharacterAction.Interact))
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

			if (CanAssignOnClick(selectedCharacter)) // attackable
			{
				if (itemToUse)
				{
					if (itemToUse.CanBeUsedOn(attackable) && selectedCharacter.CanUseItem(itemToUse))
					{
						selectedCharacter.Routines.AssignUseItemOn(itemToUse, attackable); // use item on attackable
						SetItemToUse(null, false);
					}
					else
					{
						if(debugMode > 0) Debug.Log(this + " " + selectedCharacter + " !CanUseAction UseItem");
						selectedCharacter.FloatingText_Cant();
					}
				}
				
				else if (selectedCharacter.CanAttack(false))
				{
					if(hasNoActionsCursor && !selectedCharacter.CanUseAction(CharacterAction.Attack))
						selectedCharacter.FloatingText_Cant();
					else
					{
						if (debugMode > 0) Debug.Log(this + " " + selectedCharacter + " Assign Attack " + attackable);
						if (!IsLeaderMode())
							selectedCharacter.Routines.AssignMoveAndAttack(attackable, selectedCharacter.IsUsingRangeWeapon());
						else
							selectedCharacter.Routines.Leader_AssignAttack(attackable);
						//selectedCharacter.Routines.AssignAttack(clickedCharacter, false, false);
					}
					return;
				}
				
				selectedCharacter.FloatingText_Cant();
			}
		}
		else if(isDebugMode)  Debug.Log("> IsEnemyToPlayer no selected character");
	}

	private void ClickNavigation(Vector3 navPosition)
	{
		Debug.Log(this + " ClickNavigation " + navPosition);

		if (CharacterSelection.TryGetSelectedCharacter(out selectedCharacter))
		{
			onClick?.Invoke();
			
			if (CanAssignOnClick(selectedCharacter)) // move to dest
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
	                                              && CanAssignOnClick(selectedCharacter); // CanAssignOnClickSelected
	
	private bool CanAssignOnClick(CharacterEntity character)
	{
		return hasFocus
		       && !gameTurnManager.IsWaiting()
		       && character.IsControllableByPlayer()
		       && (Time.time - character.Routines.LastTimeAssignedTarget > MinDelayBetweenAssignTarget);
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
	private ItemData draggedItem;
	private ItemData mouseOverItem;

	public void AssignDropItem()
	{
		CharacterEntity partyChar;
		
		if (draggedItem && selectedCharacter)
		{
			if ( (CharacterEntity.TryGetPartyCharacter(mouseOverPortrait, out partyChar) || (lastMouseOverObject && lastMouseOverObject.TryGetComponent(out partyChar)))
			    && partyChar != selectedCharacter 
			    && partyChar.InParty())
			{
				Debug.Log(this + " Assign Drop Item " + selectedCharacter + " To " + partyChar);
				selectedCharacter.Routines.AssignGiveItem(draggedItem, partyChar);
			}
			else if (hitNavigation)
			{
				Debug.Log(this + " Assign Drop Item " + selectedCharacter + " At " + lastNavPosition);
				selectedCharacter.Routines.AssignDropItem(draggedItem, lastNavPosition);
				DebugDrawer.WireDisc(lastNavPosition, 0.5f, Color.yellow, 1.0f);
			}
		}
		
		SetDraggedItem(null);
	}

	public bool IsDragged(ItemData itemData) => draggedItem == itemData;

	public void SetDraggedItem(ItemData itemData)
	{
		draggedItem = itemData;
		if (draggedItem)
		{
			SetItemToUse(null, true);
			if(selectedCharacter) selectedCharacter.SetAiming(false);
		}
	}
	
	public void SetItemToUse(ItemData itemData, bool updateSelected)
	{
		if (itemToUse != itemData)
		{
			if(debugMode >0 ) Debug.Log(this + " SetItemToUse " + itemData + " " + updateSelected);
			
			itemToUse = itemData;

			if (updateSelected)
			{
				if (selectedCharacter)
				{
					selectedCharacter.SetItemToUse(itemToUse);
				
					if (itemToUse && itemToUse.IsUsableOnSelf())
					{
						selectedCharacter.SetAiming(false);
						selectedCharacter.Routines.AssignUseItemOn(itemToUse, selectedCharacter);
						SetItemToUse(null, false);
					}
				}
			}
		}
	}

	public void SetMouseOverItem(ItemData itemData)
	{
		mouseOverItem = itemData;
	}

	public ItemData GetItemToUse() => itemToUse;

	public ItemData GetDraggedItem() => draggedItem;

	public bool IsBlocked() => currentCursor == CursorType.Blocked;

	public bool GetAimedObject(out GameObject target)
	{
		target = lastAimedObject?.gameObject;
		return target;
	}

	public CharacterEntity GetSelectedCharacter() => selectedCharacter;
	
	public Vector3 GetMousePosition()
	{
		return mousePosition;
	}

	public bool TryGetMouseOver<T>(out T component) where T : Component
	{
		component = null;
		return lastMouseOverObject && lastMouseOverObject.TryGetComponent<T>(out component);
	}

	private int mouseOverPortrait = -1;
	public void MouseOverPortrait(int index)
	{
		mouseOverPortrait = index;
	}

	public void OnClickPortrait(int partyIndex)
	{
		if (itemToUse && selectedCharacter)
		{
			if (CharacterEntity.TryGetPartyCharacter(partyIndex, out var portraitChar) && itemToUse.CanBeUsedOn(portraitChar))
			{
				selectedCharacter.Routines.AssignUseItemOn(itemToUse, portraitChar); // use item on portrait
				SetItemToUse(null, false);
				return;
			}
		}
		
		CharacterSelection.SelectPartyCharacter(partyIndex);
	}
}
