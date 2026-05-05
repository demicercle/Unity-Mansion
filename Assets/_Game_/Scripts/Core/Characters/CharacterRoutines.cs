using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using EventType = CharacterAnimation.EventType;

public class CharacterRoutines : MonoBehaviour
{
	private const float SearchPathTimeout = 6.0f;
	private const float UnfreezeAgentTimeout = 0.0f;
	private const float DefaultWaitSeconds = 1.0f;
	private  float MinDistanceFromPathLastPoint = 0.0f; // TODO?
	private const float MinDeltaToMove = 0.1f;
	private const float HeightPenalty = 1.0f;
	private const float MinGiveDistance = 0.25f;
	
	public static int debugMode { get; set; }
	public static List<CharacterRoutines> activeInstances = new List<CharacterRoutines>();
	
	
	
	public static bool AnyActiveRoutines() => activeInstances.Count > 0;
	
	public static CharacterRoutines[]  GetActiveRoutines() => activeInstances.ToArray();
	
	public static Coroutine DoLookPositionOn(CharacterEntity character, Vector3 lookPosition)
	{
		return character.StartCoroutine(character.Routines.WaitCharacterLookPosition(lookPosition));
	}

	public System.Action onStarted, onStopped;

	private object assignedTarget;
	
	public float LastTimeAssignedTarget { get; private set; }

	public bool HasAssignedTarget() => assignedTarget != null;
	
	public bool AnyActive()
	{
		return activeInstances.Contains(this);
	}
	
