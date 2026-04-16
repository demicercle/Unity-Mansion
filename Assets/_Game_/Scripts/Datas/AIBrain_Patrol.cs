using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain_Patrol", menuName = "AIBrain/Patrol", order = 0)]
public class AIBrain_Patrol : AIBrainData
{
	public override IEnumerator Think(CharacterView character)
	{
		if (PatrolRoute.FindRoute(character, out PatrolRoute patrolRoute))
		{
			patrolRoute.DebugDraw(1f);
			if (lastWaypointToReach == null)
			{
				if (!Waypoint.GetBestWaypointFromPosition(character.GetPosition(), out var bestWaypoint,
					    patrolRoute.waypoints))
				{
					Debug.LogWarning(character + " cannot find next waypoint");
				}
				else
				{
					gameView.MoveCharacterToWaypoint(character, bestWaypoint);
					while(gameView.HasCoroutines()) yield return null;
				}
			}
		}
		else
		{
			Debug.LogWarning(character + " has no patrol route");
		}
		
		yield return new WaitForSeconds(1.0f);
	}

	private Waypoint lastWaypointToReach;
}