using System;
using System.Collections;
using System.Collections.Generic;
using Oddworm.Framework;
using Pathfinding;
using UnityEngine;

public class CharacterAgent : MonoBehaviour
{
	[SerializeField] [Range(0.0f, 10.0f)] private float moveSpeed = 4.5f;
	[SerializeField] [Range(0.1f, 2.0f)] private float radius = 0.5f;
	[SerializeField] [Range(0.1f, 3.0f)] private float height = 1.5f;
	[SerializeField] [Min(0f)] private float endReachDistance = 0.0f;
	[SerializeField] private List<LineRenderer> pathRenderers;

	public System.Action onSearchPath
	{
		get => aiPath.onSearchPath;
		set => aiPath.onSearchPath = value;
	}
	
	public bool PositionChanged(Vector3 p1, Vector3 p2) => p1.ToString() != p2.ToString();
	
	public float GetMaxSpeed() => moveSpeed;

	public float GetRadius() => radius;

	public float GetHeight() => height;

	public void Teleport(Vector3 newPos, int newRot)
	{
		aiPath.Teleport(newPos, true);
		aiPath.rotation = Quaternion.Euler(0, newRot, 0);
	}

	public void SetObstacle(bool value)
	{
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
		if (!hasDestination)
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
		Debug.Log(this + " StartMove");
	}

	public void StopMovement()
	{
		aiPath.isStopped = true;
		aiPath.updatePosition = aiPath.updateRotation = false;
		aiPath.simulateMovement = false;
		Debug.Log(this + " StopMovement");
		ClearDestination();
	}
	
	public void AssignDestination(Vector3 hitPoint)
	{
		if (hasDestination && !PositionChanged(hitPoint, aiPath.destination))
			return;
	    
		//Debug.Log(aiPath + " AssignDestination " + hitPoint);
		aiPath.destination = hitPoint;
		aiPath.SearchPath();
		hasDestination = true;
		onSearchPath?.Invoke();
		DbgDraw.Disc(hitPoint, Quaternion.identity, aiPath.radius, Utils.ColorAlpha(Color.cyan, 0.2f), 0f);
	}

	public void ClearDestination()
	{
		hasDestination = false;
	}

	public bool ReachedDestination(Vector3 dest)
	{
		return aiPath.reachedDestination || Vector3.Distance(aiPath.position, dest) <= GetMinReachDistance();
	}

	private const float TIMED_OUT = 3f;
	public IEnumerator SearchPathAsync(Vector3 dest, System.Action onComplete = null)
	{
		AssignDestination(dest); // WaitCharacterMoved
		
		var timeout = 0.0f;
		do
		{
			if (timeout >= TIMED_OUT) break;
			yield return null;
			timeout += Time.deltaTime;
		}
		while (pathPending);

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
	
	public bool isMoving => !aiPath.isStopped;
	    
	//public bool reachedDestination => aiPath.reachedDestination;
	   
	public float GetMinReachDistance() => Mathf.Max(endReachDistance, aiPath.endReachedDistance);
	   
	public bool pathPending => aiPath.pathPending;

	public float rotationSpeed => aiPath.rotationSpeed;

	private bool shouldDrawPath;
	private bool hasDestination;
	private AIPath aiPath;
	private CharacterController controller;
	private CharacterView characterView;
	private Vector3[] lastPath;

	private void OnDrawGizmosSelected()
	{
		UpdateComponent();
		Gizmos.color = Color.cyan;
		Utils.DrawGizmosCircle(transform.position, Vector3.up, GetMinReachDistance());
	}

	private void Awake()
	{
		characterView = GetComponent<CharacterView>();
		controller = characterView.GetController();
		controller.selectionChanged += SelectionChanged;
		UpdateComponent();
	}

	private void Start()
	{
		if(!hasDestination)
			StopMovement();
	}
	
	private void OnDestroy()
	{
		controller.selectionChanged -= SelectionChanged;
	}

	private void LateUpdate()
	{
		DrawPath();
	}

	private void UpdateComponent()
	{
		aiPath = Utils.GetOrAddComponent<AIPath>(gameObject);
		aiPath.radius = radius;
		aiPath.height = height;
		aiPath.maxSpeed = moveSpeed;
		if(endReachDistance > 0f) aiPath.endReachedDistance = endReachDistance;
	}

	private void SelectionChanged(bool isSelected)
	{
		shouldDrawPath = isSelected && controller.IsControllable();
	}
	
	private void DrawPath()
	{
		if (!shouldDrawPath || (lastPath = GetCurrentPath()).Length <= 1)
		{
			pathRenderers.ForEach(p => p.enabled = false);
			return;
		}

		var pathColor = characterView.GetCurrentColor();
		
		if (pathRenderers.Count > 0)
		{
			pathRenderers[0].startColor = pathRenderers[0].endColor = pathColor;
			pathRenderers[0].positionCount = lastPath.Length;
			pathRenderers[0].useWorldSpace = true;
			pathRenderers[0].SetPositions(lastPath);
		}

		if (pathRenderers.Count > 1)
		{
			lastPath = Utils.LimitPathTotalLength(lastPath, controller.GetRemainingMovePoints());
			pathRenderers[1].startColor = pathRenderers[1].endColor = pathColor;
			pathRenderers[1].positionCount = lastPath.Length;
			pathRenderers[1].useWorldSpace = true;
			pathRenderers[1].SetPositions(lastPath);
		}

		pathRenderers.ForEach(p => p.enabled = true);
	}
}