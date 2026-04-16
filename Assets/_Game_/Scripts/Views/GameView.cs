using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class GameView
{
	public System.Action<CharacterView, int> characterAddedToParty;
	public System.Action<CharacterView> actionUsed;
	static public System.Action<CharacterView> clickSelectCharacter;
	static public System.Action clickEndTurn;
	static public System.Action clickRunAction;
	public System.Action<bool> routinesStarted;
	public System.Action<CharacterView, bool> characterSelected;
	static public System.Action<CharacterView, Vector3> characterMoved;
	static public System.Action<PropComponent, GameObject> propInteracted;
	
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

	static private GameView instance;

	static public GameView GetInstance() => instance;
	static public T FindObject<T>() where T : Object => Object.FindObjectsByType<T>(default).FirstOrDefault();
	static public T[] FindAllObjects<T>() where T : Object => Object.FindObjectsByType<T>(default);

	public GameData gameData;
	public bool drawDebug = false;
	[Range(0f,1f)] public float debugGUI = 0f;
	public float cameraSpeed = 1.0f;
	[Range(0.01f, 0.50f)] public float maxNavigationDistance = 0.5f;

	public bool HasCoroutines() => coroutines.Count > 0;
	
	public bool GetNextCoroutine(out IEnumerator coroutine)
	{
		if (coroutines.Count > 0)
		{
			coroutine = coroutines[0];
			return true;
		}

		coroutine = default;
		return false;
	}

	public void RemoveNextCoroutine()
	{
		if (coroutines.Count > 0)
		{
			coroutines.RemoveAt(0);
			Debug.Log("EndCoroutine: " + coroutines.Count);
			if (coroutines.Count == 0)
			{
				routinesStarted?.Invoke(false);
			}
		}
	}
	
	public bool mouseOverViewport { get; set; }
	public bool hasFocus { get; set; }
	
	private Vector2 mousePosition;
	private bool hasMousePosition;
	private AstarPath aStar;
	private MainCamera mainCamera;
	private InputSystem_Actions inputSystem;
	private CharacterView _selectedCharacterView;
	private List<IEnumerator> coroutines;

	private List<CharacterView> allCharacters;
	private List<CharacterView> partyCharacters;

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
			SelectCharacter(null); // deselect all
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
					if (partyCharacters.Contains(clickedCharacter))
					{
						SelectCharacter(clickedCharacter);
					}
				}

				if (hit.collider.TryGetComponent(out PropComponent clickedProp)) // click prop
				{
					if (_selectedCharacterView != null && _selectedCharacterView.TryGetAgent(out var agent) && _selectedCharacterView.CanMove())
					{
						if (!HasCoroutines())
						{
							if (clickedProp.GetBestWaypoint(_selectedCharacterView.GetPosition(), out var bestWaypoint))
							{
								StartCoroutine(WaitCharacterMoved(_selectedCharacterView, bestWaypoint.GetPosition(), () =>
								{
									var dist = Vector3.Distance(_selectedCharacterView.GetPosition(), bestWaypoint.GetPosition());
									if (dist <= agent.reachDistance)
									{
										// waypoint reach
										if (_selectedCharacterView.CanInteract())
										{
											StartCoroutine(WaitCharacterInteract(_selectedCharacterView, clickedProp, bestWaypoint));
										}
									}
									else
									{
										Debug.Log(_selectedCharacterView + " not at reach");
									}
								}));
							}
						}
					}
				}
			}
			else if (raycastResult == CURSOR_RAYCAST_RESULT_NAVIGATION) // click navigation
			{
				if (_selectedCharacterView != null && _selectedCharacterView.TryGetAgent(out var agent) && _selectedCharacterView.CanMove())
				{
					if (!HasCoroutines())
					{
						StartCoroutine(WaitCharacterMoved(_selectedCharacterView, hit.point));
					}
				}
			}
		}
	}

	public void StartCoroutine(IEnumerator coroutine)
	{
		if (coroutines.Count == 0)
		{
			routinesStarted?.Invoke(true);
		}
		
		Debug.Log("StartCoroutine: " + coroutines.Count);
		coroutines.Add(coroutine);
	}

	private IEnumerator WaitCharacterInteract(CharacterView characterView, PropComponent prop, Waypoint waypoint = null)
	{
		Debug.Log("WaitCharacterInteract " + characterView + " > " + prop + " @ " + waypoint);
		if (waypoint != null)
		{
			characterView.SetRotateTowards(waypoint.GetPosition(), -1f);
		}
		
		propInteracted?.Invoke(prop, characterView.gameObject);
		yield return prop.Interact(characterView);
	}
	
	private IEnumerator WaitCharacterMoved(CharacterView movingCharacterView, Vector3 dest, System.Action onComplete = null)
	{
		Debug.Log("WaitCharacterMoved " + movingCharacterView);
		
		if (!movingCharacterView.TryGetAgent(out var agent))
		{
			Debug.LogWarning(movingCharacterView + " has no agent");
			yield break;
		}

		if (!movingCharacterView.TryGetData(out var characterData))
		{
			Debug.LogWarning(movingCharacterView + " has no data");
			yield break;
		}
		
		agent.AssignDestination(dest);
		
		while (agent.pathPending)
			yield return null;
		
		if (!movingCharacterView.GetPath(out var path))
		{
			Debug.LogWarning(movingCharacterView + " has no path");
			yield break;
		}

		if (path.Length > 1)
		{
			var firstPoint = path[1];
			var dir = (firstPoint - movingCharacterView.GetPosition()).normalized;
			while (!movingCharacterView.IsFacingDirection(dir))
			{
				movingCharacterView.SetRotateTowards(firstPoint, Time.deltaTime);
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning(movingCharacterView + " has path with only 1 point");
		}
		
		agent.SetCanMove(true);
		
		do
		{
			if (characterData.GetRemainingMovePoints() <= 0)
			{
				agent.StopMovement();
				break;
			}
			
			var oldPosition = movingCharacterView.GetPosition();
			
			yield return null;
			
			var newPosition = movingCharacterView.GetPosition();
			var deltaPos = (newPosition -  oldPosition).magnitude; // TODO: use length squared and recalculate move points
			
			characterData.ConsumeMovePoints(deltaPos);
			characterData.SetPosition(newPosition);
			characterData.SetRotation(movingCharacterView.GetRotation());
			
		} while (agent.isMoving && !agent.reachedDestination);
		
		agent.ClearDestination();
		agent.SetCanMove(false);
		
		onComplete?.Invoke();
	}

	public void SelectCharacter(CharacterView characterView, bool focusCamera = false)
	{
		if(_selectedCharacterView != null) characterSelected?.Invoke(_selectedCharacterView, false);
		_selectedCharacterView = characterView;
		if (_selectedCharacterView != null)
		{
			characterSelected?.Invoke(_selectedCharacterView, true);
			if (focusCamera) mainCamera.Focus(_selectedCharacterView);
		}
	}

	public void EndTurn()
	{
		StartCoroutine(WaitEndTurn());
	}

	private IEnumerator WaitEndTurn()
	{
		Debug.Log(">> End turn");
		yield return new WaitForSeconds(1.0f);
		gameData.NextTurn();
		
		Debug.Log(">> New turn: " + gameData.GetCurrentTurn());
	}

	public void ClickRunAction() => RunAction(_selectedCharacterView, false);
	public void RunAction(CharacterView onCharacterView, bool force)
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

		StartCoroutine(WaitRunAction(onCharacterView));
	}

	private IEnumerator WaitRunAction(CharacterView onCharacterView)
	{
		Debug.Log(">> WaitRunAction: " + onCharacterView);

		if (!onCharacterView.TryGetData(out var characterData))
		{
			yield break;
		}

		DebugGUI.WorldLabel(onCharacterView.GetPosition(), "RUN", 1f).offset = Vector2.up; // TODO Floating Text Component
		yield return new WaitForSeconds(1.0f);
		characterData.RunAction();
	}

	public void Awake()
	{
		instance = this;
	}
	
	public IEnumerator Start()
	{
		Debug.Log(this + " Start()");
		hasFocus = !Application.isEditor;
		
		mainCamera = FindObject<MainCamera>();
		aStar = FindObject<AstarPath>();
		
		coroutines = new List<IEnumerator>();
		partyCharacters = new List<CharacterView>();
		allCharacters = new List<CharacterView>(FindAllObjects<CharacterView>());
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Enable();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.RightClick.performed += ctx => RightClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => mainCamera.Zoom(ctx.ReadValue<Vector2>().y);
		inputSystem.Player.Enable();
		
		gameData.Init(this);
		
		// init characters
		foreach (var character in allCharacters)
		{
			if (!character.TryGetData(out var characterData))
			{
				Debug.LogWarning(character + " has no data!");
				character.gameObject.SetActive(false);
				continue;
			}
			
			gameData.AddCharacter(characterData);
			character.Init();
			
			if (character.IsPlayer())
			{
				Debug.Log("Add to party: " + character + " with data " + characterData);
				gameData.AddCharacterToParty(characterData);
				partyCharacters.Add(character);
			}
		}
		
		// register UI Events
		RegisterEvents(true);

		yield return new WaitForSeconds(0.1f); // todo hak camera focus
		
		// focus first character
		SelectCharacter(partyCharacters.First(), true);
	}

	public void Dispose()
	{
		// unregister UI Events
		RegisterEvents(false);
	}
	
	public void RegisterEvents(bool value)
	{
		if (value)
		{
			gameData.characterAddedToParty += CharacterAddedToParty;
		}
		else
		{
			gameData.characterAddedToParty -= CharacterAddedToParty;
		}
	}

	private void CharacterAddedToParty(CharacterData characterData, int partyIndex)
	{
		characterAddedToParty?.Invoke(characterData.GetCharacter(), partyIndex); // TODO remove "GetCharacter()"
	}

	public void LateUpdate()
	{
		if (hasFocus)
		{
			if (hasMousePosition) mainCamera.MoveCameraWithCursor(mousePosition);
		}
	}
	
	public void Update()
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
			var result = CursorRaycast(out var hit);
			if (result == CURSOR_RAYCAST_RESULT_NAVIGATION) // mouse over navigation
			{
				if (_selectedCharacterView != null && _selectedCharacterView.TryGetAgent(out var agent))
				{
					if (!HasCoroutines())
					{
						agent.AssignDestination(hit.point);
					}
				}
			}
			else if (result == CURSOR_RAYCAST_RESULT_CHARACTER) // mouse over character (or prop?)
			{
				if (hit.collider.TryGetComponent(out CharacterView overCharacter))
				{
					if (_selectedCharacterView != null && _selectedCharacterView.TryGetAgent(out var agent))
					{
						if (!HasCoroutines())
						{
							if (partyCharacters.Contains(overCharacter))
							{
								agent.ClearDestination();
							}
							else
							{
								agent.AssignDestination(overCharacter.GetPosition());
							}
						}
					}
				}
				
				if (hit.collider.TryGetComponent(out PropComponent overProp)) // click prop
				{
					if (_selectedCharacterView != null && _selectedCharacterView.TryGetAgent(out var agent))
					{
						if (!HasCoroutines())
						{
							if (overProp.GetBestWaypoint(_selectedCharacterView.GetPosition(), out var bestWaypoint))
							{
								agent.AssignDestination(bestWaypoint.GetPosition());
							}
							else
							{
								Debug.LogWarning("no waypoint found");
							}
						}
					}
				}
			}
			else
			{
				if (_selectedCharacterView != null && _selectedCharacterView.TryGetAgent(out var agent))
				{
					if (!HasCoroutines())
					{
						agent.ClearDestination();
					}
				}
			}
		}
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
			if(drawDebug) DbgDraw.WireDisc(hit.point, Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90,0,0),maxNavigationDistance, Color.blue);
			Utils.DrawDebugCircle(hit.point, Vector3.up, maxNavigationDistance);

			if (TryGetNearestNavPosition(hit.point, maxNavigationDistance, out var navPosition))
			{
				hit.point = navPosition;
				if(drawDebug) DbgDraw.WireDisc(navPosition, Quaternion.identity,maxNavigationDistance * 0.5f, Color.cyan);

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
		if (_selectedCharacterView)
		{
			GUILayout.Space(space);
			GUILayout.BeginVertical("window");
			_selectedCharacterView?.OnGUIDebug();
			GUILayout.EndVertical();
		}
	}
	
	string GetLabel(string label) => ObjectNames.NicifyVariableName(label) + ": ";
}

public interface ICursorHandler
{
	void CursorOver();
}