using System.Linq;
using Pathfinding;
using UnityEngine;


public class HeightPathModifier : MonoModifier 
{
	public override int Order => 10;

	public AIPath aiPath;
	public float heightTolerance = 0.5f;
	
	public override void Apply(Path path)
	{
		if (path.CompleteState == PathCompleteState.Complete)
		{
			var endPoint = path.vectorPath.Last();
			var heightOffset = Mathf.Abs(endPoint.y - aiPath.destination.y);
			if (heightOffset > heightTolerance)
			{
				Debug.Log(this + " Apply Error: " + heightOffset);
				path.Error();
			}
		}
	}
}