using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using Pathfinding;
using Pathfinding.Drawing;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterAgent : MonoBehaviour
{
	private const float TimeOut = 3f;
	
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

		if (NavmeshRebuilder.debugMode > 0)
		{
			DebugDrawer.WireDisc(lastPoint + Vector3.up * 0.1f, Vector3.forward, maxNavDist, drawColor);
			DebugDrawer.Line(lastPoint + Vector3.up * 0.1f, dest + Vector3.up * 0.1f, drawColor);
		}
		
		return buffer.ToArray();
	}

	public bool isPathValid => lastPath?.Length > 1;

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
	
	public void AssignDestination(Vector3 hitPoint, bool force)
	{
		if (force || !hasDestination)
		{
			AssignFollowTransform(null);
			hasDestination = true;
			
			aiPath.destination = hitPoint;
			aiPath.SearchPath();
			
			UpdatePathAndTarget(true);
		}
	}

	public bool IsFollowing() => aiDestinationSetter && aiDestinationSetter.enabled;

	public void AssignFollowTransform(Transform followTarget, float distance = 0f)
	{
		hasDestination = false;
		if(!aiDestinationSetter) aiDestinationSetter = GetComponent<AIDestinationSetter>();
		if(!aiDestinationSetter) aiDestinationSetter = gameObject.AddComponent<AIDestinationSetter>();
		if (aiDestinationSetter.target != followTarget && IsMoving()) StopMovement();
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
		StopMovement();
		
		if(debugMode > 0) Debug.Log(this + " SetAgentActive=" + value);
		aiPath.enabled = value;
		
		if (aiPath.enabled)
		{
			aiPath.Teleport(transform.position);
		}
	}

	private void OnSearchPath()
	{
		lastPath = GetCurrentPath();
		if (debugMode > 1) Debug.Log(this + " OnSearchPath lastPath=" + lastPath.Length);
		if (debugMode > 0) DebugDrawer.Path(lastPath, false, Color.cyan);
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
	private Vector3[] lastPath;

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

	void UpdateMoveTarget(bool isBlinking)
	{
		if (!hasDestination || !aiPath.hasPath || IsFollowing()) //hide
		{
			moveTarget.gameObject.SetActive(false);
			return;
		}

		//Debug.Log(this + " show moveTarget");
		moveTarget.gameObject.SetActive(true);
		moveTarget.transform.position = aiPath.destination;
		
		if (isPathValid)
			moveTarget.SetColor(ColorSettings.GetCharacterColor(_character, true, true));
		else 
			moveTarget.SetColor(ColorSettings.GetDisabledColor());
		
		TransformAnimations.BounceRealtime(moveTarget.transform, isBlinking);
	}
}