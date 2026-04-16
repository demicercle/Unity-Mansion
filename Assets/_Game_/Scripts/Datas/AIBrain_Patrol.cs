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
		}
		else
		{
			Debug.LogWarning(character + " has no patrol route");
		}
		
		yield return new WaitForSeconds(1.0f);
	}
}