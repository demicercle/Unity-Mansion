using System;
using System.Collections.Generic;
using Oddworm.Framework;
using Pathfinding;
using UnityEngine;

public class CharacterAgent : MonoBehaviour
{
	private void Awake()
	{
		aiPath = Utils.GetOrAddComponent<AIPath>(gameObject);
	}

	public AIPath aiPath;
	
	public Vector3 velocity => aiPath.velocity;
	
	public bool isMoving => !aiPath.isStopped;
	    
	public bool reachedDestination => aiPath.reachedDestination;
	   
	public float reachDistance => aiPath.endReachedDistance;
	   
	public bool pathPending => aiPath.pathPending;

	    
	private bool hasDestination;

	    public System.Action onSearchPath
	    {
		    get => aiPath.onSearchPath;
		    set => aiPath.onSearchPath = value;
	    }

	    public void Teleport(Vector3 newPos, int newRot)
	    {
		    aiPath.Teleport(newPos, true);
		    aiPath.rotation = Quaternion.Euler(0, newRot, 0);
	    }
		
	    public void SetCanMove(bool value)
	    {
		    aiPath.simulateMovement = value;
		    aiPath.isStopped = !value;
	    }

	    public void SetObstacle(bool value)
	    {
		    if (aiPath.TryGetComponent(out GraphUpdateScene graphUpdate))
		    {
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
		    }
	    }

	    public Vector3[] GetCurrentPath()
	    {
		    if (!hasDestination)
			    return new Vector3[0];
			
		    var buffer = new List<Vector3>();
		    aiPath.GetRemainingPath(buffer, out var stale);
		    return buffer.ToArray();
	    }
		
	    public void AssignDestination(Vector3 hitPoint)
	    {
		    //Debug.Log(aiPath + " AssignDestination " + hitPoint);
		    aiPath.destination = hitPoint;
		    aiPath.SearchPath();
		    hasDestination = true;
		    onSearchPath?.Invoke();
		    DbgDraw.Disc(hitPoint, Quaternion.identity, aiPath.radius, Utils.ColorAlpha(Color.cyan, 0.2f), 0f);
	    }

	    public void ClearDestination()
	    {
		    if (hasDestination)
		    {
//				Debug.Log(aiPath + " ClearDestination()");
			    hasDestination = false;
			    aiPath.destination = aiPath.transform.position;
			    onSearchPath?.Invoke();
		    }
	    }

	    public void StopMovement()
	    {
		    aiPath.isStopped = true;
		    ClearDestination();
	    }
}