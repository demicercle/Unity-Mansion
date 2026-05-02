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
	private const float WaypointSearchSeconds = 0.10f;
	
	public static int debugMode { get; set; }

	//[Header("Movement")] [SerializeField] private bool forceNoObstacle = false;
	[SerializeField] [Range(0.0f, 10.0f)] private float moveSpeed = 4.5f;
	[SerializeField] [Range(0.1f, 2.0f)] private float radius = 0.5f;
	[SerializeField] [Range(0.1f, 3.0f)] private float height = 1.5f;
	[SerializeField] [Min(0f)] private float minReachDistance = 0.1f;
	[SerializeField] private bool reversePriority = false;
	
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

	private bool isObstacle;
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

	public bool isPathValid => lastPath?.Length > 1 && RoomTrigger.IsPositionVisible(lastPath.Last());

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
		
		if (debugMode > 1)
		{
			Debug.Log(this + " SearchBestWaypoint from " + _selfCharacter + " to " + waypointProvider + " waypoints=" + waypoints.Length);
			DebugDrawer.Line(_selfCharacter.GetCenterPosition(), waypointProvider.transform.position, Color.blueViolet, 1.0f);
		}

		var waypointOut = default(Waypoint);
		var startPoint = _selfCharacter.GetPosition();
		var bestLength = 0f;
		var index = 0;
		foreach (var wp in waypoints)
		{
			var endPoint = wp.GetPosition();

			if (NavUtils.NavDistanceRoundY(startPoint, endPoint) > WaypointMaxSearchDistance)
				continue;

			if (!RoomTrigger.IsPositionVisible(endPoint)) // ignore not revealed waypoint
				continue;

			
			yield return StartCoroutine(WaitNavmesh());
			
			aiPath.canSearch = true;
				
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
					if (waypointOut && (int)pathLength * WaypointRoundDecimals == (int)bestLength * WaypointRoundDecimals) // sometimes results are wrong, this is a hak
					{
						var bestDist = NavUtils.NavDistanceRoundY(startPoint, waypointOut.GetPosition());
						var pathDist = NavUtils.NavDistanceRoundY(startPoint, endPoint);
						if (bestDist < pathDist)
							pathLength = -1f; // override path length because the path is actually not shorter
					}
					if(debugMode > 1) Debug.Log(index+ " waypointOut=" + waypointOut + " pathLength=" + pathLength + " from bestLength=" + bestLength + " path.Length=" + pathPoints.Length);
					if (!waypointOut || (pathLength > 0 && pathLength < bestLength))
					{
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
			AssignDestination(waypointOut.GetPosition(), true);
			do yield return null;
			while (aiPath.pathPending);
			
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

	private IAttackable lastTarget;
	
	public void AssignDestination(Vector3 hitPoint, bool force, IAttackable target = null)
	{
		if (force || !hasDestination)
		{
			if(debugMode > 1) Debug.Log(this + " Assign_new_Destination " + force);
			
			if (IsFollowing()) 
				AssignFollowTransform(null);

			if(debugMode > 1) Debug.Log(this + " aiPath.SearchPath() ");
			aiPath.endReachedDistance = minReachDistance;
			aiPath.destination = hitPoint;
			aiPath.canSearch = true;
			aiPath.SearchPath();
			
			hasDestination = true;
			lastTarget = target;
			UpdatePathAndTarget(true);
		}
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
		if (hasDestination) // ClearDestination
		{
			debugString = "";
			bestWaypoint = null;
			hasDestination = false;
//			Debug.Log(aiPath + " hasDestination=" + hasDestination);
			UpdatePathAndTarget(true);
		}
	}

	private string debugString;
	public bool ReachedDestination() => ReachedDestination(aiPath.destination);
	public bool ReachedDestination(Vector3 dest)
	{
		var dist = NavUtils.NavDistanceRoundY(aiPath.position, dest);
		var reach = Mathf.Max(GetMinReachDistance(), aiPath.endReachedDistance);
		dist = float.Parse(dist.ToString("N2"));
		reach = float.Parse(reach.ToString("N2"));
		debugString = $"{dist}/{reach}";
		return dist <= reach;
	}

	public bool IsActive() => aiPath && aiPath.enabled;
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

	public void RecalculatePath()
	{
		if (hasDestination)
		{
			AssignDestination(aiPath.destination, true);
		}
	}
	private void OnSearchPath()
	{
		lastPath = GetCurrentPath();
		if (debugMode > 2) Debug.Log(this + " OnSearchPath lastPath=" + lastPath.Length);
		if (debugMode > 2) DebugDrawer.Path(lastPath, false, Color.blue);
		UpdatePathAndTarget(true);
	}

	public Coroutine SearchPathAsync(Vector3 dest, bool clamp, System.Action onComplete = null)
	{
		return StartCoroutine(DoSearchPathAsync(dest, clamp, onComplete));
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
	
	private IEnumerator DoSearchPathAsync(Vector3 dest, bool clamp, System.Action onComplete = null)
	{
		if (debugMode > 1) Debug.Log(this + " DoSearchPathAsync");
		// WaitCharacterMoved

		yield return StartCoroutine(WaitNavmesh());

		if (clamp && NavUtils.TryGetNearestNavPosition(dest, aiPath.endReachedDistance, out dest))
		{
			if(debugMode > 1) Debug.Log(this + " dest clamped to navmesh");
		}
		
		var timeout = 0.0f;
		do
		{
			if(!aiPath.pathPending || !aiPath.hasPath)
				AssignDestination(dest, true);

			yield return StartCoroutine(WaitNavmesh());
			
			UpdatePathAndTarget(true);
			if (timeout >= TimeOut) break;
			if(debugMode > 0) DebugDrawer.Line(_selfCharacter.GetPosition(), dest, Color.blue, 0f);
			yield return null;
			lastPath = GetCurrentPath();
			if (debugMode > 1) Debug.Log(this + " DoSearchPathAsync lastPath=" + lastPath.Length);
			timeout += Time.deltaTime;
		}
		while (aiPath.pathPending || lastPath.Length <= 1);

		UpdatePathAndTarget(true);
		
		if (timeout >= TimeOut)
		{
			Debug.LogWarning(this + " pathPending timeout!");
			yield break;
		}
		
		if (debugMode > 1) Debug.Log(this + " DoSearchPathAsync onComplete lastPath=" + lastPath.Length);
		onComplete?.Invoke();
		//var path = GetCurrentPath();
	}
	
	public Vector3 velocity => aiPath.velocity;

	public bool IsMoving() => !aiPath.isStopped /*&& !aiPath.pathPending && velocity.magnitude > Consts.MinMagnitude)*/;
	   
	public float GetMinReachDistance() => minReachDistance;

	public void SetReachDistance(float value) => aiPath.endReachedDistance = value;

	public void SetDefaultReachDistance() => aiPath.endReachedDistance = minReachDistance;

	/*public void EnableAvoidance(bool b)
	{
		rvoController.enabled = b || forceNoObstacle;
	}*/
	   
	public bool pathPending => aiPath.pathPending || lastPath == null || lastPath.Length <= 1;

	public float rotationSpeed => aiPath.rotationSpeed;

	///private float lastAssignTime;
	private bool hasDestination;

	//private Transform followTarget;
	private AIDestinationSetter aiDestinationSetter;
	private AIPath aiPath;
	private CharacterEntity _selfCharacter;
	private GameTurnManager _gameTurnManager; // TODO
	private CharacterAnimation _characterAnimation;
	[HideInInspector] private Vector3[] lastPath; //hide in unity because it crashes (too much things to display probably)
	private MonoBehaviour waypointProvider, lastWaypointProvider;
	private Waypoint bestWaypoint;
	private Waypoint[] waypoints;
	private RVOController rvoController;

	private void OnDrawGizmosSelected()
	{
		if (!Application.isPlaying)
			UpdateComponent();
	}

	private void Awake()
	{
		_characterAnimation = GetComponent<CharacterAnimation>();
		_selfCharacter = GetComponent<CharacterEntity>();
		_selfCharacter.onUseAction += OnUseAction;
		_selfCharacter.onPartyIndexChanged += OnPartyIndexChanged;
		
		_gameTurnManager = GameTurnManager.Instance;
		_gameTurnManager.onTurnEnded += UpdatePathAndTarget;
		_gameTurnManager.onTurnStarted += UpdatePathAndTarget;
		CharacterSelection.selectionChanged += SelectionChanged;
		
		UpdateComponent();
		aiPath.onSearchPath += OnSearchPath;

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
		aiPath.onSearchPath -= OnSearchPath;
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
		rvoController.priority = CharacterSelection.IsCharacterSelected(_selfCharacter) ? 0.90f : _selfCharacter.InParty() ? 0.66f : 0.33f;
		if(reversePriority) rvoController.priority = 1f - rvoController.priority;
		rvoController.lockWhenNotMoving = _selfCharacter.IsSelected();
	}

	private void LateUpdate()
	{
		//rvoController.enabled = !_selfCharacter.IsCurrentTurn() || !_selfCharacter.IsSelected();
		
		if (debugMode > 0)
		{
			if (lastPath?.Length > 0) DebugDrawer.Path(lastPath, false, isPathValid ? Color.cyan : Color.red);
			if (hasDestination) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, isPathValid ? Color.cyan : Color.red);
			if (rvoController.enabled) DebugDrawer.WireCapsule(transform.position, transform.rotation, rvoController.radius, rvoController.height, Color.magenta, 0f);
		}
		
		
		//if (CharacterRoutines.AnyActiveRoutines()) // todo
		//	UpdatePathAndTarget(true);
	}

	private Vector3[] drawPath;
	private float maxDrawLength;
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
		                    && CharacterSelection.IsCharacterSelected(_selfCharacter)
		                    && _selfCharacter.IsControllableByPlayer()
		                    && (!CharacterRoutines.AnyActiveRoutines() || _selfCharacter.IsMoving())
		                    && lastPath.Length > 1;

		shouldDrawPath[1] = hasDestination  // "thin" path
		                    && !IsFollowing()
		                    && !IsMoving()
		                    && GameTurnManager.Instance.IsTurnBasedMode()
		                    && CharacterSelection.IsCharacterSelected(_selfCharacter) 
		                    && _selfCharacter.IsControllableByPlayer() 
		                    && !CharacterRoutines.AnyActiveRoutines()
		                    && lastPath.Length > 1;
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
			drawPath = lastPath;
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
			drawPath = lastPath;
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

	bool ShowMoveTarget()
	{
		if (IsFollowing() || !_selfCharacter.IsPlayer())
			return false;

		if (bestWaypoint)
			return true;

		return hasDestination;
	}

	void UpdateMoveTarget(bool isBlinking)
	{
		if (!moveTarget) return; // editor error
		
		moveTarget.gameObject.SetActive(ShowMoveTarget());
		if (!moveTarget.gameObject.activeSelf)
			return;
		
		moveTarget.transform.position = aiPath.destination;
		moveTarget.SetColor(isPathValid ? ColorSettings.GetCharacterColor(_selfCharacter, true, true) 
			: ColorSettings.GetDisabledColor(_selfCharacter.GetCurrentFaction()));
		
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