	public void AssignMoveAndAttack(IAttackable target, bool losCheck, bool useAction = true)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = target;
		if(debugMode > 0) Debug.Log(this + " AssignAttack " + target + " useAction=" + useAction  + " losCheck=" + losCheck + " time=" + LastTimeAssignedTarget);
		StartNewRoutine(WaitCharacterAttack(target, losCheck, true));
	}

	public void AssignShootAt(Vector3 position, bool useAction = true)
	{
		if(debugMode > 0) Debug.Log(this + " AssignShootAt " + position);
		StartNewRoutine(WaitCharacterShootAt(position, useAction));
	}
	
	public void AssignPerformAttack(IAttackable target, bool useAction = true)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = target;
		if(debugMode > 0) Debug.Log(this + " AssignAttackInstant " + target + " useAction=" + useAction);
		StartNewRoutine(WaitCharacterPerformAttack(target, useAction));
		//OnAssignAction?.Invoke();
	}
	
	public void AssignInteract(PropComponent clickedProp) 
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = clickedProp;
		if(debugMode > 0) Debug.Log(this + " AssignInteract " + clickedProp);
		StartNewRoutine(WaitCharacterInteract(clickedProp));
	}
	public void AssignInteractAtWaypoint(PropComponent clickedProp, Waypoint waypoint)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = clickedProp;
		if(debugMode > 0) Debug.Log(this + " AssignInteractAtWaypoint " + clickedProp + " " + waypoint);
		StartNewRoutine(WaitCharacterInteractAtWaypoint(clickedProp, waypoint));
	}
	public void AssignMoveToDestination(Vector3 navPosition, bool raycastForDestructible = false)
	{
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignMoveToDestination " + navPosition);
		StartNewRoutine(WaitCharacterMoved(navPosition, 0f, raycastForDestructible));
	}
	public void AssignMoveToWaypoint(Waypoint waypoint, System.Action onComplete = null)
	{
		assignedTarget = waypoint;
		if(debugMode > 0) Debug.Log(this + " AssignMoveToWaypoint " + waypoint );
		StartNewRoutine(WaitMoveCharacterToWaypoint(waypoint, onComplete));
	}
	public void AssignUseAction(CharacterAction waitAction)
	{
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignWaitAction " + waitAction);
		StartNewRoutine(WaitAction(waitAction));
	}
	public void AssignLookPosition(Vector3 lookPosition)
	{
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignLookPosition ");
		if (debugMode > 0) DebugDrawer.Line(_selfCharacter.GetEyesPosition(), lookPosition, Color.magenta, 1.0f);//TODO defaultDrawDuration
		StartNewRoutine(WaitCharacterLookPosition(lookPosition));
	}
	public void AssignTalk(CharacterEntity npc) // TODO all " Assign " methods should return the coroutine
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = npc;
		if(debugMode > 0) Debug.Log(this + " AssignTalk " + npc);
		StartCoroutine(WaitCharacterTalk(npc));
	}
	public void AssignUseItemOn(ItemData itemToUse, object objectTo)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = objectTo;
		if(debugMode > 0) Debug.Log(this + " AssignUseItem " + itemToUse + " on " + objectTo);
		StartCoroutine(WaitCharacterUseItemOn(itemToUse, objectTo));
	}

	public void AssignUseItemAt(ItemData itemToUse, Vector3 position)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignUseItem " + itemToUse + " at " + position);
		StartCoroutine(WaitCharacterUseItemAt(itemToUse, position));
	}


	public void AssignGiveItem(ItemData draggedItem, CharacterEntity partyChar)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = partyChar;
		if(debugMode > 0) Debug.Log(this + " AssignGiveItem " + draggedItem + " " + partyChar);
		StartNewRoutine(WaitCharacterGiveItem(draggedItem, partyChar));
	}
	
	public void AssignDropItem(ItemData draggedItem,  Vector3 worldPosition)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignDropItem " + draggedItem + " " + worldPosition);
		StartNewRoutine(WaitCharacterDropItem(draggedItem, worldPosition));
	}
	
	// leader assign
	public void Leader_AssignMoveToDestination(Vector3 navPosition)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignMoveToDestination ");
		_selfCharacter.Routines.AssignMoveToDestination(navPosition, false);
		Leader_AssignFollows();
	}
	public void Leader_AssignInteract(PropComponent clickedProp)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignInteract ");
		_selfCharacter.Routines.AssignInteract(clickedProp);
		Leader_AssignFollows();
	}
	public void Leader_AssignAttack(IAttackable other)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignAttack ");
		_selfCharacter.Routines.AssignMoveAndAttack(other, _selfCharacter.IsUsingRangeWeapon());
		Leader_AssignFollows();
	}
	public void Leader_AssignTalk(CharacterEntity npc)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignTalk ");
		_selfCharacter.Routines.AssignTalk(npc);
		Leader_AssignFollows();
	}
	
	private void AssignFollows(CharacterEntity leader)
	{
		if(debugMode > 0) Debug.Log(this + " AssignFollows " + leader);
		StopAllRoutines();
		
		var follow = GetComponent<CharacterFollows>();
		if (!follow) follow = gameObject.AddComponent<CharacterFollows>();
		follow.SetLeader(leader);
		follow.enabled = true;
	}

	private void Leader_AssignFollows()
	{
		var leader = default(CharacterEntity);
		
		foreach (var partyCharacter in CharacterEntity.GetPartyCharacters())
		{
			if (partyCharacter == _selfCharacter)
			{
				leader = partyCharacter;
				break;
			}
		}
		
		if (!leader)
		{
			Debug.LogWarning(this + " can't Leader_AssignFollows");
			return;
		}

		foreach (var partyCharacter in CharacterEntity.GetPartyCharacters().OrderBy(c => Vector3.Distance(leader.GetPosition(), c.GetPosition())))
		{
			if (partyCharacter != _selfCharacter)
			{
				partyCharacter.Routines.AssignFollows(leader);
				leader = partyCharacter;
			}
		}
	}
	
	public void StopAllRoutines()
	{
		if (activeInstances.Contains(this))
		{
			StopAllCoroutines();
			activeInstances.Remove(this);
			Stopped();
		}
	}
	
