using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterRoutines : MonoBehaviour
{
	public const float SearchPathTimeout = 6.0f;
	public const float DefaultWaitSeconds = 1.0f;
	
	public static int debugMode { get; set; }
	public static List<CharacterRoutines> activeInstances = new List<CharacterRoutines>();
	
	public static bool AnyActiveRoutines() => activeInstances.Count > 0;
	
	public static Coroutine DoLookPositionOn(CharacterController character, Vector3 lookPosition)
	{
		return character.StartCoroutine(character.Routines.WaitCharacterLookPosition(lookPosition));
	}

	public System.Action OnStarted;
	
	public bool AnyActive()
	{
		return activeInstances.Contains(this);
	}
	
	public void AssignAttack(CharacterController target, bool canUseRunAction, bool losCheck)
	{
		if(debugMode > 0) Debug.Log(this + " AssignAttack " + target + " canUseRunAction=" + canUseRunAction + " losCheck=" + losCheck);
		StartNewRoutine(WaitCharacterAttackCharacter(target, canUseRunAction, losCheck));
	}
	public void AssignAttackInstant(CharacterController target, bool useAction)
	{
		if(debugMode > 0) Debug.Log(this + " AssignAttackInstant " + target + " usesActions=" + useAction);
		StartNewRoutine(WaitCharacterPerformAttack(target, useAction));
		//OnAssignAction?.Invoke();
	}
	public void AssignInteract(PropComponent clickedProp) 
	{
		if(debugMode > 0) Debug.Log(this + " AssignInteract " + clickedProp);
		StartNewRoutine(WaitCharacterInteract(clickedProp));
	}
	public void AssignMoveToDestination(Vector3 navPosition)
	{
		if(debugMode > 0) Debug.Log(this + " AssignMoveToDestination " + navPosition);
		StartNewRoutine(WaitCharacterMoved(navPosition, 0f));
	}
	public void AssignMoveToWaypoint(Waypoint waypoint, bool canUseRunAction)
	{
		if(debugMode > 0) Debug.Log(this + " AssignMoveToWaypoint " + waypoint + " canUseRunAction=" + canUseRunAction);
		StartNewRoutine(WaitMoveCharacterToWaypoint(waypoint, canUseRunAction));
	}
	public void AssignRunAction()
	{
		if(debugMode > 0) Debug.Log(this + " AssignRunAction ");
		characterOwner.UseAction(CharacterAction.Run);
		StartNewRoutine(WaitAction(CharacterAction.Run));
	}

	public void AssignLookPosition(Vector3 lookPosition)
	{
		if(debugMode > 0) Debug.Log(this + " AssignLookPosition ");
		if (debugMode > 0) DebugDrawer.Line(characterOwner.GetEyesPosition(), lookPosition, Color.magenta, 1.0f);//TODO defaultDrawDuration
		StartNewRoutine(WaitCharacterLookPosition(lookPosition));
	}
	
	// leader assign
	public void AssignMoveLeaderToDestination(Vector3 navPosition)
	{
		if(debugMode > 0) Debug.Log(this + " AssignMoveLeaderToDestination ");
		foreach (var partyCharacter in CharacterController.GetPartyCharacters())
		{
			if (partyCharacter == characterOwner)
			{
				partyCharacter.Routines.AssignMoveToDestination(navPosition);
			}
			else
			{
				partyCharacter.Routines.AssignFollows(characterOwner);
			}
		}
	}

	public void AssignFollows(CharacterController leader)
	{
		if(debugMode > 0) Debug.Log(this + " AssignFollows " + leader);
		StopAllRoutines();
		
		var follow = GetComponent<CharacterFollows>();
		if (!follow) follow = gameObject.AddComponent<CharacterFollows>();
		follow.SetLeader(leader);
		follow.enabled = true;
	}

	public void StopAllRoutines()
	{
		if (activeInstances.Contains(this))
		{
			StopAllCoroutines();
			activeInstances.Remove(this);
		}
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
		OnStarted?.Invoke();
	}
	
	//public Coroutine WaitComplete() => StartCoroutine(_WaitComplete());
	private IEnumerator _WaitComplete()
	{
		if(activeInstances.Contains(this)) Debug.LogError(this + " _WaitComplete activeInstances.Contains(this)");
		activeInstances.Add(this);
		
		while (activeInstances[0] != this) // wait other routines
			yield return null; 
		
		if(debugMode > 0) Debug.Log(this + " routine started " + activeInstances.Count);
		yield return coroutine;
		activeInstances.Remove(this);
		if(debugMode > 0) Debug.Log(" routine ends, next to play: " + activeInstances.FirstOrDefault());
		debugString = "";
	}
	
	private IEnumerator WaitCharacterInteract( PropComponent clickedProp)
	{
		debugString = "WaitCharacterInteract";
		
		var agent = characterOwner.GetAgent();
		yield return agent.AssignWaypointDestinationAsync(clickedProp);

		var bestWaypoint = agent.GetBestWaypoint();
		if (!bestWaypoint)
		{
			Debug.LogWarning(this + " no best waypoint, abort WaitCharacterInteract");
			yield break;
		}
		
		if(debugMode > 0) Debug.Log(characterOwner + " WaitCharacterInteract " + clickedProp);
		yield return StartCoroutine(WaitCharacterMoved(bestWaypoint.GetPosition(), bestWaypoint.radius));
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
		else if(debugMode > 0) 
		{
			var dist = Vector3.Distance(characterOwner.GetPosition(), bestWaypoint.GetPosition());
			Debug.Log(this + " not at reach: " + dist + " / " + agent.GetMinReachDistance() + " to " + bestWaypoint);
		}
	}
	
	private IEnumerator WaitCharacterAttackCharacter(CharacterController target, bool canUseRunAction, bool losCheck)
	{
		if(debugMode > 0) Debug.Log("WaitCharacterAttackCharacter " + characterOwner + " " + target + " canUseRunAction=" + canUseRunAction + " losCheck=" + losCheck);

		if (characterOwner.TryGetAnimation(out var characterAnim))
		{
			if (characterAnim.TrySwitchWeapons(true))
			{
				do
				{
					debugString = "TrySwitchWeapons";
					yield return null;
				} while (characterAnim.IsSwitchingWeapons);
				if(debugMode > 0) Debug.Log("weapons switched for attacking");
			}
		}

		var atAttackRange = false;
		while (!atAttackRange)
		{
			yield return null;

			atAttackRange = characterOwner.IsAtAttackRange(target, losCheck);
			if (atAttackRange)
			{
				if(debugMode > 0) Debug.Log(characterOwner + " atAttackRange of " + target + ", StopAgent");
				characterOwner.StopAgent();
			}
			
			else if (!characterOwner.IsMoving())
			{
				if (!characterOwner.HasMovePoints() && canUseRunAction && characterOwner.CanUseAction(CharacterAction.Run))
				{
					yield return characterOwner.StartCoroutine(WaitAction(CharacterAction.Run));
				}
				
				if (!characterOwner.HasMovePoints())
				{
					if(debugMode > 0) Debug.Log(characterOwner + " has no move points to reach target");
					characterOwner.FloatingText_Cant();
					yield break;
				}

				var reachDistance = characterOwner.GetAttackRange(target);
				if(debugMode > 0) Debug.Log(this + " starts moving towards target ; reachDistance=" + reachDistance);
				yield return characterOwner.StartCoroutine(WaitCharacterMoved(target.GetPosition(), reachDistance));
			}
			
			else Debug.LogWarning(characterOwner + " cannot moves towards " + target);
		}
		
		yield return characterOwner.StartCoroutine(WaitCharacterPerformAttack(target, true));
	}

	private IEnumerator WaitCharacterPerformAttack(CharacterController target, bool useAction)
	{
		debugString = "WaitCharacterPerformAttack";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterPerformAttack " + target );
		
		yield return StartCoroutine(WaitCharacterLookPosition(target.GetCenterPosition()));

		if (useAction && !characterOwner.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " cannot attack (no actions)");
			characterOwner.FloatingText_Cant();
			yield return null;
		}
		
		characterOwner.AttackAction();
		yield return characterOwner.WaitAnimationEvent(characterOwner.IsUsingRangeWeapon() ? CharacterAnimation.EventType.Shoot : CharacterAnimation.EventType.Hit);
		//yield return new WaitForSeconds(character.GetDefaultAttackHitTime());//TODO wait attack duration
		//TODO DamageInfo, hit check (GD)
		target.TakeDamage(characterOwner.GetLastAttackDamage(), characterOwner, true);

		if(debugMode > 0) Debug.Log("attack ends");
	}

	private IEnumerator WaitCharacterInteract(PropComponent prop, Waypoint waypoint)
	{
		debugString = "WaitCharacterInteract";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterInteract > " + prop + " @ " + waypoint);
		
		if (!waypoint)
		{
			Debug.LogWarning(this + " WaitCharacterInteract - no waypoint");
			yield break;
		}
		
		var wpDir = waypoint.GetDirection();
		while (!characterOwner.IsFacingDirection(wpDir))
		{
			characterOwner.LookDirection(wpDir, Time.deltaTime);
			yield return null;
		}
		
		characterOwner.UseAction(prop.GetInteractAction(), prop);
		yield return prop.WaitInteraction(characterOwner);
		
		prop.InteractedBy(characterOwner);

		do
		{
			yield return null;
		} while (!prop.AnimFinished());
	}

	private IEnumerator WaitCharacterLookPosition(Vector3 pos) => WaitCharacterLookDirection((pos - characterOwner.GetPosition()).normalized);
	
	private IEnumerator WaitCharacterLookDirection(Vector3 dir)
	{
		debugString = "WaitCharacterLookDirection";
		characterOwner.StopAgent();
		
		while (!characterOwner.IsFacingDirection(dir))
		{
			characterOwner.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
	}
	
	private IEnumerator WaitMoveCharacterToWaypoint(Waypoint waypoint, bool canUseRunAction)
	{
		debugString = "WaitMoveCharacterToWaypoint";
		if(debugMode > 0) Debug.Log(this + " WaitMoveCharacterToWaypoint " + waypoint + " canUseRunAction=" + canUseRunAction);
		yield return characterOwner.StartCoroutine(WaitCharacterMoved(waypoint.GetPosition(), waypoint.radius));
		
		if (characterOwner.WaypointReached(waypoint))
		{
			yield return StartCoroutine(WaitCharacterLookDirection(waypoint.GetDirection()));
		}
		else
		{
			if (canUseRunAction)
			{
				if (!characterOwner.HasActions() && characterOwner.CanUseAction(CharacterAction.Run))
				{
					yield return characterOwner.StartCoroutine(WaitAction(CharacterAction.Run));
					yield return WaitMoveCharacterToWaypoint(waypoint, false);//TODO: multiple run actions??
					yield break;
				}
			}
		}
		
		if(debugMode > 0) Debug.Log(this + " did not reach waypoint");
	}

	private IEnumerator WaitCharacterMoved(Vector3 dest, float reachDistance)
	{
		debugString = "WaitCharacterMoved " + reachDistance.ToString("N2");
		if(debugMode > 0) Debug.Log("WaitCharacterMoved " + characterOwner + " " + reachDistance);
			
		var agent = characterOwner.GetAgent();
		if(agent.IsMoving()) agent.StopMovement(); // stop any other movement
		
		yield return agent.SearchPathAsync(dest);

		var startTime = Time.time;
		var path = default(Vector3[]);
		do
		{
			path = agent.GetCurrentPath();
			if(debugMode > 1) Debug.Log("WaitCharacterMoved path=" + path.Length + " time=" + (Time.time - startTime) );
			if (path.Length > 1) break;
			yield return null;
			if (Time.time - startTime > SearchPathTimeout)
				break;
		} while (true);
		
		if (path.Length <= 1)
		{
			Debug.LogWarning(characterOwner + " TimedOut!      has wrong path: length=" + path.Length);
			yield return new WaitForSeconds(1.5f);
			characterOwner.UseAllActions();
			yield break;
		}
		
		var firstPoint = path[1];
		var dir = (firstPoint - characterOwner.GetPosition()).normalized;
		while (!characterOwner.IsFacingDirection(dir))
		{
			characterOwner.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
		
		if(debugMode > 0) Debug.Log(this + " agent.StartMove ");
		agent.SetReachDistance(Mathf.Max(agent.GetMinReachDistance(), reachDistance));
		agent.StartMove();
		
		var timeout = 0.0f;
		do
		{
			if (timeout > Consts.CharacterMovedTimeout)
			{
				Debug.LogWarning(characterOwner + " TIMED_OUT!");
				characterOwner.FloatingText_Cant();
				characterOwner.StopAgent();
				yield break;
			}

			if (!characterOwner.IsMoving())
			{
				if(debugMode > 0) Debug.Log(characterOwner + " something stopped movement");
				yield break;
			}

			if (!characterOwner.HasMovePoints())
			{
				if(debugMode > 0) Debug.Log(characterOwner + " !HasMovePoints");
				characterOwner.FloatingText_Cant();
				agent.StopMovement();
				yield break;
			}

			var oldPosition = characterOwner.GetPosition();

			if(debugMode > 0) DebugDrawer.Line(oldPosition, dest, Color.cyan, 0f);
			if(debugMode > 0) DebugDrawer.WireArrow(oldPosition, oldPosition + agent.velocity, Vector3.one, Color.blue, 0f);
			if(debugMode > 0) DebugDrawer.WireDisc(dest, Vector3.up, agent.GetRadius(), Color.cyan, 0f);
			
			yield return null;

			var newPosition = characterOwner.GetPosition();
			var deltaPos =
				(newPosition - oldPosition).magnitude; // TODO: use length squared and recalculate move points
			
			if(debugMode > 2) Debug.Log(this + " moving " + deltaPos);
			characterOwner.ConsumeMovePoints(deltaPos, true);

			if (deltaPos < Consts.MinMagnitude) timeout += Time.deltaTime;
			else timeout = 0f;

			if (debugMode > 0)
			{
				if (timeout > 0f) DebugDrawer.WorldLabel(characterOwner.GetCenterPosition(), "timeout=" + timeout.ToString("N2"));
				else DebugDrawer.WorldLabel(characterOwner.GetCenterPosition(), "deltaPos=" + deltaPos.ToString("N2"));
			}
		}
		while (!agent.ReachedDestination(dest));
		
		if(debugMode > 0) Debug.Log(this + " reached destination!");
		agent.StopMovement();
	}
	
	private IEnumerator WaitAction(CharacterAction action)
	{
		debugString = "WaitAction " + action;
		if(debugMode > 0) Debug.Log(">> WaitAction: " + this + " " + action);
		yield return new WaitForSeconds(DefaultWaitSeconds);
	}
	#endregion
	
	private string debugString = "";
	private void OnGUI()
	{
		if (debugMode > 0)
		{
			Utils.DrawWorldGUI(transform.position, activeInstances.IndexOf(this).ToString() + "\n" + Utils.SplitCamelCase(debugString));
		}
	}
}