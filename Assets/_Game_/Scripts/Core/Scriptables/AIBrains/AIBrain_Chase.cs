using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "AIBrain_Chase", menuName = "AIBrain/Chase", order = 0)]
public class AIBrain_Chase : AIBrainBase
{
	[SerializeField] private bool noRun;
	[SerializeField] private bool noLOS;
	[SerializeField] private bool cantBarks;
	[SerializeField] private Color hearingColor = Color.magenta;
	[SerializeField] private float hearingDistance = 1.0f;
	[SerializeField] private float defaultWait = 1.0f;
	[SerializeField] private float defaultTicks = 0.5f;
	
	private CharacterController lastTarget;
	private CharacterController lastThinker;
	private DetectionTrigger detectionTrigger;

	public override int GetPriority(CharacterController character)
	{
		if (lastTarget != null)
		{
			if (detectionTrigger && !detectionTrigger.IsCharacterDetected(lastTarget))
			{
				if (!detectionTrigger.TryGetLastKnownPosition(lastTarget, out var _))
					lastTarget = null;
			}
		}
		
		if (lastTarget == null)
		{
			if (!detectionTrigger)
			{
				InstantSearchTarget(character);
			}
			else
			{
				if (detectionTrigger.TryGetClosestDetectedCharacter(character.GetPosition(), out var otherCharacter))
				{
					lastTarget = otherCharacter;
				}
			}
		}
		
		return !lastTarget ? -1 : defaultPriority;
	}

	public override IEnumerator Think(CharacterController character)
	{
		if (lastThinker != lastTarget) detectionTrigger = null; // for some reason, it's a new character thinking
		
		detectionTrigger = character.GetComponent<DetectionTrigger>();
		if(!detectionTrigger) detectionTrigger = character.GetComponentInChildren<DetectionTrigger>();
		if(!detectionTrigger) Debug.LogWarning(this + " has no detection trigger, will do instant search");
		
		lastThinker = character;

		yield return character.StartCoroutine(DoTargetUpdate(character));

		if (lastTarget != null)
		{
			if (!character.CanAttack())
			{
				Debug.Log(this + " cannot attack !");
				if (cantBarks) character.FloatingText_Cant();
				yield return new WaitForSeconds(defaultWait);
			}
			
			while (character.CanAttack())
			{
				Debug.Log(this + " chase " + lastTarget);
				
				DebugDrawer.Line(character.GetEyesPosition(), lastTarget.GetCenterPosition(), Color.red, 1.0f);
				character.Routines.AssignAttack(lastTarget, !noRun, noLOS);

				do
				{
					yield return new WaitForSeconds(defaultTicks);
					
				} while (CharacterRoutines.AnyActiveRoutines());
			}
		}
		else
		{
			Debug.Log(character + " did not find any target");
			yield return new WaitForSeconds(defaultWait);
		}
	}
	
	public override void DrawGizmos(CharacterController character)
	{
		Gizmos.color = hearingColor;
		Utils.DrawGizmosCircle(character.GetEyesPosition(), default, hearingDistance);
		if(Application.isPlaying && lastTarget != null) DebugDrawer.Line(character.GetEyesPosition(), character.GetCenterPosition(), hearingColor, 0.0f);
	}
	
	
	

	private IEnumerator DoTargetUpdate(CharacterController character)
	{
		if (lastTarget != null)
		{
			if (lastTarget.IsDead())// 
			{
				Debug.Log(this + " forget dead target");
				lastTarget = null; 
			}

			if (detectionTrigger)
			{
				if (!detectionTrigger.IsCharacterDetected(lastTarget))
				{
					if (detectionTrigger.TryGetLastKnownPosition(lastTarget, out var lastKnownPosition))
					{
						character.Routines.MoveToDestination(lastKnownPosition);
						
						do
						{
							yield return null;
						} while (!detectionTrigger.IsCharacterDetected(lastTarget));

						if (detectionTrigger.IsCharacterDetected(lastTarget))
						{
							Debug.Log(this + " detects AGAIN " + lastTarget);
							// TODO something?
						}
						else
						{
							// TODO look around
							lastTarget = null; // forget target
						}
					}
				}
			}
		}

		if (!lastTarget)
		{
			if (!detectionTrigger)
			{
				InstantSearchTarget(character);
			}
			else
			{
				if (detectionTrigger.TryGetClosestDetectedCharacter(character.GetPosition(), out var other))
				{
					lastTarget = other;
				}
				
				Debug.Log(this + " finds target: " + lastTarget);
			}
		}
	}

	private void InstantSearchTarget(CharacterController character)
	{
		var closest = character.GetClosestCharacter(character.GetNearbyLivingCharacters(hearingDistance, 1.0f, hearingColor));
		if (closest != null)
		{
			lastTarget = closest;
		}
				
		Debug.Log(this + " finds target (without detection trigger): " + lastTarget);
	}
}