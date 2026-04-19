using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "AIBrain_Chase", menuName = "AIBrain/Chase", order = 0)]
public class AIBrain_Chase : AIBrainData
{
	public bool noRun;
	public bool noLOS;
	public Color hearingColor = Color.magenta;
	public float hearingDistance = 1.0f;
	
	private CharacterController lastTarget;

	public override int GetPriority(CharacterController character)
	{
		UpdateTarget(character);
		
		return lastTarget == null ? -1 : defaultPriority;
	}

	public void UpdateTarget(CharacterController character)
	{
		if (lastTarget != null)
		{
			if (lastTarget.IsDead())// 
			{
				Debug.Log(this + " forget dead target");
				lastTarget = null; 
			}
		}
		
		if (lastTarget == null)
		{
			Debug.Log(this + " finds target");
			var closest = character.GetClosestCharacter(character.GetNearbyLivingCharacters(hearingDistance, 1.0f, hearingColor));
			if (closest != null)
			{
				Debug.Log(this + " detects: " + closest);
				lastTarget = closest;
				//DebugGUI.Line(character.GetEyesPosition(), lastTarget.GetCenterPosition(), hearingColor, 1.0f);
			}
		}
	}
	
	public override void DrawGizmos(CharacterController character)
	{
		Gizmos.color = hearingColor;
		Utils.DrawGizmosCircle(character.GetEyesPosition(), default, hearingDistance);
		if(Application.isPlaying && lastTarget != null) DebugDrawer.Line(character.GetEyesPosition(), character.GetCenterPosition(), hearingColor, 0.0f);
	}

	public override IEnumerator Think(CharacterController character)
	{
		UpdateTarget(character);
		yield return new WaitForSeconds(1.0f);

		if (lastTarget != null)
		{
			while (character.CanAttack())
			{
				DebugDrawer.Line(character.GetEyesPosition(), lastTarget.GetCenterPosition(), Color.red, 1.0f);
				gameView.AssignAttack(character, lastTarget, !noRun, noLOS);
				
				while (gameView.HasCoroutines())
				{
					yield return null;
				}
				
				yield return new WaitForSeconds(1f);
			}
		}
	}
}