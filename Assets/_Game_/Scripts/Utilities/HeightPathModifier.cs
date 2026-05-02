using System.Linq;
using Pathfinding;
using UnityEngine;


public class HeightPathModifier : MonoModifier 
{
	public override int Order => 10;

	public AIPath aiPath;
	public float heightTolerance = 0.5f;
	public int debugMode = 1;
	
	public override void Apply(Path path)
	{
		if (path.CompleteState == PathCompleteState.Complete)
		{
			var endPoint = path.vectorPath.Last();
			
			var heightOffset = Mathf.Abs(endPoint.y - aiPath.destination.y);
			if (heightOffset > heightTolerance)
			{
				if(debugMode > 0) Debug.Log(this + " Apply heightTolerance Error: " + heightOffset);
				if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.orange, 1.0f);
				if(debugMode > 0) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.orange, 1.0f);
				path.Error();
				return;
			}

			var dist = Vector3.Distance(endPoint, aiPath.destination);
			if (dist > aiPath.endReachedDistance)
			{
				if(debugMode > 0) Debug.Log(this + " Apply endReachedDistance Error: " + dist + " / " + aiPath.endReachedDistance);
				if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.red, 1.0f);
				if(debugMode > 0) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.red, 1.0f);
				path.Error();
				return;
			}
			
			if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.green, 1.0f);
			if(debugMode > 0) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.green, 1.0f);
		}
	}
}