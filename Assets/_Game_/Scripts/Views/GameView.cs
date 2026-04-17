using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


[System.Serializable]
public class GameView : MonoBehaviour, IView<GameController>
{
	static public event System.Action<GameView> onUpdate;
	
	override public string ToString() => "GameView";

	
	static private GameView instance;
	static public GameView GetInstance()
	{
		if (instance == null) instance = FindObjectOfType<GameView>();
		return instance;
	}
	
	private GameController characterController;

	public GameController GetController()
	{
		if(!characterController) characterController = GetComponent<GameController>();
		return characterController;
	}
	
	#region SELECTION

	private CharacterController selectedCharacter;
	public CharacterController GetSelectedCharacter() => selectedCharacter;
	public bool IsCharacterSelected(CharacterController character) => character && selectedCharacter == character;

	public void SelectCharacter(int index)
	{
		var partyCharacters = GetController().GetPartyCharacters();
		SelectCharacter(partyCharacters[index]);
	}
	
	public void SelectCharacter(CharacterController character)
	{
		if (selectedCharacter != null)
			selectedCharacter.SetSelected(false);
		
		selectedCharacter = character;
		
		if (selectedCharacter != null)
			selectedCharacter.SetSelected(true);
		
		SelectionChanged();
	}
	
	public void SelectNextCharacter()
	{
		var gameController = GetController();
		
		var party = gameController.GetPartyCharacters().OrderBy(c => c.GetPartyIndex()).ToArray();
		var startIndex = gameController.FindSelectedCharacterIndex();
		if (startIndex < 0)
		{
			Debug.Log(this + " Select First Character");
			SelectCharacter(party.FirstOrDefault());
			return;
		}
		
		Debug.Log(this + " SelectNextCharacter " + startIndex);
		var partyCount = party.Length;
		while (partyCount-- > 0)
		{
			var newIndex = (partyCount + startIndex) % party.Length;
			Debug.Log(this + " ? " + newIndex);
			var newChar = party[newIndex];
			if (!newChar.IsDead())
			{
				SelectCharacter(newChar);
				return;
			}
		}
		
		Debug.LogWarning(this + " Has no character to select next!");
	}
	
	void SelectionChanged()
	{
		if (selectedCharacter)
		{
			mainCamera.Focus(selectedCharacter);
		}
	}
	#endregion
	
	void Awake()
	{
		instance = this;
		DontDestroyOnLoad(gameObject);
		name = "GameView (Singleton)";
		hasFocus = !Application.isEditor;
		
		mainCamera = FindObjectsByType<MainCamera>(default).First();
		aStar = FindObjectsByType<AstarPath>(default).First();
		
		// init inputs
		inputSystem = new InputSystem_Actions();
		inputSystem.UI.Click.performed += ctx => LeftClick(ctx.ReadValue<float>());
		inputSystem.UI.RightClick.performed += ctx => RightClick(ctx.ReadValue<float>());
		inputSystem.UI.Point.performed += ctx => MouseMove(ctx.ReadValue<Vector2>());
		inputSystem.UI.ScrollWheel.performed += ctx => mainCamera.Zoom(ctx.ReadValue<Vector2>().y);
	}

	[SerializeField] private List<string> scenesToLoad = new List<string>();
	
