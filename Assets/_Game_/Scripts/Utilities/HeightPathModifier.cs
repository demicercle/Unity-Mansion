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
		var endPoint = path.vectorPath.Last();
		if (Mathf.Abs(endPoint.y - aiPath.destination.y) > heightTolerance)
		{
			path.Error();
		}
	}
}