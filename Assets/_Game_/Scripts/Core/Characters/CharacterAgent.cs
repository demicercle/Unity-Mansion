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
	private const float PartialPathTolerance = 0.5f;
	private const float PartialPathHeightPenalty = 2.0f;
	
	public static int debugMode { get; set; }

	//[Header("Movement")] [SerializeField] private bool forceNoObstacle = false;
	[SerializeField] [Range(0.0f, 10.0f)] private float moveSpeed = 4.5f;
	[SerializeField] [Range(0.1f, 2.0f)] private float radius = 0.5f;
	[SerializeField] [Range(0.1f, 3.0f)] private float height = 1.5f;
	[SerializeField] [Min(0f)] private float minReachDistance = 0.1f;
	[SerializeField] [Range(0f, 1f)] private float priorityLeader = 0.7f;
	[SerializeField] [Range(0f, 1f)] private float priorityFollower = 0.5f;
	[SerializeField] [Range(0f, 1f)] private float priorityOther = 0.3f;
	
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
		UpdatePathAndTarget(true);
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
		waypoints = waypointProvider.GetComponentsInChildren<Waypoint>();
		
		if (debugMode > 1)
		{
			Debug.Log(this + " SearchBestWaypoint from " + _selfCharacter + " to " + waypointProvider + " waypoints=" + waypoints.Length);
			DebugDrawer.Line(_selfCharacter.GetCenterPosition(), waypointProvider.transform.position, Color.blueViolet, 1.0f);
		}

		var waypointOut = default(Waypoint);
		var startPoint = _selfCharacter.GetPosition();
		var bestLength = 0f;
		var bestPath = default(ABPath);
		var index = 0;
		foreach (var wp in waypoints)
		{
			var endPoint = wp.GetPosition();

			if (NavUtils.NavDistance(startPoint, endPoint) > WaypointMaxSearchDistance)
				continue;

			if (!RoomTrigger.IsPositionVisible(endPoint)) // ignore not revealed waypoint
				continue;

			
			yield return StartCoroutine(WaitNavmesh());
			
			aiPath.canSearch = true;
				
			// Crée une requête de chemin
			ABPath waypointPath = ABPath.Construct(startPoint, endPoint, null);
			waypointPath.calculatePartial = false;
			
			// Envoie la requête au système de pathfinding
			AstarPath.StartPath(waypointPath);
			
			// Attends que le chemin soit calculé (asynchrone)
			if (debugMode > 1) Debug.Log(" StartPath to " + wp + " pathRequest=" + waypointPath);
			yield return StartCoroutine(waypointPath.WaitForPath());
				
			// Récupère le chemin calculé
			if (waypointPath.error)
			{
				if (debugMode > 1) DebugDrawer.Line(startPoint, endPoint, Color.red, debugDrawDuration);
				
				Debug.LogWarning("Erreur lors du calcul du chemin : " + waypointPath.errorLog);
			}
			else
			{
				//if (debugMode > 1) DebugDrawer.Line(startPoint, endPoint, Color.darkBlue, debugDrawDuration);
				Vector3[] pathPoints = waypointPath.vectorPath.ToArray();
				bool isPartial = waypointPath.CompleteState == PathCompleteState.Partial || 
				                 !NavUtils.TryGetNearestNavPosition(pathPoints.LastOrDefault(), WaypointMaxEndPointDistance, out var _);
				
				if (isPartial)
				{
					if (debugMode > 1) Debug.Log(index + " partialPath, skip it       pathPoints=" + pathPoints.Length);
					if(debugMode > 1) DebugDrawer.Path(pathPoints, false, Color.darkRed, debugDrawDuration);
				}
				else
				{
					if(debugMode > 1) DebugDrawer.Path(pathPoints, false, Color.blueViolet, debugDrawDuration);
					float pathLength = waypointPath.GetTotalLength();
					if (waypointOut && (int)pathLength * WaypointRoundDecimals == (int)bestLength * WaypointRoundDecimals) // sometimes results are wrong, this is a hak
					{
						var bestDist = NavUtils.NavDistance(startPoint, waypointOut.GetPosition());
						var pathDist = NavUtils.NavDistance(startPoint, endPoint);
						if (bestDist < pathDist)
							pathLength = -1f; // override path length because the path is actually not shorter
					}
					if(debugMode > 1) Debug.Log(index+ " waypointOut=" + waypointOut + " pathLength=" + pathLength + " from bestLength=" + bestLength + " path.Length=" + pathPoints.Length);
					if (!waypointOut || (pathLength > 0 && pathLength < bestLength))
					{
						bestPath = waypointPath;
						bestLength = pathLength;
						waypointOut = wp;
					}
				}
				
				// Tu peux maintenant utiliser 'waypoints' comme tu le souhaites
				index++;
			}
		}

		if (!waypointOut)
		{
			if(debugMode > 0) Debug.Log(this + " no waypoint found! provider=" + waypointProvider + " count=" + waypoints.Length);
		}
		else
		{
			lastWaypointProvider = waypointProvider;
			aiPath.destination = waypointOut.GetPosition();
			aiPath.SetPath(bestPath);
			
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
		if(debugMode > 1) Debug.Log(this + " Assign_new_Destination");
			
		if (IsFollowing()) 
			AssignFollowTransform(null);

		StopAllCoroutines();
		return StartCoroutine(DoAssignDestination(hitPoint));
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
		UpdatePathAndTarget(true);
	}

	public void ClearDestination()
	{
		debugString = "";
		bestWaypoint = null;
		hasDestination = false;
		lastSearchPath = null;
		lastVectorPath = Array.Empty<Vector3>();
		StopAllCoroutines();
		UpdatePathAndTarget(true);
	}

	public bool ReachedDestination() => ReachedDestination(aiPath.destination);
	
	public bool ReachedDestination(Vector3 dest)
	{
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
	private bool hasDestination;

	//private Transform followTarget;
	private AIDestinationSetter aiDestinationSetter;
	private AIPath aiPath;
	private CharacterEntity _selfCharacter;
	private GameTurnManager _gameTurnManager; // TODO
	private Vector3[] lastVectorPath;
	private ABPath lastSearchPath;
	private MonoBehaviour waypointProvider, lastWaypointProvider;
	private Waypoint bestWaypoint;
	private Waypoint[] waypoints;
	private RVOController rvoController;
	private string debugString;
	private IAttackable lastTarget;
	private bool isObstacle;
	private List<Waypoint> ignoreWaypointList  = new List<Waypoint>();
	private Vector3[] drawPath;
	private float maxDrawLength;
	private bool[] shouldDrawPath = new []{false,false};
	private bool[] isDrawingPath = new []{false,false};

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
		_gameTurnManager.onTurnEnded += UpdatePathAndTarget;
		_gameTurnManager.onTurnStarted += UpdatePathAndTarget;
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

	private void OnPartyIndexChanged(int newIndex, int partyCount)
	{
		UpdateRVO();
	}

	private void SelectionChanged()
	{
		UpdateRVO();
		UpdateObstacle();
		if(!IsMoving()) ClearDestination();
		UpdatePathAndTarget(true);
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
		
		if (debugMode > 0)
		{
			debugString = "";

			if (lastVectorPath?.Length > 0)
			{
				debugString += "path:" + lastVectorPath.Length + "\n";
				
				if(hasDestination)
					debugString += NavUtils.NavDistance(aiPath.position, aiPath.destination).ToString("N2") 
					               + ":" + aiPath.endReachedDistance.ToString("N2")
					               + ":" + CalculatePartialPathDistance(aiPath.endOfPath, aiPath.destination).ToString("N2");
			}
				
			if (lastVectorPath?.Length > 0) DebugDrawer.Path(lastVectorPath, false,Color.cyan);
			if (hasDestination) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance,Color.cyan);
			if (rvoController.enabled) DebugDrawer.WireCapsule(transform.position, transform.rotation, rvoController.radius, rvoController.height, Color.magenta, 0f);
		}
		
		
		//if (CharacterRoutines.AnyActiveRoutines()) // todo
		//	UpdatePathAndTarget(true);
	}

	private IEnumerator DoAssignDestination(Vector3 dest)
	{
		hasDestination = false;
		
		yield return StartCoroutine(DoSearchPathAsync(dest));

		if (!lastSearchPath.error)
		{
			hasDestination = true;
			aiPath.destination = dest;
			aiPath.SetPath(lastSearchPath);
		}
	}

	private bool ValidateDistance(Vector3 endPoint, Vector3 dest) => CalculatePartialPathDistance(endPoint, dest) <= PartialPathTolerance;
	
	private float CalculatePartialPathDistance(Vector3 endPoint, Vector3 dest) => NavUtils.NavHDistance(endPoint, dest) 
	                                                                     + NavUtils.NavVDistance(endPoint, dest) * PartialPathHeightPenalty;

	private IEnumerator DoSearchPathAsync(Vector3 newDest) => DoSearchPathAsync(newDest, newDest);
	private IEnumerator DoSearchPathAsync(Vector3 newDest, Vector3 finalDest)
	{
		if (debugMode > 1) Debug.Log(this + " DoSearchPathAsync");

		yield return StartCoroutine(WaitNavmesh());
		
		// Crée une requête de chemin
		var startPoint = aiPath.position;
		var endPoint = newDest;
		lastSearchPath = ABPath.Construct(startPoint, endPoint, null);
		lastSearchPath.calculatePartial = false;
		
		lastVectorPath = Array.Empty<Vector3>();
		
		// Envoie la requête au système de pathfinding
		AstarPath.StartPath(lastSearchPath);
		yield return StartCoroutine(lastSearchPath.WaitForPath());
		
		if (lastSearchPath.error || !ValidateDistance(lastSearchPath.endPoint, newDest))
		{
			if(debugMode > 0) Debug.Log(this + " path error to " + newDest);
			foreach (var wp in Waypoint.GetOrderedWaypointsFrom(finalDest))
			{
				if (!ignoreWaypointList.Contains(wp))
				{
					if(debugMode > 0) Debug.Log(this + " try reach waypoint " + wp);
					ignoreWaypointList.Add(wp);
					yield return StartCoroutine(DoSearchPathAsync(wp.GetPosition(), finalDest));
					yield break;
				}
			}
			
			Debug.LogWarning(this + " cannot find waypoint to destination, abort");
			yield break;
		}

		lastVectorPath = lastSearchPath.vectorPath.ToArray();
		
		if (debugMode > 1) Debug.Log(this + " DoSearchPathAsync onComplete lastPath=" + lastVectorPath.Length);
		ignoreWaypointList.Clear();
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
	
	private bool IsDestinationValid(Vector3 target)
	{
		var layeredGraph = AstarPath.active.graphs.OfType<LayerGridGraph>().First();
	    
        // Crée une contrainte pour forcer la bonne couche
        NNConstraint constraint = NNConstraint.Default;
        constraint.constrainWalkability = true;
        constraint.constrainDistance = true;

        // Trouve le nœud le plus proche SUR LA BONNE COUCHE
        NNInfo nearest = AstarPath.active.GetNearest(target, constraint);

        if (nearest.node != null)
        {
            // Vérifie si le nœud est sur la bonne couche (Y)
            var node = nearest.node as GridNode;
            if (node != null)
            {
                // Calcule la différence de hauteur entre la cible et le nœud
                float heightDiff = Mathf.Abs(target.y - (float)node.position.y);

                // Si la différence est trop grande, cherche un nœud sur une autre couche
                if (heightDiff > layeredGraph.nodeSize) // Seuil = taille d'un nœud
                {
	                return false;
                }
            }
        }

        return true;
	}
	
	private void UpdatePathAndTarget() => UpdatePathAndTarget(true);
	
	private void UpdatePathAndTarget(bool force)
	{
		//TODO: controller.IsActing(), controller.IsMoving(), controller.IsRotating()

		shouldDrawPath[0] = hasDestination // "thin" path
		                    && !IsFollowing()
		                    && !IsMoving()
		                    && GameTurnManager.Instance.IsTurnBasedMode()
		                    && CharacterSelection.IsCharacterSelected(_selfCharacter)
		                    && _selfCharacter.IsControllableByPlayer()
		                    && (!CharacterRoutines.AnyActiveRoutines() || _selfCharacter.IsMoving())
		                    && lastVectorPath.Length > 1;

		shouldDrawPath[1] = hasDestination  // "thin" path
		                    && !IsFollowing()
		                    && !IsMoving()
		                    && GameTurnManager.Instance.IsTurnBasedMode()
		                    && CharacterSelection.IsCharacterSelected(_selfCharacter) 
		                    && _selfCharacter.IsControllableByPlayer() 
		                    && !CharacterRoutines.AnyActiveRoutines()
		                    && lastVectorPath.Length > 1;
								//&& !controller.IsRotating()
								//&& !controller.IsMoving();

		if (lastTarget != null)
		{
			if ( (_selfCharacter.ShouldAttack(lastTarget) && _selfCharacter.IsAtAttackRange(lastTarget))
			    || (lastTarget is CharacterEntity && _selfCharacter.ShouldTalk(lastTarget as CharacterEntity) 
			                                      && _selfCharacter.GetDistanceTo(lastTarget.GetPosition()) <= _selfCharacter.GetTalkDistance(lastTarget as CharacterEntity)))
			{
				shouldDrawPath[0] = shouldDrawPath[1] = false;
			}
		}
		
		if (force || shouldDrawPath[0] != isDrawingPath[0])
		{
			drawPath = lastVectorPath;
			pathRenderers[0].enabled = isDrawingPath[0] = shouldDrawPath[0];
			if (pathRenderers[0].enabled) // draw path thin
			{
				pathRenderers[0].startColor = pathRenderers[0].endColor = ColorSettings.GetDisabledColor(_selfCharacter.GetCurrentFaction());
				pathRenderers[0].positionCount = drawPath.Length;
				pathRenderers[0].useWorldSpace = true;
				pathRenderers[0].SetPositions(drawPath);
			}
		}

		if (force || shouldDrawPath[1] != isDrawingPath[1])
		{
			drawPath = lastVectorPath;
			pathRenderers[1].enabled = isDrawingPath[1] = shouldDrawPath[1];
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
		
		UpdateMoveTarget(_selfCharacter.IsMoving());
	}

	private bool ShowMoveTarget()
	{
		if (IsFollowing() || !_selfCharacter.IsPlayer())
			return false;

		if (bestWaypoint)
			return true;

		return hasDestination;
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