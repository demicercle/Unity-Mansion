using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using Pathfinding;
using Pathfinding.Drawing;
using Pathfinding.RVO;
using UnityEngine;


[RequireComponent(typeof(CharacterEntity))]
public class CharacterAgent : MonoBehaviour
{
	private const float ReachDestinationScale = 100.0f;
	private const float TimeOut = 3f;
	private const float WaypointMaxEndPointDistance = 0.2f;
	private const int WaypointRoundDecimals = 1000;
	private const float WaypointMaxSearchDistance = 28.0f;
	
	public static int debugMode { get; set; }

	//[Header("Movement")] [SerializeField] private bool forceNoObstacle = false;
	[SerializeField] [Range(0.0f, 10.0f)] private float moveSpeed = 4.5f;
	[SerializeField] [Range(0.1f, 2.0f)] private float radius = 0.5f;
	[SerializeField] [Range(0.1f, 3.0f)] private float height = 1.5f;
	[SerializeField] [Min(0f)] private float minReachDistance = 0.1f;
	[SerializeField] [Range(0f, 1f)] private float priorityLeader = 0.7f;
	[SerializeField] [Range(0f, 1f)] private float priorityFollower = 0.5f;
	[SerializeField] [Range(0f, 1f)] private float priorityOther = 0.3f;
	[SerializeField] private float heightTolerance = 0.5f;
	
	[Header("Rendering")]
	[SerializeField] private List<LineRenderer> pathRenderers;
	
	[Header("Circles")]
	[SerializeField] private CircleRenderer moveTarget;
	
	//public bool PositionChanged(Vector3 p1, Vector3 p2) => p1.ToString() != p2.ToString();

	public bool updateRotation
	{
		get => aiPath.updateRotation;
		set => aiPath.updateRotation = value;
	}
	
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

	public void SetObstacle(bool value)
	{
		if (isObstacle == value)
			return;

		rvoController.enabled = !value;
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

			NavmeshRebuilder.SetNavigationDirty();
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
		var drawColor = Color.deepSkyBlue;
		if (buffer.Count <= 1) drawColor = Color.darkRed;

		if (debugMode > 0)
		{
			DebugDrawer.WireDisc(lastPoint + Vector3.up * 0.1f, Vector3.forward, GetRadius(), drawColor);
			DebugDrawer.Line(lastPoint + Vector3.up * 0.1f, dest + Vector3.up * 0.1f, drawColor);
		}
		
		return buffer.ToArray();
	}

	public void StartMove()
	{
		if(debugMode > 0) Debug.Log(this + " StartMove");
		SetObstacle(false);
		aiPath.updatePosition = aiPath.updateRotation = true;
		aiPath.simulateMovement = true;
		aiPath.isStopped = false;
		aiPath.canSearch = true;
	}

	public void StopMovement(bool clamp = false)
	{
		if(debugMode > 0) Debug.Log(this + " StopMovement()");
		DropOnGround();
		UpdateObstacle();
		if(clamp) ClampOnNavigation();
		aiPath.isStopped = true;
		aiPath.updatePosition = aiPath.updateRotation = false;
		aiPath.simulateMovement = false;
		aiPath.canSearch = false;
		ClearDestination();
	}

	public void UpdateObstacle()
	{
		SetObstacle(!_selfCharacter.IsSelected() 
		            && !_selfCharacter.IsMoving()
		            && _selfCharacter.IsCharacterRevealed() 
		            && GameTurnManager.Instance.IsTurnBasedMode());
	}

	public void DropOnGround()
	{
		if (Physics.Linecast(_selfCharacter.GetPosition() + Vector3.up * GetRadius(), _selfCharacter.GetPosition(),
			    out var groundHit, Layers.LayerGround))
		{
			aiPath.Teleport(groundHit.point);
		}
	}

	public void ClampOnNavigation()
	{
		if( NavUtils.TryGetNearestNavPosition(aiPath.position, aiPath.radius, out var newPosition))
			aiPath.Teleport(newPosition);
	}

	public bool HasWaypointTo(MonoBehaviour provider) => bestWaypoint && lastWaypointProvider &&
	                                                     lastWaypointProvider.GetComponentsInChildren<Waypoint>()
		                                                     .Contains(bestWaypoint); // TODO optim

	public Waypoint GetBestWaypoint() => bestWaypoint;
	
