using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain_Patrol", menuName = "AIBrain/Patrol", order = 0)]
public class AIBrain_Patrol : AIBrainData
{
	public bool canUseRunAction;
	
	public override IEnumerator Think(CharacterView character)
	{
		yield return new WaitForSeconds(1.0f);
		
		if (PatrolRoute.FindRoute(character, out PatrolRoute patrolRoute))
		{
			var waypoints = patrolRoute.waypoints;

			while (character.CanMove())
			{
				patrolRoute.DebugDraw(1f);
				
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
					}
				}

				if (lastWaypointToReach != null)
				{
					if (!character.WaypointReached(lastWaypointToReach)) // move to patrol waypoint
					{
						gameView.MoveCharacterToWaypoint(character, lastWaypointToReach); // patrol move
						while(gameView.HasCoroutines()) yield return null;

						if (!character.CanMove() && character.CanUseRunAction() && canUseRunAction)
						{
							gameView.RunAction(character);
							while(gameView.HasCoroutines()) yield return null;
						}
					}
				
					if (character.WaypointReached(lastWaypointToReach)) // pick next waypoint in patrol
					{
						var wpIndex = waypoints.IndexOf(lastWaypointToReach);
					
						if (patrolRoute.closedLoop) 
							wpIndex = (wpIndex + 1) % waypoints.Count;
						else
							wpIndex += 1;

						if (wpIndex >= waypoints.Count)
						{
							Debug.Log(character + " reached last waypoint " + wpIndex);
							yield break;
						}
						else
						{
							lastWaypointToReach = waypoints[wpIndex];
							DebugGUI.Line(lastWaypointToReach.GetPosition(), character.GetPosition(), Color.blue, 1.0f);
							Debug.Log(character + " picked next waypoint " + wpIndex + " " + lastWaypointToReach);
						}
					}
				}
			}
		}
		else
		{
			Debug.LogWarning(character + " has no patrol route");
		}
	}

	private Waypoint lastWaypointToReach;
}