using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class CharacterRoutines
{
	public static bool HasCoroutines() => coroutineScheduler != null && coroutineScheduler.HasCoroutines();
	
	public static void AssignAttack(CharacterController attacker, CharacterController target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log(" AssignAttack " + attacker + " " + target+ " " + canUseRunAction + " " + losCheck);
		QueueRoutine(WaitCharacterAttackCharacter(attacker, target, canUseRunAction, losCheck));
	}
	public static void AssignInteract(CharacterController character, PropComponent clickedProp) // TODO Prop Controller
	{
		Debug.Log(" AssignInteract " + character + " " + clickedProp);
		QueueRoutine(WaitCharacterInteract(character, clickedProp));
	}
	public static void AssignMoveToDestination(CharacterController controller, Vector3 navPosition)
	{
		Debug.Log(" AssignMove " + controller + " " + navPosition);
		QueueRoutine(WaitCharacterMoved(controller, navPosition));
	}
	public static void AssignMoveToWaypoint(CharacterController character, Waypoint waypoint, bool canUseRunAction)
	{
		QueueRoutine(WaitMoveCharacterToWaypoint(character, waypoint, canUseRunAction));
	}
	public static void AssignRunAction(CharacterController character)
	{
		Debug.Log(" AssignRunAction " + character);
		QueueRoutine(WaitRunAction(character));
	}
	
	private static IEnumerator WaitCharacterInteract(CharacterController character, PropComponent clickedProp)
	{
		if (clickedProp.GetBestWaypoint(character.GetPosition(), out var bestWaypoint))
		{
			Debug.Log(" WaitCharacterInteract " + character + " " + clickedProp);
			yield return character.StartCoroutine(WaitCharacterMoved(character, bestWaypoint.GetPosition()));
			var agent = character.GetAgent();
			if (agent.ReachedDestination(bestWaypoint.GetPosition()))
			{
				// waypoint reach
				if (character.CanInteract())
				{
					yield return character.StartCoroutine(WaitCharacterInteract(character, clickedProp, bestWaypoint));
				}
				else
				{
					character.FloatingText_Cant();
				}
			}
			else
			{
				var dist = Vector3.Distance(character.GetPosition(), bestWaypoint.GetPosition());
				Debug.Log(character + " not at reach: " + dist + " / " + agent.GetMinReachDistance() + " to " + bestWaypoint);
			}
		}
		else Debug.LogWarning(" AssignInteract " + character + " " + clickedProp + " no waypoint!");
	}
	
	private static IEnumerator WaitCharacterAttackCharacter(CharacterController attacker, CharacterController target, bool canUseRunAction, bool losCheck)
	{
		Debug.Log("WaitCharacterAttackCharacter " + attacker + " " + target + " " + losCheck);

		var atAttackRange = false;
		while (!atAttackRange)
		{
			yield return null;

			atAttackRange = attacker.IsAtAttackRange(target, losCheck);
			if (atAttackRange)
			{
				Debug.Log(attacker + " atAttackRange of " + target + ", StopAgent");
				attacker.StopAgent();
			}
			
			else if (!attacker.IsMoving())
			{
				while (!attacker.HasMovePoints() && canUseRunAction && attacker.CanUseRunAction())
				{
					yield return attacker.StartCoroutine(WaitRunAction(attacker));
				}
				
				if (!attacker.HasMovePoints())
				{
					Debug.Log(attacker + " has no move points to reach target");
					attacker.FloatingText_Cant();
					yield break;
				}
				
				Debug.Log(attacker + " starts moving towards target");
				attacker.StartCoroutine(WaitCharacterMoved(attacker, target.GetPosition()));
				yield return null;
			}
			
			//else Debug.Log(attacker + " moves towards " + target);
		}
		
		yield return attacker.StartCoroutine(WaitCharacterPerformAttack(attacker, target));
	}

	private static IEnumerator WaitCharacterPerformAttack(CharacterController character, CharacterController target)
	{
		Debug.Log("WaitCharacterPerformAttack " + character + " " + target );
		
		yield return character.StartCoroutine(WaitCharacterLookPosition(character, target.GetCenterPosition()));

		if (!character.CanAttack())
		{
			Debug.Log(character + " cannot attack!");
			character.FloatingText_Cant();
			yield return new WaitForSeconds(Consts.CantSeconds);
		}
		else
		{
			character.AttackAction();
			yield return character.WaitAnimationEvent(CharacterAnimation.EventType.Hit);
			//yield return new WaitForSeconds(character.GetDefaultAttackHitTime());//TODO wait attack duration
			// apply attack damage, etc
			target.TakeDamage(character.GetLastAttackDamage());
		}

		Debug.Log("attack ends");
	}

	private static IEnumerator WaitCharacterInteract(CharacterController character, PropComponent prop, Waypoint waypoint = null)
	{
		Debug.Log("WaitCharacterInteract " + character + " > " + prop + " @ " + waypoint);
		
		if (waypoint != null)
		{
			var wpDir = waypoint.GetDirection();

			while (!character.IsFacingDirection(wpDir))
			{
				character.LookDirection(wpDir, Time.deltaTime);
				yield return null;
			}
		}
		
		character.UseAction(prop.consumeAction);
			
		yield return new WaitForSeconds(prop.triggerDelay);

		prop.GetController().Interact();
		
		yield return new WaitForSeconds(prop.animDuration);
	}

	private static IEnumerator WaitCharacterLookPosition(CharacterController character, Vector3 pos) => WaitCharacterLookDirection(character, (pos - character.GetPosition()).normalized);
	
	private static IEnumerator WaitCharacterLookDirection(CharacterController character, Vector3 dir)
	{
		character.StopAgent();
		
		while (!character.IsFacingDirection(dir))
		{
			character.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
	}
	
	private static IEnumerator WaitMoveCharacterToWaypoint(CharacterController character, Waypoint waypoint,
		bool canUseRunAction)
	{
		Debug.Log(" WaitMoveCharacterToWaypoint " + character + " " + waypoint + " canUseRunAction=" + canUseRunAction);
		yield return character.StartCoroutine(WaitCharacterMoved(character, waypoint.GetPosition()));
		
		if (character.WaypointReached(waypoint))
		{
			yield return character.StartCoroutine(WaitCharacterLookDirection(character, waypoint.GetDirection()));
		}
		else
		{
			if (canUseRunAction)
			{
				if (!character.HasActions() && character.CanUseRunAction())
				{
					yield return character.StartCoroutine(WaitRunAction(character));
					yield return WaitMoveCharacterToWaypoint(character, waypoint, false);//TODO: multiple run actions??
					yield break;
				}
			}
		}
		
		Debug.Log(character + " did not reach waypoint");
	}

	private static IEnumerator WaitCharacterMoved(CharacterController movingCharacter, Vector3 dest, System.Action onComplete = null)
	{
		Debug.Log("WaitCharacterMoved " + movingCharacter);
		
		var agent = movingCharacter.GetAgent();
		
		yield return agent.SearchPathAsync(dest);
		
		var path = agent.GetCurrentPath();
		if (path.Length > 1)
		{
			var firstPoint = path[1];
			var dir = (firstPoint - movingCharacter.GetPosition()).normalized;
			while (!movingCharacter.IsFacingDirection(dir))
			{
				movingCharacter.LookDirection(dir, Time.deltaTime);
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning(movingCharacter + " has wrong path: length=" + path.Length);
			yield break;
		}
		
		agent.StartMove();
		
		var timeout = 0.0f;
		while(true)
		{
			if (timeout > Consts.CharacterMovedTimeout)
			{
				Debug.LogWarning(movingCharacter + " TIMED_OUT!");
				movingCharacter.FloatingText("TIMED_OUT!");
				break;
			}

			if (!movingCharacter.IsMoving())
			{
				Debug.Log(movingCharacter + " something stopped movement");
				break;
			}
			
			if (!movingCharacter.HasMovePoints())
			{
				Debug.Log(movingCharacter + " !HasMovePoints");
				movingCharacter.FloatingText_Cant();
				agent.StopMovement();
				break;
			}

			if (agent.ReachedDestination(dest))
			{
				Debug.Log(movingCharacter + " reached destination!");
				agent.StopMovement();
				break;
			}
			
			var oldPosition = movingCharacter.GetPosition();

			DebugDrawer.Line(oldPosition, dest, Color.cyan, 0f);
			DebugDrawer.WireArrow(oldPosition, oldPosition + agent.velocity, Vector3.one, Color.blue, 0f);
			DebugDrawer.WireDisc(dest, Vector3.up, agent.GetRadius(), Color.cyan, 0f);
			yield return null;
			
			var newPosition = movingCharacter.GetPosition();
			var deltaPos = (newPosition -  oldPosition).magnitude; // TODO: use length squared and recalculate move points

			movingCharacter.ConsumeMovePoints(deltaPos);
			
			if (deltaPos < Consts.MinMagnitude) timeout += Time.deltaTime;
			else timeout = 0f;
			
			if(timeout > 0f) DebugDrawer.WorldLabel(movingCharacter.GetCenterPosition(), "timeout=" + timeout.ToString("N2"));
			else DebugDrawer.WorldLabel(movingCharacter.GetCenterPosition(), "deltaPos=" + deltaPos.ToString("N2"));
		}
		
		onComplete?.Invoke();
	}
	private static IEnumerator WaitRunAction(CharacterController character)
	{
		Debug.Log(">> WaitRunAction: " + character);
		yield return new WaitForSeconds(Consts.DefaultActionsSeconds);
		character.RunAction();
		yield return new WaitForSeconds(Consts.DefaultActionsSeconds);
	}

	/*private static IEnumerator WaitAnimationEvent(CharacterController character, CharacterAnimation.EventType eventType)
	{
		if (!character.TryGetAnimation(out var characterAnimation))
		{
			if (eventType == CharacterAnimation.EventType.Hit)
				yield return new WaitForSeconds(character.GetDefaultAttackHitTime());
			else
				yield return new WaitForSeconds(Consts.DefaultActionsSeconds);
		}
		else
		{
			yield return characterAnimation.WaitEvent(eventType);
		}
	}*/

	private static CoroutineScheduler coroutineScheduler;

	private static Coroutine QueueRoutine(IEnumerator routine)
	{
		if (coroutineScheduler == null)
		{
			coroutineScheduler = CoroutineScheduler.CreateNewInstance()
				.SetDebug(true);
		}

		return coroutineScheduler.Queue(routine).Wait();
	}
	//private static Coroutine StartCoroutine(IEnumerator routine) => CoroutineScheduler.StartRoutine(routine);
}