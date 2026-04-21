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

	public override int GetPriority(CharacterController selfCharacter)
	{
		SetupThinker(selfCharacter);
		
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
				GetClosestTarget(selfCharacter);
			}
			else
			{
				if (detectionTrigger.TryGetClosestDetectedCharacter(selfCharacter.GetPosition(), out var otherCharacter))
				{
					lastTarget = otherCharacter;
				}
			}
		}
		
		return !lastTarget ? -1 : defaultPriority;
	}

	public override IEnumerator Think(CharacterController selfCharacter)
	{
		SetupThinker(selfCharacter);

		if (!lastTarget && !detectionTrigger)
		{
			GetClosestTarget(selfCharacter);
		}
		
		if (lastTarget != null)
		{
			if (!selfCharacter.CanAttack())
			{
				Debug.Log(this + " cannot attack !");
				if (cantBarks) selfCharacter.FloatingText_Cant();
				yield return new WaitForSeconds(defaultWait);
			}
			
			while (selfCharacter.CanAttack())
			{
				Debug.Log(this + " chase " + lastTarget);
				
				DebugDrawer.Line(selfCharacter.GetEyesPosition(), lastTarget.GetCenterPosition(), Color.red, 1.0f);
				selfCharacter.Routines.AssignAttack(lastTarget, !noRun, noLOS);

				do
				{
					yield return new WaitForSeconds(defaultTicks);
					
				} while (CharacterRoutines.AnyActiveRoutines());
			}
		}
		else if (detectionTrigger)
		{
			if (!detectionTrigger.IsCharacterDetected(lastTarget))
			{
				if (detectionTrigger.TryGetLastKnownPosition(lastTarget, out var lastKnownPosition))
				{
					selfCharacter.Routines.MoveToDestination(lastKnownPosition);
						
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
	
	public override void DrawGizmos(CharacterController selfCharacter)
	{
		Gizmos.color = hearingColor;
		Utils.DrawGizmosCircle(selfCharacter.GetEyesPosition(), default, hearingDistance);
		if(Application.isPlaying && lastTarget != null) DebugDrawer.Line(selfCharacter.GetEyesPosition(), selfCharacter.GetCenterPosition(), hearingColor, 0.0f);
	}

	private void SetupThinker(CharacterController selfCharacter)
	{
		if (lastThinker && selfCharacter == lastThinker)
		{
			Debug.Log(this + " Setup " + selfCharacter + "==" + lastThinker);
			return;
		}
		
		if (lastThinker != null)
		{
			if (detectionTrigger)
			{
				detectionTrigger.onCharacterDetected -= OnCharacterDetected;
				detectionTrigger.onCharacterLost -= OnCharacterLost;
				detectionTrigger = null;
			}
		}


		Debug.Log(this + " setup new " + lastThinker + " > " + selfCharacter);
		lastThinker = selfCharacter;
		
		if (lastThinker != null)
		{
			detectionTrigger = lastThinker.GetComponent<DetectionTrigger>();
			if(!detectionTrigger) detectionTrigger = lastThinker.GetComponentInChildren<DetectionTrigger>();
			if(!detectionTrigger) Debug.LogWarning(this + " has no detection trigger, will do GetClosestTarget");
			else
			{
				detectionTrigger.onCharacterDetected += OnCharacterDetected;
				detectionTrigger.onCharacterLost += OnCharacterLost;
			}
		}
	}

	private void OnCharacterDetected(CharacterController otherCharacter)
	{
		if (lastTarget == null && !otherCharacter.IsDead() && lastThinker.IsEnemyTo(otherCharacter))
		{
			lastTarget = otherCharacter;
			Debug.Log(this + " OnCharacterDetected " + lastThinker + " detects " + lastTarget + " with " + detectionTrigger);
		}
	}

	private void OnCharacterLost(CharacterController otherCharacter)
	{
		if (lastTarget == otherCharacter)
		{
			Debug.Log(this + " OnCharacterDetected " + lastThinker + " looses " + otherCharacter + " with " + detectionTrigger);
			lastTarget = null;
		}
	}

	private void GetClosestTarget(CharacterController selfCharacter)
	{
		var closest = selfCharacter.GetClosestCharacter(selfCharacter.GetNearbyLivingCharacters(hearingDistance, 1.0f, hearingColor));
		if (closest != null)
		{
			lastTarget = closest;
		}
				
		Debug.Log(this + " GetClosestTarget " + selfCharacter + " finds target: " + lastTarget);
	}
}