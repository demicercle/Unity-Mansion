using System;
using System.Collections;
using UnityEditor.UI;
using UnityEngine;


[CreateAssetMenu(fileName = "AIBrain_Chase", menuName = "AIBrain/Chase", order = 0)]
public class AIBrain_Chase : AIBrainBase
{
	[SerializeField] private bool noRun;
	[SerializeField] private bool noLOS;
	[SerializeField] private bool cantBarks;
	[SerializeField] private Color hearingColor = Color.magenta;
	[SerializeField] [Min(0f)] private float hearingDistance = 1.0f;
	[SerializeField] [Min(0f)] private float defaultWait = 1.0f;
	[SerializeField] [Min(0f)] private float defaultTicks = 0.5f;
	[SerializeField] [Min(0f)] private float timeout = 6.0f;
	[SerializeField] [Min(0)] private int maxTurnsToReachTarget = 1;
	
	private CharacterController chasingTarget;
	private CharacterController initCharacter;
	private DetectionTrigger detectionTrigger;
	private int turnCount;

	public override void Init(CharacterController selfCharacter)
	{
		base.Init(selfCharacter);
		
		initCharacter = selfCharacter;
		detectionTrigger = initCharacter.GetComponent<DetectionTrigger>();
		if (!detectionTrigger) detectionTrigger = initCharacter.GetComponentInChildren<DetectionTrigger>();
		
		DetectionTrigger.onDetectionChanged += OnDetectionChanged;
	}

	public override void Dispose()
	{
		DetectionTrigger.onDetectionChanged -= OnDetectionChanged;
		
		base.Dispose();
	}

	private void OnDetectionChanged(CharacterController c1, CharacterController c2, bool detected)
	{
		if (!initCharacter || initCharacter != c1) 
			return;
		
		if (!chasingTarget)
		{
			if (detected && initCharacter.CanAttackOther(c2)) // TODO "Should Attack Target"
			{
				chasingTarget = c2;
			}
		}
		else
		{
			if (c2 == chasingTarget && !detected)
			{
				chasingTarget = null;
			}
		}
		
		Debug.Log(this + " OnDetectionChanged " + detected + " chasingTarget=" + chasingTarget);
	}

	public override int GetPriority(CharacterController selfCharacter)
	{
		//initCharacter = selfCharacter;
		
		if (chasingTarget)
		{
			if (detectionTrigger && !detectionTrigger.IsCharacterDetected(chasingTarget))
			{
				if (!detectionTrigger.TryGetLastKnownPosition(chasingTarget, out var _))
					chasingTarget = null;
			}

			if (chasingTarget.IsDead())
			{
				chasingTarget = null;
			}
		}
		
		if (!chasingTarget)
		{
			if (!detectionTrigger)
			{
				DefaultGetClosestTarget(selfCharacter);
			}
			else
			{
				if (detectionTrigger.TryGetClosestDetectedCharacter(selfCharacter.GetPosition(), out var otherCharacter))
				{
					chasingTarget = otherCharacter;
				}
			}
		}
		
		return !chasingTarget ? -1 : defaultPriority;
	}

	public override IEnumerator Think(CharacterController selfCharacter)
	{
		//initCharacter = selfCharacter;

		if (chasingTarget && detectionTrigger && !detectionTrigger.IsCharacterDetected(chasingTarget))
		{
			if (turnCount >= maxTurnsToReachTarget)
			{
				Debug.Log(this + " turnCount==" + turnCount + " loose target");
				chasingTarget = null;
			}
			else if (detectionTrigger.TryGetLastKnownPosition(chasingTarget, out var lastKnownPosition))
			{
				turnCount += 1;
				Debug.Log(this + " MoveToDestination lastKnownPosition " + chasingTarget + " turn #" + turnCount + "/" + maxTurnsToReachTarget);
				
				selfCharacter.Routines.MoveToDestination(lastKnownPosition);

				var startTime = Time.time;
				do
				{
					yield return new WaitForSeconds(Mathf.Max(0.1f, defaultTicks));

					if (Time.time - startTime > timeout)
					{
						Debug.Log(this + " timeout!");
						yield break;
					}

				} while (chasingTarget 
				         && !chasingTarget.IsDead()
				         && selfCharacter.HasMovePoints()
				         && !detectionTrigger.IsCharacterDetected(chasingTarget)
				        && !selfCharacter.IsAtAttackRange(chasingTarget) );
			}
			else
			{
				chasingTarget = null; // last known position forgot
			}
		}
		
		if (chasingTarget)
		{
			turnCount = 0;
			
			if (!selfCharacter.CanAttack())
			{
				Debug.Log(this + " cannot attack !");
				if (cantBarks) selfCharacter.FloatingText_Cant();
				yield return new WaitForSeconds(Mathf.Max(0.1f, defaultWait));
			}
			
			while (selfCharacter.CanAttack())
			{
				Debug.Log(this + " chase " + chasingTarget);
				
				DebugDrawer.Line(selfCharacter.GetEyesPosition(), chasingTarget.GetCenterPosition(), Color.red, 1.0f);
				selfCharacter.Routines.AssignAttack(chasingTarget, !noRun, noLOS);

				do
				{
					yield return new WaitForSeconds(Mathf.Max(0.1f, defaultTicks));
					
				} while (CharacterRoutines.AnyActiveRoutines());
			}
		}
	}
	
	public override void DrawGizmos(CharacterController selfCharacter)
	{
		Gizmos.color = hearingColor;
		Utils.DrawGizmosCircle(selfCharacter.GetEyesPosition(), default, hearingDistance);
		if(Application.isPlaying && chasingTarget != null) DebugDrawer.Line(selfCharacter.GetEyesPosition(), selfCharacter.GetCenterPosition(), hearingColor, 0.0f);
	}

	/*private void OnCharacterDetected(CharacterController otherCharacter)
	{
		if (lastTarget == null && !otherCharacter.IsDead() && lastThinker.IsEnemyTo(otherCharacter))
		{
			lastTarget = otherCharacter;
			Debug.Log(this + " OnCharacterDetected " + lastThinker + " detects " + lastTarget + " with " + detectionTrigger);
		}
	}*/

	/*private void OnCharacterLost(CharacterController otherCharacter)
	{
		if (lastTarget == otherCharacter)
		{
			Debug.Log(this + " OnCharacterDetected " + lastThinker + " looses " + otherCharacter + " with " + detectionTrigger);
			lastTarget = null;
		}
	}*/

	private void DefaultGetClosestTarget(CharacterController selfCharacter)
	{
		var closest = selfCharacter.GetClosestCharacter(selfCharacter.GetNearbyLivingCharacters(hearingDistance, 1.0f, hearingColor));
		if (closest != null)
		{
			chasingTarget = closest;
		}
				
		Debug.Log(this + " DefaultGetClosestTarget " + selfCharacter + " finds target: " + chasingTarget);
	}
}