	public Coroutine AssignWaypointDestinationAsync(MonoBehaviour provider)
	{
		if (waypointProvider)
		{
			if (waypointProvider == provider)
				return null;
			//else
			StopAllCoroutines();
		}
		
		waypointProvider = provider;
		
		if (!waypointProvider)
			return null;
		
		return StartCoroutine(SearchBestWaypoint());
	}

	private IEnumerator SearchBestWaypoint()
	{
		float debugDrawDuration = 1.0f;
		
		bestWaypoint = null;
		waypoints = waypointProvider.GetComponentsInChildren<Waypoint>().ToList();
		
		if (debugMode > 1)
		{
			Debug.Log(this + " SearchBestWaypoint from " + _selfCharacter + " to " + waypointProvider + " waypoints=" + waypoints.Count);
			DebugDrawer.Line(_selfCharacter.GetCenterPosition(), waypointProvider.transform.position, Color.blueViolet, 1.0f);
		}

		var waypointOut = default(Waypoint);
		var startPoint = _selfCharacter.GetPosition();
		var bestLength = 0f;
		foreach (var wp in waypoints)
		{
			var endPoint = wp.GetPosition();

			if (NavUtils.NavDistance(startPoint, endPoint) > WaypointMaxSearchDistance)
				continue;

			if (!RoomTrigger.IsPositionVisible(endPoint)) // ignore not revealed waypoint
				continue;

			yield return StartCoroutine(DoAssignDestination(endPoint));
			
			if (hasDestination)
			{
				var pathLength = Utils.PathLength(lastVectorPath.ToArray());
				if (!waypointOut || pathLength < bestLength)
				{
					bestLength = pathLength;
					waypointOut = wp;
				}
			}
		}

		if (!waypointOut)
		{
			if(debugMode > 0) Debug.Log(this + " no waypoint found! provider=" + waypointProvider + " count=" + waypoints.Count);
		}
		else
		{
			lastWaypointProvider = waypointProvider;
			bestWaypoint = waypointOut; // assign it so it can returns "GetBestWaypoint"
			if (debugMode > 0)
			{
				if(debugMode > 1) Debug.Log("new destination set bestWaypoint=" + bestWaypoint);
				DebugDrawer.Line(_selfCharacter.GetCenterPosition(), bestWaypoint.GetPosition(), Color.cyan, debugDrawDuration);
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
	
	public Coroutine AssignDestination(Vector3 hitPoint)
	{
		//if(debugMode > 1) Debug.Log(this + " Assign_new_Destination");
		ClearPath();
		
		if (IsFollowing()) 
			AssignFollowTransform(null);
		
		StopAllCoroutines();
		return StartCoroutine(DoAssignDestination(hitPoint));
	}

	private IEnumerator DoAssignDestination(Vector3 dest)
	{
		hasDestination = false;
		
		lastVectorPath.Clear();

		aiPath.destination = dest;
		aiPath.canSearch = true;
		aiPath.SearchPath();
		
		do
		{
			yield return null;
			aiPath.GetRemainingPath(lastVectorPath, out var _);
			//if (debugMode > 0) DebugDrawer.Path(lastVectorPath.ToArray(), false, Color.yellow, 0f);
		}
		while (aiPath.pathPending);

		hasDestination = aiPath.hasPath;
		//if (debugMode > 0) DebugDrawer.WireDisc(dest, aiPath.endReachedDistance, hasDestination ? Color.green : Color.yellow, 1.0f);
	}
	
	public bool IsFollowing() => aiDestinationSetter && aiDestinationSetter.target;

	public void AssignFollowTransform(Transform followTarget, float distance = 0f)
	{
		if(!aiDestinationSetter) aiDestinationSetter = GetComponent<AIDestinationSetter>();
		if(!aiDestinationSetter) aiDestinationSetter = gameObject.AddComponent<AIDestinationSetter>();
		if (aiDestinationSetter.target == followTarget) return;
		if (debugMode > 0) Debug.Log(this + " Assign followTarget=" + followTarget + " " + distance);
		hasDestination = false;
		if (IsMoving())
		{
			StopMovement();
		}
		aiDestinationSetter.enabled = followTarget;
		aiDestinationSetter.target = followTarget;
		aiPath.canSearch = followTarget != null;
		aiPath.endReachedDistance = Mathf.Max(distance, GetMinReachDistance());
		if (followTarget && !IsMoving()) StartMove();
	}

	public void ClearDestination()
	{
		debugString = "";
		bestWaypoint = null;
		hasDestination = false;
		lastVectorPath = new List<Vector3>();
		StopAllCoroutines();
	}

	public bool ReachedDestination() => ReachedDestination(aiPath.destination);
	
	public bool ReachedDestination(Vector3 dest)
	{
		if (aiPath.hasPath && aiPath.reachedDestination)
			return true;
		
		var dist = NavUtils.NavDistance(aiPath.position, dest);
		var reach = Mathf.Max(GetMinReachDistance(), aiPath.endReachedDistance);
		dist = float.Parse(dist.ToString("N2"));
		reach = float.Parse(reach.ToString("N2"));
		return dist <= reach;
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
			aiPath.updatePosition = aiPath.updateRotation = false;
			SetObstacle(false);
		}

		rvoController.enabled = value;
		rvoController.layer = rvoController.enabled ? RVOLayer.DefaultAgent : RVOLayer.Layer2;
	}
	
	public Vector3 velocity => aiPath.velocity;

	public bool IsMoving() => !aiPath.isStopped /*&& !aiPath.pathPending && velocity.magnitude > Consts.MinMagnitude)*/;
	   
	public float GetMinReachDistance() => minReachDistance;

	public void SetReachDistance(float value) => aiPath.endReachedDistance = value;

	public float rotationSpeed => aiPath.rotationSpeed;

	///private float lastAssignTime;
	public bool hasDestination { get; private set; }

	//private Transform followTarget;
	private AIDestinationSetter aiDestinationSetter;
	private AIPath aiPath;
	private CharacterEntity _selfCharacter;
	private GameTurnManager _gameTurnManager; // TODO
	private List<Vector3> lastVectorPath = new List<Vector3>();
	private MonoBehaviour waypointProvider, lastWaypointProvider;
	private Waypoint bestWaypoint;
	private List<Waypoint> waypoints = new List<Waypoint>();
	private RVOController rvoController;
	private string debugString;
	private IAttackable lastTarget;
	private bool isObstacle;
	private Vector3[] drawPath;
	private float maxDrawLength;

	private void OnDrawGizmosSelected()
	{
		if (!Application.isPlaying)
			UpdateComponent();
	}

	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
		_selfCharacter.onUseAction += OnUseAction;
		_selfCharacter.onPartyIndexChanged += OnPartyIndexChanged;
		
		UpdateComponent();
		
		_gameTurnManager = GameTurnManager.Instance;
		CharacterSelection.selectionChanged += SelectionChanged;

		moveTarget.name += " (" + name + ")";
		moveTarget.radius = GetRadius() * 0.5f;
		moveTarget.transform.SetParent(null, true);
		moveTarget.gameObject.SetActive(false);
		
		SetAgentActive(false);
	}

	private void UpdateComponent()
	{
		aiPath = GetComponent<AIPath>();
		aiPath.radius = radius;
		aiPath.height = height;
		aiPath.maxSpeed = moveSpeed;
		if(minReachDistance > 0f) aiPath.endReachedDistance = minReachDistance;
		if(Application.isPlaying) aiPath.gravity = Physics.gravity; // force gravity
		rvoController = GetComponent<RVOController>();
	}
	
	private void OnDestroy()
	{
		if(moveTarget) Destroy(moveTarget.gameObject);
		_selfCharacter.onUseAction -= OnUseAction;
		_selfCharacter.onPartyIndexChanged -= OnPartyIndexChanged;
		CharacterSelection.selectionChanged -= SelectionChanged;
	}

	private void Start()
	{
		if (!hasDestination) aiPath.isStopped = true; // Start
	}

	private void OnUseAction(CharacterAction action, object param)
	{
		StopMovement();
	}

	private void OnPartyIndexChanged(int newIndex, int partyCount)
	{
		UpdateRVO();
	}

	private void SelectionChanged()
	{
		UpdateRVO();
		UpdateObstacle();
		if(!IsMoving()) ClearDestination();
	}

	private void UpdateRVO()
	{
		rvoController.lockWhenNotMoving = _selfCharacter.IsSelected() && _selfCharacter.IsControllableByPlayer() 
		                                                              && (GameTurnManager.Instance.IsPlayerTurn() || !GameTurnManager.Instance.IsTurnBasedMode());
		
		rvoController.priority = rvoController.lockWhenNotMoving ? priorityLeader 
								: _selfCharacter.InParty() ? priorityFollower 
								: priorityOther;
	}

	private void LateUpdate()
	{
		//rvoController.enabled = !_selfCharacter.IsCurrentTurn() || !_selfCharacter.IsSelected();
		
		UpdateMoveTarget(_selfCharacter.IsMoving());
		
		if (debugMode > 0)
		{
			debugString = "";

			if (lastVectorPath.Count > 0)
			{
				debugString += "path:" + lastVectorPath.Count + "\n";
				
				if(hasDestination)
					debugString += NavUtils.NavDistance(aiPath.position, aiPath.destination).ToString("N2") 
					               + ":" + aiPath.endReachedDistance.ToString("N2")
					               + ":" + NavUtils.NavDistance(aiPath.endOfPath, aiPath.destination).ToString("N2");
			}
				
			if (lastVectorPath.Count > 0) DebugDrawer.Path(lastVectorPath.ToArray(), false,Color.cyan);
			if (hasDestination) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.cyan);
			if (rvoController.enabled) DebugDrawer.WireCapsule(transform.position, transform.rotation, rvoController.radius, rvoController.height, Color.magenta, 0f);
		}
	}