#region PRIVATE

	private void StartNewRoutine(IEnumerator routine)
	{
		StopAllRoutines();
		StartCoroutine(_WaitComplete(routine));
		onStarted?.Invoke();
	}

	private CharacterEntity _selfCharacter;

	private void OnEnable()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
		if(!_selfCharacter) Debug.LogError(this + " absolutely needs a character!");
	}

	private void OnDisable()
	{
		StopAllRoutines();
	}
	
	//public Coroutine WaitComplete() => StartCoroutine(_WaitComplete());
	private IEnumerator _WaitComplete(IEnumerator routine)
	{
		if(activeInstances.Contains(this)) Debug.LogError(this + " _WaitComplete activeInstances.Contains(this)");
		activeInstances.Add(this);
		
		//while (activeInstances[0] != this) // wait other routines
		//	yield return null; 
		
		if(debugMode > 0) Debug.Log(this + " routine started " + activeInstances.Count);
		yield return StartCoroutine(routine);
		activeInstances.Remove(this);
		if(debugMode > 0) Debug.Log(" routine ends, next to play: " + activeInstances.FirstOrDefault());
		debugString = "";
		Stopped();
	}
	
	private void Stopped() => onStopped?.Invoke();
	
	private IEnumerator WaitCharacterInteract(PropComponent clickedProp)
	{
		debugString = "WaitCharacterInteract";
		if(debugMode > 0) Debug.Log(_selfCharacter + " WaitCharacterInteract " + clickedProp);
		
		var agent = _selfCharacter.GetAgent();
		agent.StopMovement(); // force agent to stop while calculating destination path
		
		yield return agent.AssignWaypointDestinationAsync(clickedProp);
		
		var foundWaypoint = agent.GetBestWaypoint();
		if (foundWaypoint)
		{
			yield return StartCoroutine(WaitCharacterInteractAtWaypoint(clickedProp, foundWaypoint));
		}
		else
		{
			yield return StartCoroutine(WaitCharacterInteractAtPosition(clickedProp, clickedProp.GetPosition()));
		}
	}

	private IEnumerator WaitCharacterInteractAtWaypoint(PropComponent clickedProp, Waypoint foundWaypoint)
	{
		var destination = foundWaypoint.GetPosition();
		var direction = foundWaypoint.GetDirection();
		var reachRadius = foundWaypoint.radius;

		var agent = _selfCharacter.GetAgent();
		agent.SetReachDistance(reachRadius);

		yield return StartCoroutine(WaitCharacterMoved(destination, reachRadius, false));
		
		if(debugMode > 0) Debug.Log(this + " WaitCharacterInteractAtWaypoint " + clickedProp + " " + foundWaypoint + " check reach destination");
		if (agent.ReachedDestination(destination))
		{
			// waypoint reach
			if (_selfCharacter.CanInteract())
			{
				while (!_selfCharacter.IsFacingDirection(direction))
				{
					_selfCharacter.LookDirection(direction, Time.deltaTime);
					yield return null;
				}
		
				_selfCharacter.UseAction(clickedProp.GetInteractAction(), clickedProp);
				yield return clickedProp.WaitInteraction(_selfCharacter);
		
				clickedProp.InteractedBy(_selfCharacter);

				do
				{
					yield return null;
				} while (!clickedProp.AnimFinished());
			}
			else
			{
				_selfCharacter.FloatingText_Cant();
			}
		}
		else if(debugMode > 0) 
		{
			Debug.Log(this + " not at reach: ");
		}
	}

	private IEnumerator WaitCharacterInteractAtPosition(PropComponent clickedProp, Vector3 destination)
	{
		var direction = Vector3.Scale(new Vector3(1,0,1), (destination - _selfCharacter.GetPosition()).normalized);
		var reachRadius = _selfCharacter.GetRadius() + clickedProp.GetRadius();

		var agent = _selfCharacter.GetAgent();
		agent.SetReachDistance(reachRadius);

		yield return StartCoroutine(WaitCharacterMoved(destination, reachRadius, false));
		yield return null;
		
		if(debugMode > 0) Debug.Log(this + " WaitCharacterInteractAtPosition " + clickedProp + " check reach destination");
		if (agent.ReachedDestination(destination))
		{
			// waypoint reach
			if (_selfCharacter.CanInteract())
			{
				while (!_selfCharacter.IsFacingDirection(direction))
				{
					_selfCharacter.LookDirection(direction, Time.deltaTime);
					yield return null;
				}
		
				_selfCharacter.UseAction(clickedProp.GetInteractAction(), clickedProp);
				yield return clickedProp.WaitInteraction(_selfCharacter);
		
				clickedProp.InteractedBy(_selfCharacter);

				do
				{
					yield return null;
				} while (!clickedProp.AnimFinished());
			}
			else
			{
				_selfCharacter.FloatingText_Cant();
			}
		}
		else if(debugMode > 0) 
		{
			Debug.Log(this + " not at reach") ;
		}
	}
	
	private IEnumerator WaitCharacterAttack(IAttackable target, bool losCheck, bool raycastForDestructible)
	{
		if(debugMode > 0) Debug.Log("WaitCharacterAttackCharacter " + _selfCharacter + " " + target + " losCheck=" + losCheck);

		if (_selfCharacter.TryGetAnimation(out var characterAnim))
		{
			yield return characterAnim.SwitchWeapons(true);
		}

		var atAttackRange = _selfCharacter.IsAtAttackRange(target);

		if (!atAttackRange)
		{
			var reachDistance = _selfCharacter.GetAttackReachDistance(target);
			if(debugMode > 0) Debug.Log(this + " starts moving towards target "+target+ " ; reachDistance=" + reachDistance);
			yield return StartCoroutine(WaitCharacterMoved(target.GetReachPosition(), reachDistance, raycastForDestructible));
			atAttackRange = _selfCharacter.IsAtAttackRange(target);
		}
		
		if (atAttackRange)
		{
			if (losCheck && !LOSCheck(target))
				yield break;
			
			if(debugMode > 0) Debug.Log(_selfCharacter + " atAttackRange of " + target + ", StopAgent");
			_selfCharacter.StopAgent();
			yield return StartCoroutine(WaitCharacterPerformAttack(target, true));
		}
	}

	private bool LOSCheck(IAttackable target)
	{
		if (!_selfCharacter.LOSCheck(target))
		{
			if (debugMode > 0) Debug.Log(_selfCharacter + " los obstructed, try to find better position");
			_selfCharacter.FloatingText_Cant();
			return false;
		}

		return true;
	}

	private IEnumerator WaitCharacterShootAt(Vector3 worldPosition, bool useAction)
	{
		debugString = "WaitCharacterShootAt";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterShootAt " + worldPosition );
		
		if (useAction && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " cannot attack (no actions)");
			_selfCharacter.FloatingText_Cant();
			yield break;
		}

		IAttackable other = null;
		RaycastHit hit = default;

		_selfCharacter.RaycastShoot(out hit);
		
		hit.collider?.TryGetComponent(out other);
		
		if(debugMode > 0) Debug.Log(this + " WaitCharacterShootAt touch " + other);
		yield return StartCoroutine(WaitCharacterHitOrShoot(other, useAction, hit));
	}

	private IEnumerator WaitCharacterPerformAttack(IAttackable target, bool useAction)
	{
		debugString = "WaitCharacterPerformAttack";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterPerformAttack " + target + " useAction=" + useAction );

		yield return null;
		
		if(!_selfCharacter.IsFacingPosition(target.GetReachPosition()))
			yield return StartCoroutine(WaitCharacterLookPosition(target.GetReachPosition()));

		if (useAction && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " cannot attack (no actions)");
			_selfCharacter.FloatingText_Cant();
			yield break;
		}
		
		_selfCharacter.SetAimingPosition(target.GetCenterPosition());

		yield return StartCoroutine(WaitCharacterHitOrShoot(target, useAction, default));
	}

	private IEnumerator WaitCharacterHitOrShoot(IAttackable target, bool useAction, RaycastHit shootHit)
	{
		var atkRoll = 0.0f;

		var currentWeapon = _selfCharacter.GetCurrentWeapon();
		if (currentWeapon.IsReloadable() && currentWeapon.IsEmptyAmmo())
		{
			_selfCharacter.FloatingText(LanguageSettings.Main.Empty);
			yield break;
		}
		
		if(useAction) 
			_selfCharacter.UseAction(CharacterAction.Attack, useAction, target);
		
		if (target != null)
		{
			atkRoll = _selfCharacter.RollDice(); // we roll the dice before for the animation
			if(debugMode > 0) DebugDrawer.Line(_selfCharacter.GetEyesPosition(), target.GetCenterPosition(), Color.red, 2.0f);
		}
		
		yield return _selfCharacter.WaitAnimationEvent(_selfCharacter.IsUsingRangeWeapon() ? CharacterAnimation.EventType.Shoot : CharacterAnimation.EventType.Hit);

		//yield return new WaitForSeconds(character.GetDefaultAttackHitTime());//TODO wait attack duration
		//TODO DamageInfo, hit check (GD)

		if (target != null)
		{
			var atkResult = _selfCharacter.TryHit(target, atkRoll);
			switch (atkResult)
			{
				case CharacterEntity.AttackResult.Miss:
					Debug.Log(this + " AttackResult.Miss " + target);
					//GameCanvas.Instance.FloatingText(_selfCharacter, LanguageSettings.GetSettings(DialogFiles.main).GetLine(10, "miss!"));
					break;
			
				case CharacterEntity.AttackResult.Dodge:
					Debug.Log(this + " AttackResult.Dodge by " + target);
					//GameCanvas.Instance.FloatingText(target, LanguageSettings.GetSettings(DialogFiles.main).GetLine(11, "dodge!"));
					break;
			
				case CharacterEntity.AttackResult.Hit:
					Debug.Log(this + " AttackResult.Hit " + target);
					var damageInfo = new DamageInfo()
					{
						amount = _selfCharacter.GetLastAttackDamage(atkRoll),
						isRanged = _selfCharacter.IsUsingRangeWeapon()
					};
					target.TakeDamage(damageInfo, _selfCharacter);
					_selfCharacter.GetCurrentWeapon().TrySpawnDecal(shootHit);
					break;
			}

			if (target is CharacterEntity)
			{
				var defender = target as CharacterEntity;
			
				if (!defender.IsDead() && defender.TryGetComponent(out CharacterDetection detection))
				{
					detection.SetCharacterDetected(_selfCharacter, true); // warn target if attack missed
				}

				if (defender.TryRiposte(_selfCharacter))
				{
					do yield return null;
					while (defender.Routines.AnyActive());
				}
			}
		}
		else
		{
			_selfCharacter.GetCurrentWeapon().TrySpawnDecal(shootHit);
		}
		
		if(debugMode > 0) Debug.Log(this + " attack ends");
	}

	private IEnumerator WaitCharacterLookPosition(Vector3 pos) => WaitCharacterLookDirection((pos - _selfCharacter.GetPosition()).normalized);
	
	private IEnumerator WaitCharacterLookDirection(Vector3 dir)
	{
		debugString = "WaitCharacterLookDirection";
		_selfCharacter.StopAgent();
	
		while (dir.sqrMagnitude > Consts.MinMagnitude && !_selfCharacter.IsFacingDirection(dir))
		{
			_selfCharacter.GetAgent().updateRotation = false;
			_selfCharacter.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
		
		_selfCharacter.GetAgent().updateRotation = true;
	}
	
	private IEnumerator WaitMoveCharacterToWaypoint(Waypoint waypoint, System.Action onComplete = null)
	{
		debugString = "WaitMoveCharacterToWaypoint";
		if(debugMode > 0) Debug.Log(this + " WaitMoveCharacterToWaypoint " + waypoint);
		
		yield return StartCoroutine(WaitCharacterMoved(waypoint.GetPosition(), waypoint.radius, false));
		
		if (_selfCharacter.WaypointReached(waypoint))
		{
			if(!waypoint.ignoreRotation)
				yield return StartCoroutine(WaitCharacterLookDirection(waypoint.GetDirection()));
		}
		
		onComplete?.Invoke();
	}

	private List<Waypoint> ignoreWaypoints = new List<Waypoint>();
	private IEnumerator WaitCharacterMoved(Vector3 dest, float reachDistance, bool raycastForDestructible)
	{
		reachDistance = Mathf.Max(reachDistance, _selfCharacter.GetAgent().GetMinReachDistance());

		var currentDistance = NavUtils.NavDistance(dest,  _selfCharacter.GetPosition());
		debugString = "WaitCharacterMoved " + reachDistance.ToString("N2");
		if(debugMode > 0) Debug.Log("WaitCharacterMoved " + _selfCharacter + " reachDistance=" + reachDistance + " currentDistance=" + currentDistance);
		
		if (currentDistance <= reachDistance)
		{
			if(debugMode > 0) Debug.Log("character already at reachDistance=" + reachDistance+ ", abort");
			yield break;
		}
			
		var agent = _selfCharacter.GetAgent();
		agent.StopMovement(); // stop any movement
		
		yield return agent.AssignDestination(dest);
		
		if (!agent.hasDestination)
		{
			foreach (var wp in Waypoint.GetOrderedWaypointsFromTo(_selfCharacter.GetPosition(), dest))
			{
				dest = wp.GetPosition();
				
				if(debugMode > 0) Debug.Log(this + " Assign Waypoint dest=" + wp);
				yield return agent.AssignDestination(dest);
				if (agent.hasDestination)
					break;
			}
		}

		if (!agent.hasDestination)
		{
			Debug.LogWarning(this + " WaitCharacterMoved !agent.hasDestination");
			yield break;
		}
		
		var path = agent.GetCurrentPath();
		if (path.Length < 1)
		{
			Debug.LogWarning(this + " path invalid");
			yield break;
		}
		
		if(debugMode > 0) Debug.Log(this + " move to path: " + path.Length);
		if(debugMode > 0) DebugDrawer.Path(path, false, Color.blue, 1.0f);

		var firstPoint = path[1];
		var dir = (firstPoint - _selfCharacter.GetPosition()).normalized;
		while (dir.sqrMagnitude > Consts.MinMagnitude && !_selfCharacter.IsFacingDirection(dir))
		{
//			if (debugMode > 0) Debug.Log(this + " WaitMoveLastPath LookDirection");
			_selfCharacter.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
		
		var startTime = Time.time;
		var hasMoved = false;
		while (!agent.ReachedDestination(dest))
		{
			debugTimeout = (Time.time - startTime);

			if (!agent.IsMoving())
			{
				if(debugMode > 0) Debug.Log(this + " agent.StartMove and SetReachDistance");
				agent.SetReachDistance(reachDistance);
				agent.StartMove();
			}
			
			if (Time.time - startTime > SearchPathTimeout)
			{
				Debug.LogWarning(this + " TIMED_OUT!");
				_selfCharacter.FloatingText_Cant();
				_selfCharacter.StopAgent();
				yield break;
			}

			if (!_selfCharacter.IsMoving())
			{
				if (hasMoved)
				{
					if(debugMode > 0) Debug.Log(this + " something stopped movement");
					yield break;
				}
			}
			
			if (!_selfCharacter.HasMovePoints())
			{
				if(debugMode > 0) Debug.Log(this + " !HasMovePoints");
				if(_selfCharacter.IsControllableByPlayer()) _selfCharacter.FloatingText_Cant();
				agent.StopMovement();
				yield break;
			}

			if (raycastForDestructible && _selfCharacter.RaycastForDestructible(out var destructible))
			{
				if(debugMode > 0) Debug.Log(this + " RaycastForDestructible " + destructible);
				yield return StartCoroutine(WaitCharacterAttack(destructible, false, false));
				yield break;
			}

			var oldPosition = _selfCharacter.GetPosition();

			if(debugMode > 0) DebugDrawer.Path(path, false, Color.cyan);
			if(debugMode > 0) DebugDrawer.WireArrow(oldPosition, oldPosition + agent.velocity, Vector3.one, Color.cyan, 0f);
			if(debugMode > 0) DebugDrawer.WireDisc(dest, agent.GetRadius(), Color.cyan, 0f);
			
			yield return null;

			var newPosition = _selfCharacter.GetPosition();
			var deltaPos = (newPosition - oldPosition).magnitude; // TODO: use length squared and recalculate move points
			if (deltaPos >= MinDeltaToMove) // character has moved
			{
				hasMoved = true;
				//agent.DropOnGround(); // to avoid overlapping obstacles
				if(debugMode > 1) Debug.Log(this + " moving " + deltaPos);
				startTime = Time.time; // reset timeout
			}
			
			_selfCharacter.ConsumeMovePoints(GameTurnManager.Instance.IsTurnBasedMode() ? deltaPos : 0f, true);
		}
		
		if(debugMode > 0) Debug.Log(this + " WaitMoveLastPath reached destination reachDistance=" + reachDistance + " current navDistance=" + NavUtils.NavDistance(dest, _selfCharacter.GetPosition()));
		agent.StopMovement();
	}
	
	private IEnumerator WaitAction(CharacterAction action)
	{
		debugString = "WaitAction " + action;
		if(debugMode > 0) Debug.Log(">> WaitAction: " + this + " " + action);
		yield return new WaitForSeconds(DefaultWaitSeconds*0.25f);
		_selfCharacter.UseAction(action);
		yield return new WaitForSeconds(DefaultWaitSeconds*0.75f);
	}

	private IEnumerator WaitCharacterTalk(CharacterEntity npc)
	{
		debugString = "WaitCharacterTalk " + npc;
		if(debugMode > 0) Debug.Log(">> WaitCharacterTalk: " + this + " " + npc);

		var talkDistance = _selfCharacter.GetTalkDistance(npc);
		yield return StartCoroutine(WaitCharacterMoved(npc.GetPosition(),talkDistance,false));

		if (!_selfCharacter.GetAgent().ReachedDestination())
		{
			Debug.LogWarning(this + " talk distance not reached, abort");
			yield break;
		}

		while (!_selfCharacter.IsFacingPosition(npc.GetPosition()))
		{
			_selfCharacter.LookWorldPosition(npc.GetPosition(), Time.deltaTime);
			yield return null;
		}

		while (!npc.IsFacingPosition(_selfCharacter.GetPosition()))
		{
			npc.LookWorldPosition(_selfCharacter.GetPosition(), Time.deltaTime);
			yield return null;
		}

		if(debugMode > 0) Debug.Log(this + " Use Talk action");
		_selfCharacter.UseAction(CharacterAction.Talk, npc);
	}

	private IEnumerator WaitCharacterUseItemOn( ItemData itemToUse, object objectTo)
	{
		var useRange = itemToUse.GetUseRange();
		if (objectTo is CharacterEntity)
		{
			var otherCharacter = objectTo as CharacterEntity;
			if (useRange > 0 && NavUtils.NavDistance(_selfCharacter.GetPosition(), otherCharacter.GetPosition()) >
			    useRange)
			{
				if(debugMode > 0) Debug.Log(this + " WaitCharacterUseItem WaitCharacterMoved");
				yield return StartCoroutine(WaitCharacterMoved(otherCharacter.GetPosition(), useRange, false));
			}

			if (useRange <= 0 ||
			    NavUtils.NavDistance(_selfCharacter.GetPosition(), otherCharacter.GetPosition()) <= useRange)
			{
				if (debugMode > 0) Debug.Log(this + " WaitCharacterUseItem at useRange");
				while (otherCharacter != _selfCharacter && !_selfCharacter.IsFacingPosition(otherCharacter.GetPosition()))
				{
					_selfCharacter.LookWorldPosition(otherCharacter.GetPosition(), Time.deltaTime);
					yield return null;
				}
				
				if (!_selfCharacter.TryGetComponent(out Inventory inventory))
				{
					Debug.LogWarning(_selfCharacter + " has no inventory");
					yield break;
				}
				
				_selfCharacter.UseAction(CharacterAction.UseItem, otherCharacter);
				
				if (_selfCharacter.TryGetAnimation(out CharacterAnimation anim))
				{
					yield return anim.WaitEvent(EventType.Hit);
				}

				if (!inventory.TryRemoveItem(itemToUse))
				{
					Debug.LogWarning("item cannot be removed from inventory");
					yield break;
				}
				
				itemToUse.UseOn(otherCharacter.gameObject, _selfCharacter.gameObject);
				inventory.NotifyUsed(itemToUse, otherCharacter);
			}
		}
	}

	private IEnumerator WaitCharacterUseItemAt(ItemData itemToUse, Vector3 position)
	{
		if(debugMode > 0) Debug.Log(this + " WaitCharacterUseItemAt " + itemToUse);
		debugString = "WaitCharacterUseItemAt " + itemToUse;
		if (!_selfCharacter.TryGetComponent(out Inventory inventory))
		{
			Debug.LogWarning(_selfCharacter + " has no inventory");
			yield break;
		}
				
		_selfCharacter.UseAction(CharacterAction.UseItem, itemToUse);
		if (_selfCharacter.TryGetAnimation(out CharacterAnimation anim))
		{
			anim.WaitEvents(EventType.Hit, EventType.Shoot);
			do
			{
				_selfCharacter.SetItemToUse(itemToUse, debugMode > 0);
				yield return null;
			} while (anim.IsWaitingEvents);
			if(debugMode > 0) Debug.Log(this + " item Shoot or Hit");
		}

		if (!inventory.TryRemoveItem(itemToUse))
		{
			Debug.LogWarning("item cannot be removed from inventory");
			yield break;
		}

		if (debugMode > 0) Debug.Log(this + " launch projectile");
		itemToUse.LaunchProjectile(_selfCharacter.gameObject, position);
		inventory.NotifyUsed(itemToUse, position);
		yield return new WaitForSeconds(DefaultWaitSeconds); // to end anim?
		_selfCharacter.SetItemToUse(null, debugMode > 0);
	}

	private IEnumerator WaitCharacterGiveItem(ItemData draggedItem, CharacterEntity partyChar)
	{
		debugString = "WaitCharacterGiveItem";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterGiveItem " + draggedItem + " to " + partyChar);

		_selfCharacter.StopFollow();
		partyChar.StopFollow();

		var reachDistance = partyChar.GetRadius() + _selfCharacter.GetRadius() + MinGiveDistance;
		
		if (_selfCharacter.GetDistanceTo(partyChar.GetPosition()) > reachDistance)
		{
			yield return StartCoroutine(WaitCharacterMoved(partyChar.GetPosition(), reachDistance, false));
		}

		if (_selfCharacter.GetDistanceTo(partyChar.GetPosition()) <= reachDistance)
		{
			if(debugMode > 0) Debug.Log(this + " WaitCharacterGiveItem reachDistance=" + reachDistance);
			
			while (!_selfCharacter.IsFacingPosition(partyChar.GetPosition()))
			{
				_selfCharacter.LookWorldPosition(partyChar.GetPosition(), Time.deltaTime);
				yield return null;
			}
			
			_selfCharacter.TryGetComponent(out Inventory selfInventory);
			partyChar.TryGetComponent(out Inventory partyInventory);
			if (selfInventory && partyInventory)
			{
				if (!partyInventory.TryAddItem(draggedItem) || !selfInventory.TryRemoveItem(draggedItem))
				{
					if(debugMode > 0) Debug.Log(this + " WaitCharacterGiveItem FloatingText_Cant");
					_selfCharacter.FloatingText_Cant();
				}
				else
				{
					if(debugMode > 0) Debug.Log(this + " WaitCharacterGiveItem NotifyTransfered");
					Inventory.NotifyTransfered(selfInventory, partyInventory, draggedItem);
					
					_selfCharacter.UseAction(CharacterAction.GiveItem, true);
					
					if (_selfCharacter.TryGetAnimation(out var anim))
						yield return anim.WaitEvents(EventType.Hit, EventType.Shoot);
				}
			}
		}
	}
	
	private IEnumerator WaitCharacterDropItem(ItemData draggedItem, Vector3 worldPosition)
	{
		debugString = "WaitCharacterDropItem";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterDropItem " + draggedItem + " to " + worldPosition);

		_selfCharacter.StopFollow();
		
		var reachDistance =_selfCharacter.GetRadius() + MinGiveDistance;
		if (_selfCharacter.GetDistanceTo(worldPosition) > reachDistance)
		{
			yield return StartCoroutine(WaitCharacterMoved(worldPosition, reachDistance, false));
		}

		if (_selfCharacter.GetDistanceTo(worldPosition) <= reachDistance)
		{
			_selfCharacter.TryGetComponent(out Inventory selfInventory);
			
			if (selfInventory)
			{
				if (!selfInventory.TryCreateLootBag(draggedItem, worldPosition, out var lootBag) || !selfInventory.TryRemoveItem(draggedItem))
				{
					_selfCharacter.FloatingText_Cant();
				}
				else
				{
					Inventory.NotifyTransfered(selfInventory, lootBag, draggedItem);
					
					_selfCharacter.UseAction(CharacterAction.DropItem, true);
					
					if (_selfCharacter.TryGetAnimation(out var anim))
						yield return anim.WaitEvents(EventType.Hit, EventType.Shoot);
				}
			}
		}
	}
	#endregion

	private float debugTimeout = 0;
	private string debugString = "";
	private void OnGUI()
	{
		if (debugMode > 0)
		{
			Utils.DrawWorldGUI(transform.position, activeInstances.IndexOf(this).ToString() + "\n" + Utils.SplitCamelCase(debugString) + "\n" + debugTimeout.ToString("N2"));
		}
	}

	public void AssignReloadWeapon(WeaponData weapon, ItemData ammoItem)
	{
		if(debugMode > 0) Debug.Log(this + " AssignReloadWeapon " + weapon + " ammo=" + ammoItem);
		StartNewRoutine(WaitCharacterReloadWeapon(weapon, ammoItem));
	}

	private IEnumerator WaitCharacterReloadWeapon(WeaponData weapon, ItemData ammoItem)
	{
		debugString = "WaitCharacterReloadWeapon";
		if (debugMode > 0) Debug.Log(this + " WaitCharacterReloadWeapon " + weapon + " ammo=" + ammoItem);
		
		_selfCharacter.SetCurrentWeapon(weapon);
		_selfCharacter.TryGetAnimation(out var anim);
		
		if (anim)
		{
			anim.SwitchWeapons(true);
			do yield return null;
			while (anim.IsSwitchingWeapons);
		}

		if (!_selfCharacter.TryGetComponent(out Inventory selfInventory))
		{
			Debug.LogWarning(this + " has no inventory to reload weapon");
			yield break;
		}

		if (weapon.IsFullAmmo())
		{
			_selfCharacter.FloatingText(LanguageSettings.Main.Full);
			yield break;
		}
		
		_selfCharacter.UseAction(CharacterAction.Reload, true);

		while (ammoItem.GetQuantity() > 0 && !weapon.IsFullAmmo())
		{
			ammoItem.AddQuantity(-1);
			weapon.AddAmmo(1);
			yield return null;
		}
		
		if (ammoItem.GetQuantity() <= 0)
		{
			if(!selfInventory.TryRemoveItem(ammoItem))
			{
				Debug.LogWarning(this + " unable to remove item " + ammoItem);
			}
		}

		selfInventory.NotifyChanged();

		if (anim)
		{
			yield return new WaitForSeconds(anim.GetReloadAnimDuration());
		}
		else
		{
			yield return new WaitForSeconds(DefaultWaitSeconds);
		}
	}
}