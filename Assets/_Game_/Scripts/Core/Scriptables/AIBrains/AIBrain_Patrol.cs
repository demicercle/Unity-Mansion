using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain_Patrol", menuName = "AIBrain/Patrol", order = 0)]
public class AIBrain_Patrol : AIBrainBase
{
	[SerializeField] private float waypointWaitSeconds = 2.0f;
	[SerializeField] private bool canUseRunAction;
	
	public override IEnumerator Think(CharacterController selfCharacter)
	{
		if (!PatrolRoute.FindRoute(selfCharacter, out PatrolRoute patrolRoute))
		{
			Debug.LogWarning(selfCharacter + " has no patrol route");
			yield break;
		}
		
		var waypoints = patrolRoute.waypoints;
		while (selfCharacter.HasActions() && selfCharacter.HasMovePoints())
		{
			yield return null;
			patrolRoute.DebugDraw(0);
				
			if (lastWaypointToReach == null)
			{
				if (!Waypoint.GetBestWaypointFromPosition(selfCharacter.GetPosition(), out var bestWaypoint,
					    waypoints))
				{
					Debug.LogWarning(selfCharacter + " cannot find next waypoint");
					yield break;
				}
				else
				{
					lastWaypointToReach = bestWaypoint;
					Debug.Log(selfCharacter + " picked bestWaypoint " + lastWaypointToReach);
				}
			}

			if (lastWaypointToReach != null)
			{
				if (!selfCharacter.WaypointReached(lastWaypointToReach))
				{
					Debug.Log(this + " !WaypointReached, patrols towards lastWaypointToReach " + lastWaypointToReach);
					selfCharacter.Routines.AssignMoveToWaypoint(lastWaypointToReach, canUseRunAction);
				}
				
				else
				{
					var wpIndex = waypoints.IndexOf(lastWaypointToReach);
					
					if (patrolRoute.closedLoop) 
						wpIndex = (wpIndex + 1) % waypoints.Count;
					else
						wpIndex += 1;
					
//					Debug.Log(this + " picked next wpIndex " + wpIndex);

					if (wpIndex >= waypoints.Count)
					{
						Debug.Log(selfCharacter + " reached last waypoint " + wpIndex + ", use all actions");
						selfCharacter.UseAllActions();
					}
					else
					{
						lastWaypointToReach = waypoints[wpIndex];
						Debug.Log(selfCharacter + " picked next waypoint " + wpIndex + " " + lastWaypointToReach);
						DebugDrawer.Line(lastWaypointToReach.GetPosition(), selfCharacter.GetPosition(), patrolRoute.color, 1.0f);
					}
					
					yield return new WaitForSeconds(waypointWaitSeconds);
				}
			}

			while (CharacterRoutines.AnyActiveRoutines())
			{
				if (lastWaypointToReach != null)
				{
					lastWaypointToReach.DrawDebug();
				}
				
				yield return null;
			}
		}
	}

	private Waypoint lastWaypointToReach;
}