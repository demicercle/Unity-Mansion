using System;
using System.Collections;
using System.Linq;
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
	//private DetectionTrigger detectionTrigger;
	private int turnCount;

	public override void Init(CharacterController selfCharacter)
	{
		base.Init(selfCharacter);
		
		/*initCharacter = selfCharacter;
		detectionTrigger = initCharacter.GetComponent<DetectionTrigger>();
		if (!detectionTrigger) detectionTrigger = initCharacter.GetComponentInChildren<DetectionTrigger>();
		if (detectionTrigger)
		{
			detectionTrigger.onCharacterDetected += OnCharacterDetected;
			detectionTrigger.onCharacterLost += OnCharacterLost;
		}*/
	}

	public override void Dispose()
	{
		/*if (detectionTrigger)
		{
			detectionTrigger.onCharacterDetected -= OnCharacterDetected;
			detectionTrigger.onCharacterLost -= OnCharacterLost;
		}*/
		
		base.Dispose();
	}
	
	public override int GetPriority(CharacterController selfCharacter)
	{
		//initCharacter = selfCharacter;
		
		/*if (chasingTarget && chasingTarget.IsDead())
		{
			chasingTarget = null;
		}
		
		if (!chasingTarget)
		{
			if (!detectionTrigger)
			{
				DefaultGetClosestTarget(selfCharacter);
			}
			else
			{
				foreach (var otherCharacter in detectionTrigger.GetDetectedCharacters()
					         .OrderBy(c => Vector3.Distance(selfCharacter.GetPosition(), c.GetPosition())))
				{
					if (!otherCharacter.IsDead() && selfCharacter.IsEnemyTo(otherCharacter))
					{
						chasingTarget = otherCharacter;
						break;
					}
				}
			}
		}*/
		
		return !chasingTarget ? -1 : defaultPriority;
	}

	public override IEnumerator Think(CharacterController selfCharacter)
	{
		//initCharacter = selfCharacter;
		
		/*if (chasingTarget)
		{
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
				selfCharacter.Routines.AssignAttack(chasingTarget, !noRun, !noLOS);

				do
				{
					yield return new WaitForSeconds(Mathf.Max(0.1f, defaultTicks));
					
				} while (CharacterRoutines.AnyActiveRoutines());
			}
		}

		if (chasingTarget && chasingTarget.IsDead())
		{
			chasingTarget = null;
		}*/
		
		yield return null;
	}
	
	/*public override void DrawGizmos(CharacterController selfCharacter)
	{
		Gizmos.color = hearingColor;
		Utils.DrawGizmosCircle(selfCharacter.GetEyesPosition(), default, hearingDistance);
		if(Application.isPlaying && chasingTarget != null) DebugDrawer.Line(selfCharacter.GetEyesPosition(), selfCharacter.GetCenterPosition(), hearingColor, 0.0f);
	}*/

	/*private void OnCharacterDetected(CharacterController otherCharacter)
	{
		if (chasingTarget == null && !otherCharacter.IsDead() && initCharacter.IsEnemyTo(otherCharacter))
		{
			chasingTarget = otherCharacter;
			Debug.Log(this + " OnCharacterDetected " + initCharacter + " detects " + chasingTarget + " with " + detectionTrigger);
		}
	}*/

	/*private void OnCharacterLost(CharacterController otherCharacter)
	{
		if (chasingTarget == otherCharacter)
		{
			Debug.Log(this + " OnCharacterDetected " + initCharacter + " looses " + chasingTarget + " with " + detectionTrigger);
			chasingTarget = null;
		}
	}*/

	/*private void DefaultGetClosestTarget(CharacterController selfCharacter)
	{
		var closest = selfCharacter.GetClosestCharacter(selfCharacter.GetNearbyLivingCharacters(hearingDistance, 1.0f, hearingColor));
		if (closest != null)
		{
			chasingTarget = closest;
		}
				
		Debug.Log(this + " DefaultGetClosestTarget " + selfCharacter + " finds target: " + chasingTarget);
	}*/
}