	private IEnumerator Start()
	{
		StartCoroutine(Coroutines());
		
		canvas = FindObjectsByType<UI_Canvas>(default).First();
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
		
		var levelCharacters = FindObjectsOfType<CharacterController>()
			.OrderBy(c => c.name)
			.ToList();
		
		Debug.Log(this + " LevelLoaded: levelCharacters=" + levelCharacters.Count);
		
		var partyIndex = 0;
		foreach (var character in levelCharacters)
		{
			GetController().AddCharacter(character);
			if(character.CompareTag(Consts.TAG_PLAYER)) character.SetPartyIndex(partyIndex++);
		}
		
		// TODO Fade Out
		yield return null;
		
		SetInputEnabled(true);
		
		GetController().NewTurn();

		yield return new WaitForSeconds(0.5f); // todo hak camera focus
		
		SelectNextCharacter();
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
		var clickedCharacter = clickedCharacterView.GetController();
	//	//var selectedCharacter = gameController.GetSelectedCharacter();
		if (CanControlCharacter(clickedCharacter))
		{
			SelectCharacter(clickedCharacter); // ClickCharacter select party character
		}
		else if (clickedCharacter.IsEnemyToPlayer())
		{
			if (selectedCharacter != null)
			{
				AssignAttack(selectedCharacter, clickedCharacter, false, true);
			}
			else
			{
				SelectCharacter(clickedCharacter); // ClickCharacter select enemy
			}
		}
		else
		{
			if (selectedCharacter != null)
			{
				Debug.Log("TODO interact with " + clickedCharacter); //TODO
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
					Debug.Log(character + " not at reach");
				}
			}));
		}
	}

	public void ClickNavigation(Vector3 navPosition)
	{
		Debug.Log(this + " ClickNavigation " + navPosition);
		var gameController = GetController();
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

	public System.Action<bool> routinesStarted;
	
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

	public List<CharacterView> GetAllCharacters() =>  CharacterView.allInstances.ToList();

	public List<CharacterView> GetPartyCharacters() => CharacterView.allInstances.Where(c => c.GetController().InParty()).ToList();

	public int GetPartyCharacterIndex(CharacterView characterView) => GetPartyCharacters().IndexOf(characterView);
	public int GetCharacterIndex(CharacterView characterView) => GetAllCharacters().IndexOf(characterView);

	public bool drawDebug = false;
	[UnityEngine.Range(0f,1f)] public float debugGUI = 0f;
	public float cameraSpeed = 1.0f;
	[UnityEngine.Range(0.01f, 0.50f)] public float maxNavDistance = 0.50f;

	public bool HasCoroutines() => coroutines.Count > 0;
	
	public bool mouseOverViewport { get; set; }
	
	public bool hasFocus { get; set; }

	private UI_Canvas canvas;
	private Vector2 mousePosition;
	private bool hasMousePosition;
	private AstarPath aStar;
	private MainCamera mainCamera;
	public InputSystem_Actions inputSystem { get; private set; }
	
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
			if (GetController().IsPlayerTurn() && !HasCoroutines())
			{
				SelectCharacter(null); // deselect all
			}
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
		Debug.Log("QueueRoutine: " + coroutines.Count);
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
				character.LookDirection(wpDir, Time.deltaTime);
			
			if (prop.consumeActions > 0)
				character.ConsumeActions(prop.consumeActions);
			
			yield return new WaitForSeconds(prop.triggerDelay);

			prop.GetController().Interact();
		
			yield return new WaitForSeconds(prop.animDuration);

			if (prop.rebuildNavigation)
			{
				yield return Utils.RebuildNavigationAsync();
			}
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
			movingCharacter.SetPosition(newPosition);
			
			var newRotation = movingCharacter.GetRotation();
			movingCharacter.SetRotation(newRotation);
			
			onUpdate?.Invoke();
			
		} while (agent.isMoving && !agent.reachedDestination);
		
		//movingCharacter.SetPosition(path.Last());
		//movingCharacter.SetRotation(movingCharacter.GetRotation());
		
		agent.SetCanMove(false);
		
		onComplete?.Invoke();
	}

	public bool CanEndTurn() => !HasCoroutines() && GetController().IsPlayerTurn();

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

			mainCamera.Focus(otherCharacter);

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
		
		SelectNextCharacter();
	}

	public bool CanUseRunAction() =>
		!HasCoroutines() && GetController().IsPlayerTurn() && selectedCharacter != null && selectedCharacter.CanUseRunAction();

	public bool IsControlPanelVisible() =>
		selectedCharacter && selectedCharacter.InParty() && GetController().IsPlayerTurn();

	public CharacterController GetPartyCharacter(int index) => GetController().GetPartyCharacter(index);
	
	public void ClickRunAction()
	{
		if (selectedCharacter.CanUseRunAction())
		{
			selectedCharacter.RunAction();
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

	public bool CanControlSelectedCharacter() => selectedCharacter && CanControlCharacter(selectedCharacter);
	public bool CanControlCharacter(CharacterController character)
	{
		return !HasCoroutines() && GetController().IsPlayerTurn() && character.IsControllable();
	}
	
	
	private IEnumerator WaitRunAction(CharacterController character)
	{
		Debug.Log(">> WaitRunAction: " + character);
		yield return new WaitForSeconds(1.0f);
		character.RunAction();
	}

	private void LateUpdate()
	{
		if (hasFocus)
		{
			if (hasMousePosition) mainCamera.MoveCameraWithCursor(mousePosition);
		}

		canvas.UpdateUI(this);
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
			var gameController = GetController();
			//var selectedCharacter = gameController.GetSelectedCharacter();
			var debugCursor = false;
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
	
	string GetLabel(string label) => ObjectNames.NicifyVariableName(label) + ": ";

}

public interface ICursorHandler
{
	void CursorOver();
}