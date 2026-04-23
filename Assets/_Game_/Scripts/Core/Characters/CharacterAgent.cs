using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using Pathfinding;
using Pathfinding.Drawing;
using Pathfinding.RVO;
using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class CharacterAgent : MonoBehaviour
{
	private const float TimeOut = 3f;
	private const float WaypointMaxEndPointDistance = 0.2f;
	private const int WaypointRoundDecimals = 1000;
	private const float WaypointMaxSearchDistance = 10.0f;
	
	public static int debugMode { get; set; }
	
	[Header("Movement")]
	[SerializeField] [Range(0.0f, 10.0f)] private float moveSpeed = 4.5f;
	[SerializeField] [Range(0.1f, 2.0f)] private float radius = 0.5f;
	[SerializeField] [Range(0.1f, 3.0f)] private float height = 1.5f;
	[SerializeField] [Min(0f)] private float endReachDistance = 0.0f;
	
	[Header("Rendering")]
	[SerializeField] private List<LineRenderer> pathRenderers;
	
	[Header("Circles")]
	[SerializeField] private CircleRenderer moveTarget;
	
	//public bool PositionChanged(Vector3 p1, Vector3 p2) => p1.ToString() != p2.ToString();
	
	public float GetMaxSpeed() => moveSpeed;

	public float GetRadius() => radius;

	public float GetHeight() => height;

	public void Teleport(Vector3 newPos, int newRot, bool stopMovement = true)
	{
		if(stopMovement) StopMovement();
		aiPath.updatePosition = aiPath.updateRotation = true;
		aiPath.Teleport(newPos, true);
		aiPath.rotation = Quaternion.Euler(0, newRot, 0);
	}

	private bool isObstacle;
	public void SetObstacle(bool value)
	{
		if (isObstacle == value)
			return;

		isObstacle = value;
		if (debugMode > 0) Debug.Log(this + " SetObstacle " + isObstacle);
		if (aiPath.TryGetComponent(out GraphUpdateScene graphUpdate) )
		{
			if (graphUpdate.enabled == value) return;
			//Debug.Log(this + " SetObstacle " + value);
			graphUpdate.enabled = value;
			if (value)
			{
				graphUpdate.points = new[]
				{
					Vector3.forward * aiPath.radius,
					Vector3.right * aiPath.radius,
					-Vector3.forward * aiPath.radius,
					-Vector3.right * aiPath.radius,
				};
			}
			NavmeshRebuilder.RebuildNavigationOn(gameObject);
		}
		else Debug.LogWarning(this + " SetObstacle not possible: no GraphUpdateScene component");
	}

	public Vector3[] GetCurrentPath()
	{
		if (!hasDestination) // GetCurrentPath
			return Array.Empty<Vector3>();
		
		var buffer = new List<Vector3>();
		aiPath.GetRemainingPath(buffer, out var stale);
		
		if (buffer.Count == 0)
			return Array.Empty<Vector3>();

		var dest = aiPath.destination;
		var lastPoint = buffer.Last();
		var maxNavDist = GameInput.Instance.maxNavDistance;
		var drawColor = Color.deepSkyBlue;

		if (Vector3.Distance(lastPoint, dest) > maxNavDist)
		{
			drawColor = Color.darkRed;
			buffer.Clear();
		}

		if (debugMode > 0)
		{
			DebugDrawer.WireDisc(lastPoint + Vector3.up * 0.1f, Vector3.forward, maxNavDist, drawColor);
			DebugDrawer.Line(lastPoint + Vector3.up * 0.1f, dest + Vector3.up * 0.1f, drawColor);
		}
		
		return buffer.ToArray();
	}

	public bool isPathValid => lastPath?.Length > 1 && RoomTrigger.IsPositionVisible(lastPath.Last());

	public void StartMove()
	{
		if(debugMode > 0) Debug.Log(this + " StartMove");
		SetObstacle(false);
		aiPath.updatePosition = aiPath.updateRotation = true;
		aiPath.simulateMovement = true;
		aiPath.isStopped = false;
		UpdatePathAndTarget(true);
	}

	public void StopMovement()
	{
		if(debugMode > 0) Debug.Log(this + " StopMovement()");
		SetObstacle(!_character.IsSelected());
		aiPath.isStopped = true;
		aiPath.updatePosition = aiPath.updateRotation = false;
		aiPath.simulateMovement = false;
		ClearDestination();
	}

	public Waypoint GetBestWaypoint() => bestWaypoint;
	
	public Coroutine AssignWaypointDestinationAsync(MonoBehaviour provider)
	{
		if (waypointProvider)
		{
			if (waypointProvider == provider)
				return null;
			//else
			StopCoroutine("SearchBestWaypoint");
		}
		
		waypointProvider = provider;
		
		if (!waypointProvider)
			return null;
		
		return StartCoroutine("SearchBestWaypoint");
	}

	private IEnumerator SearchBestWaypoint()
	{
		float debugDrawDuration = 1.0f;
		
		bestWaypoint = null;
		waypoints = waypointProvider.GetComponentsInChildren<Waypoint>();
		
		if (debugMode > 0)
		{
			Debug.Log(this + " SearchBestWaypoint from " + _character + " to " + waypointProvider + " waypoints=" + waypoints.Length);
			DebugDrawer.Line(_character.GetCenterPosition(), waypointProvider.transform.position, Color.blueViolet, 1.0f);
		}

		var startPoint = _character.GetPosition();
		var bestLength = 0f;
		var index = 0;
		foreach (var wp in waypoints)
		{
			var endPoint = wp.GetPosition();

			if (Vector3.Distance(startPoint, endPoint) > WaypointMaxSearchDistance)
				continue;
				
			// Crée une requête de chemin
			ABPath path = ABPath.Construct(startPoint, endPoint, null);
			path.calculatePartial = false;
			
			// Envoie la requête au système de pathfinding
			AstarPath.StartPath(path);
			
			// Attends que le chemin soit calculé (asynchrone)
			if (debugMode > 1) Debug.Log(" StartPath to " + wp + " pathRequest=" + path);
			yield return StartCoroutine(path.WaitForPath());
				
			// Récupère le chemin calculé
			if (path.error)
			{
				if (debugMode > 1) DebugDrawer.Line(startPoint, endPoint, Color.red, debugDrawDuration);
				
				Debug.LogWarning("Erreur lors du calcul du chemin : " + path.errorLog);
			}
			else
			{
				//if (debugMode > 1) DebugDrawer.Line(startPoint, endPoint, Color.darkBlue, debugDrawDuration);
				Vector3[] pathPoints = path.vectorPath.ToArray();
				bool isPartial = path.CompleteState == PathCompleteState.Partial || 
				                 !NavUtils.TryGetNearestNavPosition(pathPoints.LastOrDefault(), WaypointMaxEndPointDistance, out var _);
				
				if (isPartial)
				{
					if (debugMode > 1) Debug.Log(index + " partialPath, skip it       pathPoints=" + pathPoints.Length);
					if(debugMode > 1) DebugDrawer.Path(pathPoints, false, Color.darkRed, debugDrawDuration);
				}
				else
				{
					if(debugMode > 1) DebugDrawer.Path(pathPoints, false, Color.blueViolet, debugDrawDuration);
					float pathLength = path.GetTotalLength();
					if (bestWaypoint && (int)pathLength * WaypointRoundDecimals == (int)bestLength * WaypointRoundDecimals) // sometimes results are wrong, this is a hak
					{
						var bestDist = Vector3.Distance(startPoint, bestWaypoint.GetPosition());
						var pathDist = Vector3.Distance(startPoint, endPoint);
						if (bestDist < pathDist)
							pathLength = -1f; // override path length because the path is actually not shorter
					}
					if(debugMode > 1) Debug.Log(index+ " bestWaypoint=" + bestWaypoint + " pathLength=" + pathLength + " from bestLength=" + bestLength + " path.Length=" + pathPoints.Length);
					if (!bestWaypoint || (pathLength > 0 && pathLength < bestLength))
					{
						bestLength = pathLength;
						bestWaypoint = wp;
					}
				}
				
				// Tu peux maintenant utiliser 'waypoints' comme tu le souhaites
				index++;
			}
		}

		if (!bestWaypoint)
		{
			if(debugMode > 0) Debug.Log(this + " no waypoint found! provider=" + waypointProvider + " count=" + waypoints.Length);
		}
		else
		{
			AssignDestination(bestWaypoint.GetPosition(), true);
			
			if (debugMode > 0)
			{
				Debug.Log("new destination set bestWaypoint=" + bestWaypoint);
				DebugDrawer.Line(_character.GetCenterPosition(), bestWaypoint.GetPosition(), Color.cyan, debugDrawDuration);
				DebugDrawer.WireDisc(bestWaypoint.GetPosition(), bestWaypoint.radius, Color.cyan, debugDrawDuration);
				DebugDrawer.Line(bestWaypoint.GetPosition(), bestWaypoint.GetPosition() + Vector3.up, Color.cyan, debugDrawDuration);
			}
		}
		
		waypointProvider = null; // reset, TODO for optim
	}

	public void ClearWaypoint()
	{
		bestWaypoint = null;
	}
	
	public void AssignDestination(Vector3 hitPoint, bool force)
	{
		if (force || !hasDestination)
		{
			if(!hasDestination && debugMode > 0) Debug.Log(this + " Assign_new_Destination " + force);
			
			if (IsFollowing()) 
				AssignFollowTransform(null);

			if (!hasDestination || Vector3.Distance(aiPath.destination, hitPoint) > 0.05f)
			{
				if(debugMode > 1) Debug.Log(this + " aiPath.SearchPath() ");
				aiPath.endReachedDistance = endReachDistance;
				aiPath.destination = hitPoint;
				aiPath.canSearch = true;
				aiPath.SearchPath();
			}
			
			hasDestination = true;
			
			UpdatePathAndTarget(true);
		}
	}

	public bool IsFollowing() => aiDestinationSetter && aiDestinationSetter.target;

	public void AssignFollowTransform(Transform followTarget, float distance = 0f)
	{
		if(!aiDestinationSetter) aiDestinationSetter = GetComponent<AIDestinationSetter>();
		if(!aiDestinationSetter) aiDestinationSetter = gameObject.AddComponent<AIDestinationSetter>();
		if (aiDestinationSetter.target == followTarget) return;
		if (debugMode > 0)Debug.Log(this + " Assign followTarget=" + followTarget + " " + distance);
		hasDestination = false;
		if (IsMoving())
		{
			StopMovement();
			SetObstacle(true);
		}
		aiDestinationSetter.enabled = followTarget;
		aiDestinationSetter.target = followTarget;
		aiPath.endReachedDistance = Mathf.Max(distance, GetMinReachDistance());
		if (followTarget && !IsMoving()) StartMove();
		UpdatePathAndTarget(true);
	}

	public void ClearDestination()
	{
		if (hasDestination) // ClearDestination
		{
			bestWaypoint = null;
			hasDestination = false;
//			Debug.Log(aiPath + " hasDestination=" + hasDestination);
			UpdatePathAndTarget(true);
		}
	}

	public bool ReachedDestination(Vector3 dest)
	{
		return /*aiPath.reachedDestination ||*/ Vector3.Distance(aiPath.position, dest) <= GetMinReachDistance();
	}

	public void SetAgentActive(bool value)
	{
		if(debugMode > 0) Debug.Log(this + " SetAgentActive=" + value);
		
		StopMovement();
		
		aiPath.enabled = value;
		
		if (aiPath.enabled)
		{
			aiPath.Teleport(transform.position);
		}
		else
		{
			SetObstacle(false);
		}

		rvoController.enabled = value;
	}

	private void OnSearchPath()
	{
		lastPath = GetCurrentPath();
		if (debugMode > 1) Debug.Log(this + " OnSearchPath lastPath=" + lastPath.Length);
		UpdatePathAndTarget(true);
	}

	public Coroutine SearchPathAsync(Vector3 dest, System.Action onComplete = null)
	{
		return StartCoroutine(DoSearchPathAsync(dest, onComplete));
	}
	
	private IEnumerator DoSearchPathAsync(Vector3 dest, System.Action onComplete = null)
	{
		AssignDestination(dest, true); // WaitCharacterMoved
		
		var timeout = 0.0f;
		do
		{
			UpdatePathAndTarget(true);
			if (timeout >= TimeOut) break;
			yield return null;
			timeout += Time.deltaTime;
		}
		while (pathPending);

		UpdatePathAndTarget(true);
		
		if (timeout >= TimeOut)
		{
			Debug.LogWarning(this + " pathPending timeout!");
			yield break;
		}
		
		onComplete?.Invoke();
		//var path = GetCurrentPath();
	}
	
	public Vector3 velocity => aiPath.velocity;

	public bool IsMoving() => !aiPath.isStopped /*&& !aiPath.pathPending && velocity.magnitude > Consts.MinMagnitude)*/;
	   
	public float GetMinReachDistance() => Mathf.Max(endReachDistance, aiPath.endReachedDistance);

	public void SetReachDistance(float value) => aiPath.endReachedDistance = value;

	public void SetDefaultReachDistance() => aiPath.endReachedDistance = endReachDistance;

	public void EnableAvoidance(bool b)
	{
		rvoController.enabled = b;
	}
	   
	public bool pathPending => aiPath.pathPending;

	public float rotationSpeed => aiPath.rotationSpeed;

	///private float lastAssignTime;
	private bool hasDestination;

	//private Transform followTarget;
	private AIDestinationSetter aiDestinationSetter;
	private AIPath aiPath;
	private CharacterController _character;
	private GameTurnManager _gameTurnManager; // TODO
	private CharacterAnimation _characterAnimation;
	[HideInInspector] private Vector3[] lastPath; //hide in unity because it crashes (too much things to display probably)
	private MonoBehaviour waypointProvider;
	private Waypoint bestWaypoint;
	private Waypoint[] waypoints;
	private RVOController rvoController;

	private void OnDrawGizmosSelected()
	{
		UpdateComponent();
		Gizmos.color = Color.cyan;
		Utils.DrawGizmosCircle(transform.position, Vector3.up, GetMinReachDistance());
	}

	private void Awake()
	{
		_characterAnimation = GetComponent<CharacterAnimation>();
		_character = GetComponent<CharacterController>();
		_character.onUseAction += OnUseAction;
		
		_gameTurnManager = GameTurnManager.Instance;
		_gameTurnManager.onTurnEnded += UpdatePathAndTarget;
		_gameTurnManager.onTurnStarted += UpdatePathAndTarget;
		CharacterSelection.selectionChanged += SelectionChanged;
		
		UpdateComponent();
		aiPath.onSearchPath += OnSearchPath;
	}

	private void UpdateComponent()
	{
		aiPath = GetComponent<AIPath>();
		aiPath.radius = radius;
		aiPath.height = height;
		aiPath.maxSpeed = moveSpeed;
		if(endReachDistance > 0f) aiPath.endReachedDistance = endReachDistance;
		rvoController = GetComponent<RVOController>();
		if(Application.isPlaying) SetAgentActive(false);
	}
	
	private void OnDestroy()
	{
		aiPath.onSearchPath -= OnSearchPath;
		_character.onUseAction -= OnUseAction;
		_gameTurnManager.onTurnEnded -= UpdatePathAndTarget;
		_gameTurnManager.onTurnStarted -= UpdatePathAndTarget;
		CharacterSelection.selectionChanged -= SelectionChanged;
	}

	private void Start()
	{
		if (!hasDestination) aiPath.isStopped = true; // Start
		
		UpdatePathAndTarget(true);
	}

	private void OnUseAction(CharacterAction action, object param)
	{
		StopMovement();
		UpdatePathAndTarget(true);
	}

	private void SelectionChanged()
	{
		if (_character.IsSelected())
		{
			SetObstacle(!_character.IsControllableByPlayer());
		}
		else
		{
			SetObstacle(true);
			if(!IsMoving()) ClearDestination();
		}
		
		UpdatePathAndTarget(true);
	}

	private void LateUpdate()
	{
		if (lastPath?.Length > 0 && debugMode > 0) DebugDrawer.Path(lastPath, false, isPathValid ? Color.cyan : Color.red);
		
		if (CharacterRoutines.AnyActiveRoutines()) // todo
			UpdatePathAndTarget(true);
	}

	private bool[] shouldDrawPath = new []{false,false};
	private bool[] isDrawingPath = new []{false,false};
	private void UpdatePathAndTarget() => UpdatePathAndTarget(true);
	private void UpdatePathAndTarget(bool force)
	{
		//TODO: controller.IsActing(), controller.IsMoving(), controller.IsRotating()

		shouldDrawPath[0] = hasDestination // "thin" path
		                    && !IsFollowing()
		                    && !IsMoving()
		                    && GameTurnManager.Instance.IsTurnBasedMode()
		                    && CharacterSelection.IsCharacterSelected(_character)
		                    && _character.IsControllableByPlayer()
		                    && (!CharacterRoutines.AnyActiveRoutines() || _character.IsMoving())
		                    && lastPath.Length > 1;

		shouldDrawPath[1] = hasDestination  // "thin" path
		                    && !IsFollowing()
		                    && !IsMoving()
		                    && GameTurnManager.Instance.IsTurnBasedMode()
		                    && CharacterSelection.IsCharacterSelected(_character) 
		                    && _character.IsControllableByPlayer() 
		                    && !CharacterRoutines.AnyActiveRoutines()
		                    && lastPath.Length > 1;
								//&& !controller.IsRotating()
								//&& !controller.IsMoving();

		if (force || shouldDrawPath[0] != isDrawingPath[0])
		{
			pathRenderers[0].enabled = isDrawingPath[0] = shouldDrawPath[0];
			if (pathRenderers[0].enabled) // draw path thin
			{
				pathRenderers[0].startColor = pathRenderers[0].endColor = ColorSettings.GetDisabledColor(_character.GetCurrentFaction());
				pathRenderers[0].positionCount = lastPath.Length;
				pathRenderers[0].useWorldSpace = true;
				pathRenderers[0].SetPositions(lastPath);
			}
		}

		if (force || shouldDrawPath[1] != isDrawingPath[1])
		{
			pathRenderers[1].enabled = isDrawingPath[1] = shouldDrawPath[1];
			if (pathRenderers[1].enabled) // draw path thick
			{
				lastPath = Utils.LimitPathTotalLength(lastPath, _character.GetRemainingMovePoints());
				pathRenderers[1].startColor = pathRenderers[1].endColor = ColorSettings.GetCharacterColor(_character, true, true);
				pathRenderers[1].positionCount = lastPath.Length;
				pathRenderers[1].useWorldSpace = true;
				pathRenderers[1].SetPositions(lastPath);
			}
		}
		
		UpdateMoveTarget(_character.IsMoving());
	}

	bool ShowMoveTarget()
	{
		if (IsFollowing() || !_character.IsPlayer())
			return false;

		if (bestWaypoint)
			return true;

		return hasDestination || IsMoving(); // show the target hak
	}

	void UpdateMoveTarget(bool isBlinking)
	{
		moveTarget.gameObject.SetActive(ShowMoveTarget());
		if (!moveTarget.gameObject.activeSelf)
			return;
		
		moveTarget.transform.position = aiPath.destination;
		moveTarget.SetColor(isPathValid ? ColorSettings.GetCharacterColor(_character, true, true) 
			: ColorSettings.GetDisabledColor(_character.GetCurrentFaction()));
		
		TransformAnimations.BounceRealtime(moveTarget.transform, isBlinking);
	}
}