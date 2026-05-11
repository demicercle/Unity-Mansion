using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UI;
using EventType = CharacterAnimation.EventType;

public class CharacterRoutines : MonoBehaviour
{
	private const float SearchPathTimeout = 6.0f;
	private const float DefaultWaitSeconds = 1.0f;
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

	public void AssignMoveAndAttack(IAttackable target, float reachDistance) =>
		AssignMoveAndAttack(target, reachDistance, true, true);
	public void AssignMoveAndAttack(IAttackable target, bool useAction) =>
		AssignMoveAndAttack(target, _selfCharacter.GetAttackReachDistance(target), true, useAction);
	public void AssignMoveAndAttack(IAttackable target) =>
		AssignMoveAndAttack(target, _selfCharacter.GetAttackReachDistance(target), true, true);
	public void AssignMoveAndAttack(IAttackable target, float reachDistance, bool losCheck, bool useAction)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = target;
		if(debugMode > 0) Debug.Log(this + " AssignAttack " + target + " useAction=" + useAction  + " losCheck=" + losCheck + " reachDistance=" + reachDistance + " time=" + LastTimeAssignedTarget);
		StartNewRoutine(WaitCharacterAttack(target, reachDistance, losCheck, !_selfCharacter.InParty()));
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
	public void AssignMoveToDestination(Vector3 navPosition, float reachDistance, bool raycastForDestructible = false)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignMoveToDestination " + navPosition);
		StartNewRoutine(WaitCharacterMoved(navPosition, reachDistance, raycastForDestructible));
	}
	public void AssignMoveToWaypoint(Waypoint waypoint, System.Action onComplete = null)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = waypoint;
		if(debugMode > 0) Debug.Log(this + " AssignMoveToWaypoint " + waypoint );
		StartNewRoutine(WaitMoveCharacterToWaypoint(waypoint, onComplete));
	}
	public void AssignUseAction(CharacterAction waitAction)
	{
		LastTimeAssignedTarget = Time.time;
		assignedTarget = null;
		if(debugMode > 0) Debug.Log(this + " AssignWaitAction " + waitAction);
		StartNewRoutine(WaitAction(waitAction));
	}
	public void AssignLookPosition(Vector3 lookPosition)
	{
		LastTimeAssignedTarget = Time.time;
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
		_selfCharacter.Routines.AssignMoveToDestination(navPosition, _selfCharacter.GetAgent().GetMinReachDistance(), false);
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
		_selfCharacter.Routines.AssignMoveAndAttack(other);
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
		if(debugMode > 0) Debug.Log(this + " routine ends " + activeInstances.Count);
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
		
		if(debugMode > 0) Debug.Log(this + " WaitCharacterInteractAtWaypoint prop=" + clickedProp + " waypoint=" + foundWaypoint + " reachRadius=" + reachRadius);
		if (agent.ReachedDestination(destination, reachRadius))
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

		if (!_selfCharacter.TryGetClosestNavigablePosition(destination, out destination))
		{
			Debug.LogWarning(this + " cannot find navigable position");
			_selfCharacter.FloatingText_Cant();
			yield break;
		}

		yield return StartCoroutine(WaitCharacterMoved(destination, reachRadius, false));
		yield return null;
		
		if(debugMode > 0) Debug.Log(this + " WaitCharacterInteractAtPosition " + clickedProp + " check reach destination");
		if (agent.ReachedDestination(destination, reachRadius))
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

	private IEnumerator WaitCharacterAttack(IAttackable target, bool losCheck, bool raycastForDestructible) =>
		WaitCharacterAttack(target, _selfCharacter.GetAttackReachDistance(target), losCheck, raycastForDestructible);
	
	private IEnumerator WaitCharacterAttack(IAttackable target, float reachDistance, bool losCheck, bool raycastForDestructible)
	{
		if(debugMode > 0) Debug.Log("WaitCharacterAttack " + _selfCharacter + " " + target + " losCheck=" + losCheck + " reachDistance=" + reachDistance);

		if (_selfCharacter.TryGetAnimation(out var characterAnim))
		{
			characterAnim.SwitchWeapons(true);
			do yield return null;
			while (characterAnim.IsSwitchingWeapons);
		}

		var atAttackRange = _selfCharacter.IsAtAttackRange(target, losCheck);

		if (!atAttackRange)
		{
			reachDistance = Mathf.Min(reachDistance, _selfCharacter.GetAttackReachDistance(target));
			
			if(debugMode > 0) Debug.Log(this + " starts moving towards target "+target+ " ; reachDistance=" + reachDistance);
			yield return StartCoroutine(WaitCharacterMoved(target.GetPosition(), reachDistance, raycastForDestructible));
			
			atAttackRange = _selfCharacter.IsAtAttackRange(target, losCheck);
			if (debugMode > 0) Debug.Log(this + " ends move, atAttackRange=" + atAttackRange);
		}
		
		if (atAttackRange)
		{
			_selfCharacter.StopAgent();
			
			if (losCheck && !LOSCheck(target))
			{
				if (debugMode > 0) Debug.Log(_selfCharacter + " cannot attack " + target);
				yield break;
			}
			
			if(debugMode > 0) Debug.Log(_selfCharacter + " atAttackRange of " + target + "");
			yield return StartCoroutine(WaitCharacterPerformAttack(target, true));
		}
		else
		{
			if(debugMode > 0) Debug.Log(_selfCharacter + " no at attack range");
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
		
		_selfCharacter.StopAgent();
		
		if (useAction && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " cannot attack (no actions)");
			_selfCharacter.FloatingText_Cant();
			yield break;
		}

		if (_selfCharacter.TryGetAnimation(out var anim))
		{
			anim.SwitchWeapons(true);
			do yield return null;
			while (anim.IsSwitchingWeapons);
		}

		while (!_selfCharacter.IsFacingPosition(worldPosition))
		{
			_selfCharacter.LookWorldPosition(worldPosition, Time.deltaTime);
			yield return null;
		}
		
		yield return StartCoroutine(YieldWeaponSwitch());
		
		_selfCharacter.UseAction(CharacterAction.Attack, useAction, null);

		yield return _selfCharacter.WaitAnimationEvent(_selfCharacter.IsUsingRangeWeapon() ? CharacterAnimation.EventType.Shoot : CharacterAnimation.EventType.Hit);

		yield return StartCoroutine(ProcessLastShootResults(-1));
	}

	private IEnumerator WaitCharacterPerformAttack(IAttackable target, bool useAction)
	{
		debugString = "WaitCharacterPerformAttack";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterPerformAttack " + target + " useAction=" + useAction );

		yield return null;

		if (useAction && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " cannot attack (no actions)");
			_selfCharacter.FloatingText_Cant();
			yield break;
		}

		yield return StartCoroutine(YieldWeaponSwitch());
		
		var targetPos = target.GetPosition();
		while (!_selfCharacter.IsFacingPosition(targetPos))
		{
			_selfCharacter.LookWorldPosition(targetPos, Time.deltaTime);
			yield return null;
		}
		
		var currentWeapon = _selfCharacter.GetCurrentWeapon();
		if (currentWeapon.IsReloadable() && currentWeapon.IsEmptyAmmo())
		{
			_selfCharacter.FloatingText(LanguageSettings.Main.Empty);
			yield break;
		}

		yield return StartCoroutine(YieldWeaponSwitch());
		
		_selfCharacter.UseAction(CharacterAction.Attack, useAction, target);
		
		yield return null;
		
		var atkRoll = target is CharacterEntity ? _selfCharacter.RollDice() : -1;
		yield return _selfCharacter.WaitAnimationEvent(_selfCharacter.IsUsingRangeWeapon() ? CharacterAnimation.EventType.Shoot : CharacterAnimation.EventType.Hit);

		_selfCharacter.SetAimingPosition(_selfCharacter.GetAimingPosition(target));
		yield return new WaitForFixedUpdate();
		yield return StartCoroutine(ProcessLastShootResults(atkRoll, target));
		_selfCharacter.SetAiming(false);
		yield return null;
		
		if(debugMode > 0) Debug.Log(this + " attack ends");
		yield return StartCoroutine(WaitForNavigation());
	}

	private IEnumerator YieldWeaponSwitch()
	{
		if (_selfCharacter.TryGetAnimation(out var anim))
		{
			if (!anim.WeaponsSwitched())
			{
				if(debugMode > 0) Debug.Log(this + " YieldWeaponSwitch");
				anim.SwitchWeapons(true);

				do yield return null;
				while (anim.IsSwitchingWeapons);
			}
		}
	}
	
	private IEnumerator ProcessLastShootResults(int atkRoll, IAttackable forceHitTarget = null)
	{
		if(debugMode > 0) Debug.Log(this + " ProcessLastShootResults atkRoll=" + atkRoll + " forceHitTarget=" + forceHitTarget);
		
		var ignoredColliders = new List<Collider>();
		var weapon = _selfCharacter.GetCurrentWeapon();
		var shootIndex = 0;
		var fromPosition = _selfCharacter.GetShootFromPosition();
		if (_selfCharacter.GetLastShootHits(out var shoots))
		{
			foreach (var shootHit in shoots)
			{
				if(debugMode > 0) Debug.Log(this + " " + shootIndex + " shootHit=" + shootHit.collider);

				var delay = weapon.GetShootDelay(shootIndex);
				
				if (delay > 0) 
					yield return new WaitForSeconds(delay);
				else 
					yield return null;
				
				shootIndex++;

				if (_selfCharacter.TryGetAnimation(out var anim))
				{
					foreach (var wp in anim.GetWeaponRenderers())
					{
						wp.OnShoot(fromPosition, shootHit.point);
					}
				}
				
				if (shootHit.collider)
				{
					if (!weapon.AllowMultipleHit() && ignoredColliders.Contains(shootHit.collider)) 
						continue; // to prevent hitting twice the same target
					
					ignoredColliders.Add(shootHit.collider);
					
					if (shootHit.collider.TryGetComponent(out IAttackable target))
					{
						ProcessHit(target, atkRoll);
						
						if (target == forceHitTarget)
							forceHitTarget = null;
						
						continue;
					}
				}
				
				weapon.TrySpawnDecal(shootHit);
			}
		}
		
		if(forceHitTarget != null)
			ProcessHit(forceHitTarget, atkRoll); 
	}

	private void ProcessHit(IAttackable target, int atkRoll)
	{
		if (atkRoll < 0f)
			atkRoll = target is CharacterEntity ? _selfCharacter.RollDice() : DicePool.RollDice();
		
		if (debugMode > 0) Debug.Log(this + " ProcessHit " + " atkRoll=" + atkRoll + " target=" + target);
		
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
				var damageInfo = new DamageInfo()
				{
					amount = _selfCharacter.GetLastAttackDamage(atkRoll),
					isRanged = _selfCharacter.IsUsingRangeWeapon()
				};
				Debug.Log(this + " AttackResult.Hit " + target + " " + damageInfo);
				target.TakeDamage(damageInfo, _selfCharacter);
				break;
		}

		if (target is CharacterEntity)
		{
			var defender = target as CharacterEntity;
			
			if (!defender.IsDead() && defender.TryGetComponent(out CharacterDetection detection))
			{
				detection.SetCharacterDetected(_selfCharacter, true); // warn target if attack missed
			}

			defender.TryRiposte(_selfCharacter);
		}
	}

	private IEnumerator WaitCharacterLookPosition(Vector3 pos) => WaitCharacterLookDirection(pos - _selfCharacter.GetPosition());
	
	private IEnumerator WaitCharacterLookDirection(Vector3 dir)
	{
		debugString = "WaitCharacterLookDirection";
		_selfCharacter.StopAgent();
		
		while (!_selfCharacter.IsFacingDirection(dir))
		{
			_selfCharacter.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
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
		if(debugMode > 0) Debug.Log("WaitCharacterMoved " + _selfCharacter + " reachDistance=" + reachDistance + " currentDistance=" + currentDistance + " raycastForDestructible=" + raycastForDestructible);
		
		if (currentDistance <= reachDistance)
		{
			if(debugMode > 0) Debug.Log("character already at reachDistance=" + reachDistance+ ", abort");
			yield break;
		}
			
		var agent = _selfCharacter.GetAgent();
		agent.StopMovement(); // stop any movement
		agent.SetReachDistance(reachDistance);
		
		if (debugMode > 0) Debug.Log(this + " assign destination: " + dest);
		yield return agent.AssignDestination(dest);

		if (!agent.hasDestination)
		{
			if (raycastForDestructible && debugMode > 0) Debug.Log(this + " !agent.hasDestination RaycastForDestructible");
			if (raycastForDestructible && _selfCharacter.RaycastForDestructible(0f, 2.0f, out var destructible))
			{
				agent.StopMovement();
				yield return StartCoroutine(WaitCharacterPerformAttack(destructible, true));
				yield break;
			}
			
			Debug.LogWarning(this + " WaitCharacterMoved !agent.hasDestination");
			yield break;
		}
		
		var path = agent.GetCurrentPath();
		if (path.Length < 1)
		{
			Debug.LogWarning(this + " path invalid");
			yield break;
		}
		
		if(debugMode > 0) DebugDrawer.Path(path, false, Color.blue, 1.0f);
			
		if(debugMode > 0) Debug.Log(this + " agent.StartMove and SetReachDistance=" + reachDistance);
		agent.SetReachDistance(reachDistance);
		agent.StartMove();
		
		var startTime = Time.time;
		while (!agent.ReachedDestination(dest, reachDistance))
		{
			debugTimeout = (Time.time - startTime);

			if (debugTimeout < 0.01f && !agent.IsMoving())
			{
				if(debugMode > 0) Debug.Log(this + " force start move");
				agent.StartMove();
			}

			while (_selfCharacter.TryGetAnimation(out var anim) && anim.IsSwitchingWeapons)
			{
				if(debugMode > 1) Debug.Log(this + " wait switch weapons");
				do yield return null;
				while (anim.IsSwitchingWeapons);
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
				if(debugMode > 0) Debug.Log(this + " something stopped movement");
				yield break;
			}
			
			if (!_selfCharacter.HasMovePoints())
			{
				if(debugMode > 0) Debug.Log(this + " !HasMovePoints");
				if(_selfCharacter.IsControllableByPlayer()) _selfCharacter.FloatingText_Cant();
				agent.StopMovement();
				yield break;
			}

			if (raycastForDestructible && _selfCharacter.RaycastForDestructible(0.5f, 1.5f, out var destructible))
			{
				if(debugMode > 0) Debug.Log(this + " RaycastForDestructible " + destructible);
				agent.StopMovement();
				yield return StartCoroutine(WaitCharacterPerformAttack(destructible, true));
				yield break;
			}

			var oldPosition = _selfCharacter.GetPosition();

			if(debugMode > 0) DebugDrawer.Path(path, false, Color.cyan);
			if(debugMode > 0) DebugDrawer.WireArrow(oldPosition, oldPosition + agent.velocity, Vector3.one, Color.cyan, 0f);
			if(debugMode > 0) DebugDrawer.WireDisc(dest, agent.GetRadius(), Color.cyan, 0f);
			
			yield return null;

			var newPosition = _selfCharacter.GetPosition();
			var deltaPos = (newPosition - oldPosition).magnitude; // TODO: use length squared and recalculate move points
			if(debugMode > 1) Debug.Log(this + " moving " + deltaPos);
			if (deltaPos >= MinDeltaToMove) // character has moved
			{
				//agent.DropOnGround(); // to avoid overlapping obstacles
				startTime = Time.time; // reset timeout
			}

			if (GameTurnManager.Instance.IsTurnBasedMode())
				_selfCharacter.ConsumeMovePoints(deltaPos, true);
			else
				_selfCharacter.ConsumeMovePoints(0, true);
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
		if (!_selfCharacter.HasActions())
		{
			_selfCharacter.FloatingText_Cant();
			_selfCharacter.SetItemToUse(null);
			yield break;
		}

		if (itemToUse.TurnBasedOnly() && !GameTurnManager.Instance.IsTurnBasedMode())
		{
			_selfCharacter.FloatingText(LanguageSettings.Main.NotNow);
			_selfCharacter.SetItemToUse(null);
			yield break;
		}
		
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
					Debug.LogWarning(this + " has no inventory");
					_selfCharacter.SetItemToUse(null);
					yield break;
				}
				
				_selfCharacter.SetItemToUse(itemToUse);
				_selfCharacter.UseAction(itemToUse.GetCharacterAction(), otherCharacter);
				
				if (_selfCharacter.TryGetAnimation(out CharacterAnimation anim))
				{
					yield return anim.WaitEvents(EventType.Hit, EventType.Shoot);
				}
				
				itemToUse.UseOn(otherCharacter.gameObject, _selfCharacter.gameObject);
				itemToUse.AddQuantity(-1);

				if (itemToUse.IsEmpty() && !inventory.TryRemoveItem(itemToUse))
				{
					Debug.LogWarning(this + " item cannot be removed from inventory");
				}
				
				inventory.NotifyUsed(itemToUse, otherCharacter);
				inventory.NotifyChanged();
				
				_selfCharacter.SetItemToUse(null);
				
				if (debugMode > 0) Debug.Log(this + " WaitCharacterUseItem NotifyUsed");
			}
		}
	}

	private IEnumerator WaitCharacterUseItemAt(ItemData itemToUse, Vector3 position)
	{
		if(debugMode > 0) Debug.Log(this + " WaitCharacterUseItemAt " + itemToUse);
		debugString = "WaitCharacterUseItemAt " + itemToUse;
		
		if (!_selfCharacter.HasActions())
		{
			_selfCharacter.FloatingText_Cant();
			_selfCharacter.SetItemToUse(null);
			yield break;
		}

		if (itemToUse.TurnBasedOnly() && !GameTurnManager.Instance.IsTurnBasedMode())
		{
			_selfCharacter.FloatingText(LanguageSettings.Main.NotNow);
			_selfCharacter.SetItemToUse(null);
			yield break;
		}
		
		if (!_selfCharacter.TryGetComponent(out Inventory inventory))
		{
			Debug.LogWarning(_selfCharacter + " has no inventory");
			_selfCharacter.SetItemToUse(null);
			yield break;
		}

		while (!_selfCharacter.IsFacingPosition(position))
		{
			_selfCharacter.LookWorldPosition(position, Time.deltaTime);
			yield return null;
		}
		
		if (_selfCharacter.TryGetAnimation(out CharacterAnimation anim))
		{
			do yield return null;
			while (anim.IsSwitchingWeapons);
		}
				
		_selfCharacter.UseAction(itemToUse.GetCharacterAction(), itemToUse);

		if (anim)
		{
			anim.WaitEvents(EventType.Hit, EventType.Shoot);
			do
			{
				_selfCharacter.SetItemToUse(itemToUse);
				yield return null;
			} while (anim.IsWaitingEvents);
			if(debugMode > 0) Debug.Log(this + " item Shoot or Hit");
		}

		
		if (debugMode > 0) Debug.Log(this + " launch projectile");
		itemToUse.LaunchProjectile(_selfCharacter, position);
		itemToUse.AddQuantity(-1);
		if (itemToUse.IsEmpty() && !inventory.TryRemoveItem(itemToUse))
		{
			Debug.LogWarning("item cannot be removed from inventory");
		}

		inventory.NotifyUsed(itemToUse, position);
		inventory.NotifyChanged();
		
		yield return new WaitForSeconds(DefaultWaitSeconds); // to end anim?
		_selfCharacter.SetItemToUse(null);
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
				if (!selfInventory.TryTransfertItemTo(draggedItem, partyInventory))
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
				if (!selfInventory.TryCreateLootBag(draggedItem, worldPosition, out var lootBag))
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
	
	private IEnumerator WaitForNavigation()
	{
		do yield return null;
		while (NavmeshRebuilder.IsNavigationDirty());
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

		if (!_selfCharacter.TryGetComponent(out Inventory selfInventory))
		{
			Debug.LogWarning(this + " has no inventory to reload weapon");
			yield break;
		}

		if (!weapon.CanBeReloadedWithAmmo(ammoItem))
		{
			_selfCharacter.FloatingText_Cant(); // wrong ammo
			yield break;
		}

		if (!_selfCharacter.HasActions())
		{
			_selfCharacter.FloatingText_Cant(); // no action to reload
			yield break;
		}
		
		_selfCharacter.SetAiming(false);
		_selfCharacter.SetCurrentWeapon(weapon);
		_selfCharacter.TryGetAnimation(out var anim);
		
		if (anim)
		{
			anim.SwitchWeapons(true);
			do yield return null;
			while (anim.IsSwitchingWeapons);
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
		
		_selfCharacter.FloatingText(LanguageSettings.Main.Reload);

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