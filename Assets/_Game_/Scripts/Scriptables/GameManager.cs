using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEngine;

[CreateAssetMenu(fileName = "New Game Manager", menuName = "Game Manager")]
public class GameManager : ScriptableObject
{
	const string TAG_PLAYER = "Player";
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
	
	public float cameraSpeed = 1.0f;
	public float maxNavigationDistance = 0.5f;

	private Vector2 mousePosition;
	private AstarPath aStar;
	private MainCamera mainCamera;
	private InputSystem_Actions inputSystem;
	private List<Character> partyCharacters;

	public void MouseMove(Vector2 pos)
	{
		mousePosition = pos;
	}
	
	public void Start()
	{
		Debug.Log(this + " Start()");
		mainCamera = FindObject<MainCamera>();
		aStar = FindObject<AstarPath>();
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Enable();
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => mainCamera.Zoom(ctx.ReadValue<Vector2>().y);
		inputSystem.Player.Enable();
		
		// init characters
		partyCharacters = new List<Character>();
		foreach (var character in FindObjects<Character>())
		{
			if (character.CompareTag(TAG_PLAYER))
			{
				Debug.Log("Add to party: " + character);
				partyCharacters.Add(character);
				GameEvents.characterAddedToParty?.Invoke(character, partyCharacters.Count-1);
			}
		}
		
		// focus first character
		mainCamera.Focus(partyCharacters.First());
	}

	public void Update()
	{
		if (inputSystem.Player.Move.IsPressed())
		{
			mainCamera.Move(inputSystem.Player.Move.ReadValue<Vector2>() * cameraSpeed * Time.unscaledDeltaTime);
		}
		
		CursorRaycast();
	}

	public void CursorRaycast()
	{
		//TODO: if mouse over viewport
		var hit = default(RaycastHit);
		var cam = Camera.main;
		var ray = cam.ScreenPointToRay(mousePosition);
		if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(LAYER_CHARACTER)))
		{
			//DebugGUI.Log("Hit Character: " + hit.collider);
			DbgDraw.WireCapsule(hit.collider.transform.position, Quaternion.identity, 0.5f, 1.5f, Color.green);
			Utils.DrawDebugCircle(hit.collider.transform.position, Vector3.up, 0.5f);
		}
		else if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask(LAYER_GROUND, LAYER_WALL)))
		{
			//DebugGUI.Log("Hit Navigation: " + hit.point);
			DbgDraw.WireDisc(hit.point, Quaternion.LookRotation(hit.normal) * Quaternion.Euler(90,0,0),maxNavigationDistance, Color.blue);
			Utils.DrawDebugCircle(hit.point, Vector3.up, maxNavigationDistance);

			if (TryGetNearestNavPosition(hit.point, maxNavigationDistance, out var navPosition))
			{
				DbgDraw.WireDisc(navPosition, Quaternion.identity,maxNavigationDistance * 0.5f, Color.cyan);
			}
		}
	}
}
