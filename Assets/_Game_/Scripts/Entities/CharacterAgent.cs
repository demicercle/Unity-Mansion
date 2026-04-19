using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oddworm.Framework;
using Pathfinding;
using Pathfinding.Drawing;
using UnityEngine;

public class CharacterAgent : MonoBehaviour
{
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
//			Debug.Log(this + " SetObstacle " + value);
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
			NavUtility.RebuildNavigationAsync();
		}
		else Debug.LogWarning(this + " SetObstacle not possible: no GraphUpdateScene component");
	}

	public Vector3[] GetCurrentPath()
	{
		if (!hasDestination) // GetCurrentPath
			return new Vector3[0];
		
		var buffer = new List<Vector3>();
		aiPath.GetRemainingPath(buffer, out var stale);
		return buffer.ToArray();
	}

	public void StartMove()
	{
		aiPath.updatePosition = aiPath.updateRotation = true;
		aiPath.simulateMovement = true;
		aiPath.isStopped = false;
		UpdatePathAndTarget(true);
		//Debug.Log(this + " StartMove");
	}

	public void StopMovement()
	{
		aiPath.isStopped = true;
		aiPath.updatePosition = aiPath.updateRotation = false;
		aiPath.simulateMovement = false;
		//Debug.Log(this + " StopMovement");
		ClearDestination();
	}
	
	public void AssignDestination(Vector3 hitPoint, bool force = false)
	{
		/*if (hasDestination // AssignDestination
		    && !Utils.SnapCompareVector3(hitPoint, aiPath.destination, 0.1f))
		{
			hasDestination = false;
			Debug.Log(aiPath + " SnapCompareVector3 hasDestination=" + hasDestination);
		}*/
		
		if (force || !hasDestination)
		{
			hasDestination = true;
			//lastAssignTime = Time.time;
			//Debug.Log(aiPath + " hasDestination=" + hasDestination);
			
			aiPath.destination = hitPoint;
			aiPath.SearchPath();
			
			UpdatePathAndTarget(true);
		}
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
		return aiPath.reachedDestination || Vector3.Distance(aiPath.position, dest) <= GetMinReachDistance();
	}

	private const float TIMED_OUT = 3f;
	public IEnumerator SearchPathAsync(Vector3 dest, System.Action onComplete = null)
	{
		AssignDestination(dest, true); // WaitCharacterMoved
		
		var timeout = 0.0f;
		do
		{
			UpdatePathAndTarget(true);
			if (timeout >= TIMED_OUT) break;
			yield return null;
			timeout += Time.deltaTime;
		}
		while (pathPending);

		UpdatePathAndTarget(true);
		
		if (timeout >= TIMED_OUT)
		{
			Debug.LogWarning(this + " pathPending timeout!");
			yield break;
		}
		
		onComplete?.Invoke();
		//var path = GetCurrentPath();
	}
	
	public bool hasPath => aiPath.hasPath;
	
	public Vector3 velocity => aiPath.velocity;

	public bool IsMoving() => !aiPath.isStopped /*&& !aiPath.pathPending && velocity.magnitude > Consts.MinMagnitude)*/;
	   
	public float GetMinReachDistance() => Mathf.Max(endReachDistance, aiPath.endReachedDistance);
	   
	public bool pathPending => aiPath.pathPending;

	public float rotationSpeed => aiPath.rotationSpeed;

	///private float lastAssignTime;
	private bool hasDestination;
	private AIPath aiPath;
	private CharacterController controller;
	private GameController gameController; // TODO
	private CharacterAnimation _characterAnimation;
	private Vector3[] lastPath;

	private void OnDrawGizmosSelected()
	{
		UpdateComponent();
		Gizmos.color = Color.cyan;
		Utils.DrawGizmosCircle(transform.position, Vector3.up, GetMinReachDistance());
	}

	void UpdateComponent()
	{
		aiPath = Utils.GetOrAddComponent<AIPath>(gameObject);
		aiPath.radius = radius;
		aiPath.height = height;
		aiPath.maxSpeed = moveSpeed;
		if(endReachDistance > 0f) aiPath.endReachedDistance = endReachDistance;
	}

	private void Awake()
	{
		_characterAnimation = GetComponent<CharacterAnimation>();
		controller = _characterAnimation.GetController();
		controller.onUseAction += OnUseAction;
		
		gameController = GameController.GetInstance();
		gameController.turnEnded += UpdatePathAndTarget;
		gameController.turnStarted += UpdatePathAndTarget;
		CharacterSelection.selectionChanged += SelectionChanged;
		
		UpdateComponent();
	}
	
	private void OnDestroy()
	{
		controller.onUseAction -= OnUseAction;
		gameController.turnEnded -= UpdatePathAndTarget;
		gameController.turnStarted -= UpdatePathAndTarget;
		CharacterSelection.selectionChanged -= SelectionChanged;
	}

	private void Start()
	{
		if (!hasDestination) aiPath.isStopped = true; // Start
		
		UpdatePathAndTarget(true);
	}
	

	void OnUseAction(CharacterAction action, object param)
	{
		StopMovement();
		UpdatePathAndTarget(true);
	}

	void SelectionChanged()
	{
		if (controller.IsSelected())
		{
			SetObstacle(!controller.IsControllableByPlayer());
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
		if (GameView.GetInstance().HasCoroutines()) // todo
			UpdatePathAndTarget(true);
	}

	private bool[] shouldDrawPath = new []{false,false};
	private bool[] isDrawingPath = new []{false,false};
	void UpdatePathAndTarget() => UpdatePathAndTarget(true);
	private void UpdatePathAndTarget(bool force)
	{
		//TODO: controller.IsActing(), controller.IsMoving(), controller.IsRotating()

		shouldDrawPath[0] = hasDestination // "thin" path
		                    && CharacterSelection.IsCharacterSelected(controller)
		                    && controller.IsControllableByPlayer()
		                    && (!GameView.GetInstance().HasCoroutines() || controller.IsMoving())
		                    && (lastPath = GetCurrentPath()).Length > 1;

		shouldDrawPath[1] = hasDestination  // "thin" path
		                    && CharacterSelection.IsCharacterSelected(controller) 
		                    && controller.IsControllableByPlayer() 
		                    && !GameView.GetInstance().HasCoroutines()
		                    && (lastPath = GetCurrentPath()).Length > 1;
								//&& !controller.IsRotating()
								//&& !controller.IsMoving();

		if (force || shouldDrawPath[0] != isDrawingPath[0])
		{
			pathRenderers[0].enabled = isDrawingPath[0] = shouldDrawPath[0];
			if (pathRenderers[0].enabled) // draw path thin
			{
				pathRenderers[0].startColor = pathRenderers[0].endColor = GameSettings.GetDisabledColor(controller.GetCurrentFaction());
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
				lastPath = Utils.LimitPathTotalLength(lastPath, controller.GetRemainingMovePoints());
				pathRenderers[1].startColor = pathRenderers[1].endColor = GameSettings.GetCharacterColor(controller, true, true);
				pathRenderers[1].positionCount = lastPath.Length;
				pathRenderers[1].useWorldSpace = true;
				pathRenderers[1].SetPositions(lastPath);
			}
		}
		
		UpdateMoveTarget(controller.IsMoving());
	}

	void UpdateMoveTarget(bool isBlinking)
	{
		if (!hasDestination || !aiPath.hasPath) //hide moveTarget !hasDestination && !aiPath.hasPath
		{
			moveTarget.gameObject.SetActive(false);
			return;
		}

		//Debug.Log(this + " show moveTarget");
		moveTarget.gameObject.SetActive(true);
		moveTarget.transform.position = aiPath.destination;
		moveTarget.SetColor(GameSettings.GetCharacterColor(controller, true, true));
		TransformAnimations.BounceRealtime(moveTarget.transform, isBlinking);
	}
}