	private IEnumerator WaitNavmesh()
	{
		do
		{
			aiPath.canSearch = false;
			yield return null;
		} while (NavmeshRebuilder.IsNavigationDirty());
		aiPath.canSearch = true;
	}


	[SerializeField] [Min(0f)] private float endPointDistance = 0.25f;
	[SerializeField] [Min(0f)] private float endPointYTolerance = 0.5f;

	public void ClearPath()
	{
		pathRenderers[0].enabled = false;
		pathRenderers[1].enabled = false;
	}
	
	public void DrawPath()
	{
		drawPath = lastVectorPath.ToArray();
		pathRenderers[0].enabled = hasDestination;
		if (pathRenderers[0].enabled) // draw path thin
		{
			pathRenderers[0].startColor = pathRenderers[0].endColor = ColorSettings.GetDisabledColor(_selfCharacter.GetCurrentFaction());
			pathRenderers[0].positionCount = drawPath.Length;
			pathRenderers[0].useWorldSpace = true;
			pathRenderers[0].SetPositions(drawPath);
//			Debug.Log(this + " draw path " + drawPath.Length);
		}
		
		drawPath = lastVectorPath.ToArray();
		pathRenderers[1].enabled = !IsMoving();
		if (pathRenderers[1].enabled) // draw path thick
		{
			maxDrawLength = _selfCharacter.GetRemainingMovePoints();
			if (_selfCharacter.IsSelected() && _selfCharacter.IsControllableByPlayer()
			                                && GameInput.Instance.TryGetMouseOver<CharacterEntity>(out var mouseOverCharacter))
			{
				if(_selfCharacter.ShouldAttack(mouseOverCharacter))
					maxDrawLength = Mathf.Min(maxDrawLength, drawPath.Length - _selfCharacter.GetAttackReachDistance(mouseOverCharacter));
			}
			drawPath = Utils.LimitPathTotalLength(drawPath, maxDrawLength);
			pathRenderers[1].startColor = pathRenderers[1].endColor = ColorSettings.GetCharacterColor(_selfCharacter, true, true);
			pathRenderers[1].positionCount = drawPath.Length;
			pathRenderers[1].useWorldSpace = true;
			pathRenderers[1].SetPositions(drawPath);
		}
	}

	private void UpdateMoveTarget(bool isBlinking)
	{
		if (!moveTarget) return; // editor error
		
		moveTarget.gameObject.SetActive(ShowMoveTarget());
		if (!moveTarget.gameObject.activeSelf)
			return;
		
		moveTarget.transform.position = aiPath.destination;
		moveTarget.SetColor(ColorSettings.GetCharacterColor(_selfCharacter, true, true));
		TransformAnimations.BounceRealtime(moveTarget.transform, isBlinking);
	}

	private bool ShowMoveTarget()
	{
		if (IsFollowing() || !_selfCharacter.IsPlayer())
			return false;

		if (bestWaypoint)
			return true;

		return hasDestination;
	}

	public void SetRotationY(float eulerAnglesY)
	{
		//aiPath.updateRotation = false;
		aiPath.rotation = Quaternion.Euler(0, eulerAnglesY, 0);
	}

	private void OnGUI()
	{
		if (debugMode > 0)
		{
			if (!string.IsNullOrEmpty(debugString))
			{
				Utils.DrawWorldGUI(transform.position, debugString);
			}
		}
	}
}