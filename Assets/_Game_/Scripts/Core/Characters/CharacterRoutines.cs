using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharacterRoutines : MonoBehaviour
{
	public const float DefaultWaitSeconds = 1.0f;
	
	static List<CharacterRoutines> activeInstances = new List<CharacterRoutines>();
	
	public static bool AnyActiveRoutines() => activeInstances.Count > 0;
	
	public void AssignAttack(CharacterController target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log(this + " AssignAttack " + target + " canUseRunAction=" + canUseRunAction + " losCheck=" + losCheck);
		StartNewRoutine(WaitCharacterAttackCharacter(target, canUseRunAction, losCheck));
	}
	public void AssignInteract(PropComponent clickedProp) // TODO Prop Controller
	{
		Debug.Log(this + " AssignInteract " + clickedProp);
		StartNewRoutine(WaitCharacterInteract(clickedProp));
	}
	public void AssignMoveToDestination(Vector3 navPosition)
	{
		Debug.Log(this + " AssignMoveToDestination " + navPosition);
		StartNewRoutine(WaitCharacterMoved(navPosition));
	}
	public void AssignMoveToWaypoint(Waypoint waypoint, bool canUseRunAction)
	{
		Debug.Log(this + " AssignMoveToWaypoint " + waypoint + " canUseRunAction=" + canUseRunAction);
		StartNewRoutine(WaitMoveCharacterToWaypoint(waypoint, canUseRunAction));
	}
	public void AssignRunAction()
	{
		Debug.Log(this + " AssignRunAction ");
		StartNewRoutine(WaitRunAction());
	}
	
#region PRIVATE
	private Coroutine coroutine;
	private CharacterController characterOwner;

	private void Awake()
	{
		characterOwner = GetComponent<CharacterController>();
		if(!characterOwner) Debug.LogError(this + " absolutely needs a character!");
	}

	private void StartNewRoutine(IEnumerator routine)
	{
		StopAllRoutines();
		coroutine = StartCoroutine(routine);
		StartCoroutine(_WaitComplete());
	}

	private void StopAllRoutines()
	{
		if (activeInstances.Contains(this))
		{
			StopAllCoroutines();
			activeInstances.Remove(this);
		}
	}
	
	//public Coroutine WaitComplete() => StartCoroutine(_WaitComplete());
	private IEnumerator _WaitComplete()
	{
		if(activeInstances.Contains(this)) Debug.LogError(this + " _WaitComplete activeInstances.Contains(this)");
		activeInstances.Add(this);
//		Debug.Log(this + " routine started " + activeInstances.Count + " > " + coroutine);
		yield return coroutine;
		activeInstances.Remove(this);
		Debug.Log(" routine ends " + activeInstances.Count);
	}
	
	private IEnumerator WaitCharacterInteract( PropComponent clickedProp)
	{
		if (clickedProp.GetBestWaypoint(characterOwner.GetPosition(), out var bestWaypoint))
		{
			Debug.Log(characterOwner + " WaitCharacterInteract " + clickedProp);
			yield return StartCoroutine(WaitCharacterMoved(bestWaypoint.GetPosition()));
			var agent = characterOwner.GetAgent();
			if (agent.ReachedDestination(bestWaypoint.GetPosition()))
			{
				// waypoint reach
				if (characterOwner.CanInteract())
				{
					yield return StartCoroutine(WaitCharacterInteract(clickedProp, bestWaypoint));
				}
				else
				{
					characterOwner.FloatingText_Cant();
				}
			}
			else
			{
				var dist = Vector3.Distance(characterOwner.GetPosition(), bestWaypoint.GetPosition());
				Debug.Log(this + " not at reach: " + dist + " / " + agent.GetMinReachDistance() + " to " + bestWaypoint);
			}
		}
		else Debug.LogWarning(this + " AssignInteract " + clickedProp + " no waypoint!");
	}
	
	private IEnumerator WaitCharacterAttackCharacter(CharacterController target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log("WaitCharacterAttackCharacter " + characterOwner + " " + target + " canUseRunAction=" + canUseRunAction + " losCheck=" + losCheck);

		if (characterOwner.TryGetAnimation(out var characterAnim))
		{
			if (characterAnim.TrySwitchWeapons(true))
			{
				do
				{
					yield return null;
				} while (characterAnim.IsSwitchingWeapons);
				Debug.Log("weapons switched for attacking");
			}
		}

		var atAttackRange = false;
		while (!atAttackRange)
		{
			yield return null;

			atAttackRange = characterOwner.IsAtAttackRange(target, losCheck);
			if (atAttackRange)
			{
				Debug.Log(characterOwner + " atAttackRange of " + target + ", StopAgent");
				characterOwner.StopAgent();
			}
			
			else if (!characterOwner.IsMoving())
			{
				while (!characterOwner.HasMovePoints() && canUseRunAction && characterOwner.CanUseRunAction())
				{
					yield return characterOwner.StartCoroutine(WaitRunAction());
				}
				
				if (!characterOwner.HasMovePoints())
				{
					Debug.Log(characterOwner + " has no move points to reach target");
					characterOwner.FloatingText_Cant();
					yield break;
				}
				
				Debug.Log(characterOwner + " starts moving towards target");
				characterOwner.StartCoroutine(WaitCharacterMoved(target.GetPosition()));
				yield return null;
			}
			
			//else Debug.Log(characterOwner + " moves towards " + target);
		}
		
		yield return characterOwner.StartCoroutine(WaitCharacterPerformAttack(target));
	}

	private IEnumerator WaitCharacterPerformAttack(CharacterController target)
	{
		Debug.Log(this + " WaitCharacterPerformAttack " + target );
		
		yield return StartCoroutine(WaitCharacterLookPosition(target.GetCenterPosition()));

		if (!characterOwner.CanAttack())
		{
			Debug.Log(this + " cannot attack!");
			characterOwner.FloatingText_Cant();
			yield return new WaitForSeconds(Consts.CantSeconds);
		}
		else
		{
			characterOwner.AttackAction();
			yield return characterOwner.WaitAnimationEvent(CharacterAnimation.EventType.Hit);
			//yield return new WaitForSeconds(character.GetDefaultAttackHitTime());//TODO wait attack duration
			//TODO DamageInfo, hit check (GD)
			target.TakeDamage(characterOwner.GetLastAttackDamage());
		}

		Debug.Log("attack ends");
	}

	private IEnumerator WaitCharacterInteract(PropComponent prop, Waypoint waypoint = null)
	{
		Debug.Log(this + " WaitCharacterInteract > " + prop + " @ " + waypoint);
		
		if (waypoint != null)
		{
			var wpDir = waypoint.GetDirection();

			while (!characterOwner.IsFacingDirection(wpDir))
			{
				characterOwner.LookDirection(wpDir, Time.deltaTime);
				yield return null;
			}
		}
		
		characterOwner.UseAction(prop.GetInteractAction());
			
		yield return new WaitForSeconds(prop.GetTriggerDelay());

		prop.InteractedBy(characterOwner);
		
		yield return new WaitForSeconds(prop.GetAnimDuration());
	}

	private IEnumerator WaitCharacterLookPosition(Vector3 pos) => WaitCharacterLookDirection((pos - characterOwner.GetPosition()).normalized);
	
	private IEnumerator WaitCharacterLookDirection(Vector3 dir)
	{
		characterOwner.StopAgent();
		
		while (!characterOwner.IsFacingDirection(dir))
		{
			characterOwner.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
	}
	
	private IEnumerator WaitMoveCharacterToWaypoint(Waypoint waypoint, bool canUseRunAction)
	{
		Debug.Log(this + " WaitMovecharacterOwnerToWaypoint " + waypoint + " canUseRunAction=" + canUseRunAction);
		yield return characterOwner.StartCoroutine(WaitCharacterMoved(waypoint.GetPosition()));
		
		if (characterOwner.WaypointReached(waypoint))
		{
			yield return StartCoroutine(WaitCharacterLookDirection(waypoint.GetDirection()));
		}
		else
		{
			if (canUseRunAction)
			{
				if (!characterOwner.HasActions() && characterOwner.CanUseRunAction())
				{
					yield return characterOwner.StartCoroutine(WaitRunAction());
					yield return WaitMoveCharacterToWaypoint(waypoint, false);//TODO: multiple run actions??
					yield break;
				}
			}
		}
		
		Debug.Log(this + " did not reach waypoint");
	}

	private IEnumerator WaitCharacterMoved(Vector3 dest)
	{
		Debug.Log("WaitCharacterMoved " + characterOwner);
		
		var agent = characterOwner.GetAgent();
		
		yield return agent.SearchPathAsync(dest);
		
		var path = agent.GetCurrentPath();
		if (path.Length > 1)
		{
			var firstPoint = path[1];
			var dir = (firstPoint - characterOwner.GetPosition()).normalized;
			while (!characterOwner.IsFacingDirection(dir))
			{
				characterOwner.LookDirection(dir, Time.deltaTime);
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning(characterOwner + " has wrong path: length=" + path.Length);
			yield break;
		}
		
		agent.StartMove();
		
		var timeout = 0.0f;
		do
		{
			if (timeout > Consts.CharacterMovedTimeout)
			{
				Debug.LogWarning(characterOwner + " TIMED_OUT!");
				characterOwner.FloatingText("TIMED_OUT!");
				yield break;
			}

			if (!characterOwner.IsMoving())
			{
				Debug.Log(characterOwner + " something stopped movement");
				yield break;
			}

			if (!characterOwner.HasMovePoints())
			{
				Debug.Log(characterOwner + " !HasMovePoints");
				characterOwner.FloatingText_Cant();
				agent.StopMovement();
				yield break;
			}

			var oldPosition = characterOwner.GetPosition();

			DebugDrawer.Line(oldPosition, dest, Color.cyan, 0f);
			DebugDrawer.WireArrow(oldPosition, oldPosition + agent.velocity, Vector3.one, Color.blue, 0f);
			DebugDrawer.WireDisc(dest, Vector3.up, agent.GetRadius(), Color.cyan, 0f);
			yield return null;

			var newPosition = characterOwner.GetPosition();
			var deltaPos =
				(newPosition - oldPosition).magnitude; // TODO: use length squared and recalculate move points

			characterOwner.ConsumeMovePoints(deltaPos);

			if (deltaPos < Consts.MinMagnitude) timeout += Time.deltaTime;
			else timeout = 0f;

			if (timeout > 0f) DebugDrawer.WorldLabel(characterOwner.GetCenterPosition(), "timeout=" + timeout.ToString("N2"));
			else DebugDrawer.WorldLabel(characterOwner.GetCenterPosition(), "deltaPos=" + deltaPos.ToString("N2"));
		}
		while (!agent.ReachedDestination(dest));
		
		Debug.Log(characterOwner + " reached destination!");
		agent.StopMovement();
	}
	private IEnumerator WaitRunAction()
	{
		Debug.Log(">> WaitRunAction: " + this);
		characterOwner.RunAction();
		yield return new WaitForSeconds(DefaultWaitSeconds);
	}
	#endregion
}