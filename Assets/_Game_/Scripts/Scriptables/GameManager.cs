using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Game Manager", menuName = "Game Manager")]
public class GameManager : ScriptableObject
{
	private const string TAG_PLAYER = "Player";
	
	static public string LAYER_GROUND = "Ground";
	static public string LAYER_WALL = "Wall";
	static public string LAYER_CHARACTER = "Character";
	
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

	static public T FindObject<T>() where T : Object => FindObjectsByType<T>(default).FirstOrDefault();
	static public T[] FindObjects<T>() where T : Object => FindObjectsByType<T>(default);

	public bool drawDebug = false;
	public bool debugGUI = true;
	public float cameraSpeed = 1.0f;
	public float maxNavigationDistance = 0.5f;

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
	
	public void RemoveNextCoroutine()  => coroutines.RemoveAt(0);
	
	public bool mouseOverViewport { get; set; }
	public bool hasFocus { get; set; }
	
	private Vector2 mousePosition;
	private bool hasMousePosition;
	private AstarPath aStar;
	private MainCamera mainCamera;
	private InputSystem_Actions inputSystem;
	private List<Character> partyCharacters;
	private Character selectedCharacter;
	private List<IEnumerator> coroutines;
	private Character movingCharacter;
	private float movingCharacterDeltaPosition;
	private Vector3 lastMovingCharacterPosition;

	public void MouseMove(Vector2 pos)
	{
		hasMousePosition = true;
		mousePosition = pos;
	}

	public void LeftClick(float value)
	{
	//	Debug.Log("LeftClick: " + value);

		if (mouseOverViewport && value > 0f) // pressed
		{
			var raycastResult = CursorRaycast(out var hit);
			if (raycastResult == CURSOR_RAYCAST_RESULT_CHARACTER)
			{
				if (hit.collider.TryGetComponent(out Character character))
				{
					if (partyCharacters.Contains(character))
					{
						SelectCharacter(character);
					}
				}
			}
			else if (raycastResult == CURSOR_RAYCAST_RESULT_NAVIGATION)
			{
				if (selectedCharacter != null && selectedCharacter.TryGetAgent(out var agent))
				{
					if (!HasCoroutines())
					{
						agent.SetCanMove(true);
						movingCharacter = selectedCharacter;
						lastMovingCharacterPosition = selectedCharacter.GetPosition();
						StartCoroutine(WaitAgentMoved(agent));
					}
				}
			}
		}
	}

	public void StartCoroutine(IEnumerator coroutine)
	{
		coroutines.Add(coroutine);
	}

	private IEnumerator WaitAgentMoved(Character.AgentWrapper agent)
	{
		do
		{
			if (movingCharacter.GetRemainingMovePoints() <= 0)
			{
				Debug.Log(movingCharacter + " out of move points");
				break;
			}
			
			yield return null;
		} while (agent.isMoving && !agent.reachedDestination);
		
		movingCharacter = null;
		agent.ClearDestination();
		agent.SetCanMove(false);
	}

	public void SelectCharacter(Character character)
	{
		if(selectedCharacter != null) GameEvents.characterSelected?.Invoke(selectedCharacter, false);
		selectedCharacter = character;
		GameEvents.characterSelected?.Invoke(selectedCharacter, true);
	}
	
	public void Start()
	{
		Debug.Log(this + " Start()");
		mainCamera = FindObject<MainCamera>();
		aStar = FindObject<AstarPath>();
		
		coroutines = new List<IEnumerator>();
		partyCharacters = new List<Character>();
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Enable();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => mainCamera.Zoom(ctx.ReadValue<Vector2>().y);
		inputSystem.Player.Enable();
		
		// init characters
		foreach (var character in FindObjects<Character>())
		{
			if (character.CompareTag(TAG_PLAYER))
			{
				Debug.Log("Add to party: " + character);
				partyCharacters.Add(character);
				GameEvents.characterAddedToParty?.Invoke(character, partyCharacters.Count-1);
			}
		}
		
		// register UI Events
		RegisterUIEvents(true);
		
		// focus first character
		mainCamera.Focus(partyCharacters.First());
	}

	public void Dispose()
	{
		// unregister UI Events
		RegisterUIEvents(false);
	}

	public void RegisterUIEvents(bool value)
	{
		if (value)
		{
			UIEvents.clickSelectCharacter += SelectCharacter;
		}
		else
		{
			UIEvents.clickSelectCharacter -= SelectCharacter;
		}
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

		if (mouseOverViewport)
		{
			var result = CursorRaycast(out var hit);
			if (result == CURSOR_RAYCAST_RESULT_NAVIGATION)
			{
				if (selectedCharacter != null && selectedCharacter.TryGetAgent(out var agent))
				{
					if (!HasCoroutines())
					{
						agent.AssignDestination(hit.point);
					}
				}
			}
			else if (result == CURSOR_RAYCAST_RESULT_CHARACTER)
			{
				if (hit.collider.TryGetComponent(out Character character))
				{
					if (selectedCharacter != null && selectedCharacter.TryGetAgent(out var agent))
					{
						if (!HasCoroutines())
						{
							agent.AssignDestination(character.GetPosition());
						}
					}
				}
			}
			else
			{
				if (selectedCharacter != null && selectedCharacter.TryGetAgent(out var agent))
				{
					if (!HasCoroutines())
					{
						agent.ClearDestination();
					}
				}
			}
		}

		if (movingCharacter)
		{
			movingCharacter.DrawPath();
			movingCharacterDeltaPosition = Vector3.Distance(movingCharacter.GetPosition(), lastMovingCharacterPosition);
			lastMovingCharacterPosition = movingCharacter.GetPosition();
			movingCharacter.ConsumeMovePoints(movingCharacterDeltaPosition);
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

			foreach (var cursorHandler in hit.collider.GetComponents<ICursorHandler>())
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

	public void OnGUI()
	{
		const float space = 0.5f;
		GUILayout.FlexibleSpace();
		GUILayout.Label(GetLabel("hasFocus") + hasFocus);
		GUILayout.Label(GetLabel("coroutines") + coroutines.Count);
		GUILayout.Space(space);
		if (selectedCharacter)
		{
			GUILayout.Label(GetLabel("selectedCharacter") + selectedCharacter);
			GUILayout.Label(GetLabel(".movePoints") + selectedCharacter.GetRemainingMovePoints());
		}
	}
	
	string GetLabel(string label) => ObjectNames.NicifyVariableName(label) + ": ";
}

public interface ICursorHandler
{
	void CursorOver();
}