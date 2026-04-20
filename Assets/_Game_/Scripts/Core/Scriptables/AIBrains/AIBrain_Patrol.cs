using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain_Patrol", menuName = "AIBrain/Patrol", order = 0)]
public class AIBrain_Patrol : AIBrainBase
{
	public bool canUseRunAction;
	
	public override IEnumerator Think(CharacterController character)
	{
		yield return new WaitForSeconds(1.0f);
		
		if (!PatrolRoute.FindRoute(character, out PatrolRoute patrolRoute))
		{
			Debug.LogWarning(character + " has no patrol route");
			yield break;
		}
		
		var waypoints = patrolRoute.waypoints;
		while (character.HasActions() && character.HasMovePoints())
		{
			yield return null;
			patrolRoute.DebugDraw(0);
				
			if (lastWaypointToReach == null)
			{
				if (!Waypoint.GetBestWaypointFromPosition(character.GetPosition(), out var bestWaypoint,
					    waypoints))
				{
					Debug.LogWarning(character + " cannot find next waypoint");
					yield break;
				}
				else
				{
					lastWaypointToReach = bestWaypoint;
					Debug.Log(character + " picked bestWaypoint " + lastWaypointToReach);
				}
			}

			if (lastWaypointToReach != null)
			{
				if (!character.WaypointReached(lastWaypointToReach))
				{
					Debug.Log(this + " !WaypointReached, patrols towards lastWaypointToReach " + lastWaypointToReach);
					character.Routines.AssignMoveToWaypoint(lastWaypointToReach, canUseRunAction);
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
						Debug.Log(character + " reached last waypoint " + wpIndex + ", use all actions");
						character.UseAllActions();
						yield return new WaitForSeconds(Consts.AIThinksSeconds);
						yield break;
					}
					else
					{
						lastWaypointToReach = waypoints[wpIndex];
						Debug.Log(character + " picked next waypoint " + wpIndex + " " + lastWaypointToReach);
						DebugDrawer.Line(lastWaypointToReach.GetPosition(), character.GetPosition(), patrolRoute.color, 1.0f);
					}
					
					yield return new WaitForSeconds(Consts.AIThinksSeconds); // TODO Patrol point "wait time"
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