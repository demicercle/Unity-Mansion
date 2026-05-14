using System.Linq;
using Pathfinding;
using UnityEngine;


public class HeightPathModifier : MonoModifier 
{
	public override int Order => 10;

	public float drawDuration => GameTurnManager.Instance.IsWaiting() ? 1.0f : 0.02f;
	
	public AIPath aiPath;
	public float heightTolerance = 0.5f;
	public bool losCheck = true;
	public int debugMode => CharacterAgent.debugMode;

	private Vector3 _lastAIPosition, _lastValidPosition;
	
	public override void Apply(Path path)
	{
		if (path.CompleteState == PathCompleteState.Complete)
		{
			var endPoint = path.vectorPath.Last();

			if (_lastAIPosition.ToString() == aiPath.position.ToString() &&
			    endPoint.ToString() == _lastValidPosition.ToString())
			{
				return;
			}
			
			var heightOffset = Mathf.Abs(endPoint.y - aiPath.destination.y);
			if (heightOffset > heightTolerance)
			{
				if(debugMode > 0) Debug.Log(this + " <color=orange>Apply heightTolerance Error: " + heightOffset + " </color>");
				if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.orange, drawDuration);
				if(debugMode > 0) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.orange, drawDuration);
				path.Error();
				return;
			}

			var dist = Vector3.Distance(endPoint, aiPath.destination);
			if (dist > aiPath.endReachedDistance)
			{
				if(debugMode > 0) Debug.Log(this + " <color=red>Apply endReachedDistance Error: " + dist + " / " + aiPath.endReachedDistance + " </color>");
				if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.red, drawDuration);
				if(debugMode > 0) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.red, drawDuration);
				path.Error();
				return;
			}

			if (losCheck)
			{
				var points = path.vectorPath.ToList();
				points.Reverse();
				while (points.Count > 1)
				{
					var p0 = points[0];
					p0 += Vector3.up * aiPath.height / 2f;
					
					var p1 = points[1];
					p1 += Vector3.up * aiPath.height / 2f;

					if(debugMode > 0) DebugDrawer.Line(p0, p1, Color.yellow, drawDuration);
					
					if (Physics.Linecast(p0, p1, out var hit, Layers.MaskPathObstruction))
					{
						if(debugMode > 0) Debug.Log(this + " <color=yellow>Apply losCheck Linecast Error: " + hit.collider);
						if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.yellow, drawDuration);
						if(debugMode > 0) DebugDrawer.Sphere(hit.point, .2f, Color.yellow, drawDuration);
						path.Error();
						return;
					}
					
					points.RemoveAt(0);
				}
			}

			_lastAIPosition = aiPath.position;
			_lastValidPosition = endPoint;
			
			if(debugMode > 0) DebugDrawer.Path(path.vectorPath.ToArray(), false, Color.green, drawDuration);
			if(debugMode > 0) DebugDrawer.WireDisc(aiPath.destination, aiPath.endReachedDistance, Color.green, drawDuration);
		}
	}
}