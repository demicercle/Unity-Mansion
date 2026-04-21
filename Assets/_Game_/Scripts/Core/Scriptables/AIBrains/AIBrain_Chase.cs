using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "AIBrain_Chase", menuName = "AIBrain/Chase", order = 0)]
public class AIBrain_Chase : AIBrainBase
{
	[SerializeField] private bool noRun;
	[SerializeField] private bool noLOS;
	[SerializeField] private Color hearingColor = Color.magenta;
	[SerializeField] private float hearingDistance = 1.0f;
	
	private CharacterController lastTarget;

	public override int GetPriority(CharacterController character)
	{
		UpdateTarget(character);
		
		return lastTarget == null ? -1 : defaultPriority;
	}

	public override IEnumerator Think(CharacterController character)
	{
		UpdateTarget(character);

		if (lastTarget != null)
		{
			while (character.CanAttack())
			{
				DebugDrawer.Line(character.GetEyesPosition(), lastTarget.GetCenterPosition(), Color.red, 1.0f);
				character.Routines.AssignAttack(lastTarget, !noRun, noLOS);
				
				while (CharacterRoutines.AnyActiveRoutines())
				{
					yield return null;
				}
				
				yield return new WaitForSeconds(1f);
			}
		}
	}
	
	public override void DrawGizmos(CharacterController character)
	{
		Gizmos.color = hearingColor;
		Utils.DrawGizmosCircle(character.GetEyesPosition(), default, hearingDistance);
		if(Application.isPlaying && lastTarget != null) DebugDrawer.Line(character.GetEyesPosition(), character.GetCenterPosition(), hearingColor, 0.0f);
	}

	private void UpdateTarget(CharacterController character)
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
}