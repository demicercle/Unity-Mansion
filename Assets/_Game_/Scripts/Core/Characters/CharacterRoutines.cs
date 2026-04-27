using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EventType = CharacterAnimation.EventType;

public class CharacterRoutines : MonoBehaviour
{
	public const float SearchPathTimeout = 6.0f;
	public const float DefaultWaitSeconds = 1.0f;
	public const float MinReachDistance = 0.1f;
	public const float MinDeltaToMove = 0.1f;
	
	public static int debugMode { get; set; }
	public static List<CharacterRoutines> activeInstances = new List<CharacterRoutines>();
	
	public static bool AnyActiveRoutines() => activeInstances.Count > 0;
	
	public static Coroutine DoLookPositionOn(CharacterEntity character, Vector3 lookPosition)
	{
		return character.StartCoroutine(character.Routines.WaitCharacterLookPosition(lookPosition));
	}

	public System.Action OnStarted;
	
	public bool AnyActive()
	{
		return activeInstances.Contains(this);
	}
	
	public void AssignAttack(IAttackable target, bool canUseRunAction, bool losCheck)
	{
		if(debugMode > 0) Debug.Log(this + " AssignAttack " + target + " canUseRunAction=" + canUseRunAction + " losCheck=" + losCheck);
		StartNewRoutine(WaitCharacterAttack(target, canUseRunAction, losCheck));
	}
	public void AssignAttackInstant(IAttackable target, bool useAction)
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
	public void AssignMoveToWaypoint(Waypoint waypoint, bool canUseRunAction, System.Action onComplete = null)
	{
		if(debugMode > 0) Debug.Log(this + " AssignMoveToWaypoint " + waypoint + " canUseRunAction=" + canUseRunAction);
		StartNewRoutine(WaitMoveCharacterToWaypoint(waypoint, canUseRunAction, onComplete));
	}
	public void AssignRunAction()
	{
		if(debugMode > 0) Debug.Log(this + " AssignRunAction ");
		StartNewRoutine(WaitAction(CharacterAction.Run));
	}
	public void AssignLookPosition(Vector3 lookPosition)
	{
		if(debugMode > 0) Debug.Log(this + " AssignLookPosition ");
		if (debugMode > 0) DebugDrawer.Line(_selfCharacter.GetEyesPosition(), lookPosition, Color.magenta, 1.0f);//TODO defaultDrawDuration
		StartNewRoutine(WaitCharacterLookPosition(lookPosition));
	}
	public void AssignTalk(CharacterEntity npc) // TODO all " Assign " methods should return the coroutine
	{
		if(debugMode > 0) Debug.Log(this + " AssignTalk " + npc);
		StartCoroutine(WaitCharacterTalk(npc));
	}
	public void AssignUseItem(ItemData itemToUse, object objectTo)
	{
		if(debugMode > 0) Debug.Log(this + " AssignUseItem " + itemToUse + " on " + objectTo);
		StartCoroutine(WaitCharacterUseItem(itemToUse, objectTo));
	}

	// leader assign
	public void Leader_AssignMoveToDestination(Vector3 navPosition)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignMoveToDestination ");
		foreach (var partyCharacter in CharacterEntity.GetPartyCharacters())
		{
			if (partyCharacter == _selfCharacter)
			{
				partyCharacter.Routines.AssignMoveToDestination(navPosition);
			}
			else
			{
				partyCharacter.Routines.AssignFollows(_selfCharacter);
			}
		}
	}
	public void Leader_AssignInteract(PropComponent clickedProp)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignInteract ");
		foreach (var partyCharacter in CharacterEntity.GetPartyCharacters())
		{
			if (partyCharacter == _selfCharacter)
			{
				partyCharacter.Routines.AssignInteract(clickedProp);
			}
			else
			{
				partyCharacter.Routines.AssignFollows(_selfCharacter);
			}
		}
	}
	public void Leader_AssignAttack(IAttackable other, bool canUseRunAction)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignAttack ");
		foreach (var partyCharacter in CharacterEntity.GetPartyCharacters())
		{
			if (partyCharacter == _selfCharacter)
			{
				partyCharacter.Routines.AssignAttack(other, canUseRunAction, partyCharacter.IsUsingRangeWeapon());
			}
			else
			{
				partyCharacter.Routines.AssignFollows(_selfCharacter);
			}
		}
	}
	public void Leader_AssignTalk(CharacterEntity npc)
	{
		if(debugMode > 0) Debug.Log(this + " Leader_AssignTalk ");
		foreach (var partyCharacter in CharacterEntity.GetPartyCharacters())
		{
			if (partyCharacter == _selfCharacter)
			{
				partyCharacter.Routines.AssignTalk(npc);
			}
			else
			{
				partyCharacter.Routines.AssignFollows(_selfCharacter);
			}
		}
	}
	public void AssignFollows(CharacterEntity leader)
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
	private CharacterEntity _selfCharacter;

	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
		if(!_selfCharacter) Debug.LogError(this + " absolutely needs a character!");
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
	
	private IEnumerator WaitCharacterInteract(PropComponent clickedProp)
	{
		debugString = "WaitCharacterInteract";
		if(debugMode > 0) Debug.Log(_selfCharacter + " WaitCharacterInteract " + clickedProp);
		
		var agent = _selfCharacter.GetAgent();
		agent.StopMovement(); // force agent to stop while calculating destination path
		
		yield return agent.AssignWaypointDestinationAsync(clickedProp);

		var destination = clickedProp.GetPosition();
		var reachRadius = clickedProp.GetRadius();
		var direction = (_selfCharacter.GetPosition() - clickedProp.GetPosition()).normalized;
		var foundWaypoint = agent.GetBestWaypoint();
		if (foundWaypoint)
		{
			if(debugMode > 0) Debug.Log(_selfCharacter + " waypoint found: " + foundWaypoint + ", move to");
			agent.SetReachDistance(foundWaypoint.radius);
			destination = foundWaypoint.GetPosition();
			direction = foundWaypoint.GetDirection();
		}

		var startTime = Time.time;
		var pathValid = false;
		do
		{
			if (Time.time - startTime > SearchPathTimeout)
			{
				Debug.LogWarning(this + " search path to waypoint timeout");
				yield break;
			}
			
			yield return agent.SearchPathAsync(destination, false);

			var path = agent.GetCurrentPath();
			pathValid = path.Length > 1 && NavUtils.NavDistanceRoundY(path.Last(), destination) <= reachRadius + agent.GetRadius();
			
		} while (!pathValid);

		yield return StartCoroutine(WaitMoveLastPath(destination));
		
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
			var dist = NavUtils.NavDistanceRoundY(_selfCharacter.GetPosition(), destination);
			Debug.Log(this + " not at reach: " + dist + " / " + agent.GetMinReachDistance() + " to " + destination);
		}
	}
	
	private IEnumerator WaitCharacterAttack(IAttackable target, bool canUseRunAction, bool losCheck)
	{
		if(debugMode > 0) Debug.Log("WaitCharacterAttackCharacter " + _selfCharacter + " " + target + " canUseRunAction=" + canUseRunAction + " losCheck=" + losCheck);

		if (_selfCharacter.TryGetAnimation(out var characterAnim))
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

			atAttackRange = _selfCharacter.IsAtAttackRange(target);
			
			if (atAttackRange && losCheck)
			{
				if (!_selfCharacter.LOSCheck(target))
				{
					if (debugMode > 0) Debug.Log(_selfCharacter + " los obstructed, try to find better position");
					_selfCharacter.FloatingText_Cant();
					yield break;
				}
			}
			
			if (atAttackRange)
			{
				if(debugMode > 0) Debug.Log(_selfCharacter + " atAttackRange of " + target + ", StopAgent");
				_selfCharacter.StopAgent();
			}
			
			else if (!_selfCharacter.IsMoving())
			{
				if (!_selfCharacter.HasMovePoints() && canUseRunAction && _selfCharacter.CanUseAction(CharacterAction.Run))
				{
					if (debugMode > 0) Debug.Log(this + " WaitCharacterAttackCharacter use Run Action");
					yield return StartCoroutine(WaitAction(CharacterAction.Run));
				}
				
				else if (!_selfCharacter.HasMovePoints())
				{
					if(debugMode > 0) Debug.Log(_selfCharacter + " has no move points to reach target");
					_selfCharacter.FloatingText_Cant();
					yield break;
				}

				else
				{
					var reachDistance = _selfCharacter.GetAttackRange(target) - MinReachDistance;
					if(debugMode > 0) Debug.Log(this + " starts moving towards target ; reachDistance=" + reachDistance);
					yield return StartCoroutine(WaitCharacterMoved(target.GetPosition(), reachDistance));
				}
			}
		}
		
		yield return StartCoroutine(WaitCharacterPerformAttack(target, true));
	}

	private IEnumerator WaitCharacterPerformAttack(IAttackable target, bool useAction)
	{
		debugString = "WaitCharacterPerformAttack";
		if(debugMode > 0) Debug.Log(this + " WaitCharacterPerformAttack " + target );
		
		yield return StartCoroutine(WaitCharacterLookPosition(target.GetPosition()));

		if (useAction && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " cannot attack (no actions)");
			_selfCharacter.FloatingText_Cant();
			yield break;
		}
		
		_selfCharacter.AttackAction(target);
		if(debugMode > 0) DebugDrawer.Line(_selfCharacter.GetEyesPosition(), target.GetPosition(), Color.red, 2.0f);
		yield return _selfCharacter.WaitAnimationEvent(_selfCharacter.IsUsingRangeWeapon() ? CharacterAnimation.EventType.Shoot : CharacterAnimation.EventType.Hit);
		//yield return new WaitForSeconds(character.GetDefaultAttackHitTime());//TODO wait attack duration
		//TODO DamageInfo, hit check (GD)
		target.TakeDamage(new DamageInfo()
		{
			amount = _selfCharacter.GetLastAttackDamage(),
			isRanged =  _selfCharacter.IsUsingRangeWeapon()
		}, _selfCharacter);

		if(debugMode > 0) Debug.Log("attack ends");
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
	
	private IEnumerator WaitMoveCharacterToWaypoint(Waypoint waypoint, bool canUseRunAction, System.Action onComplete = null)
	{
		debugString = "WaitMoveCharacterToWaypoint";
		if(debugMode > 0) Debug.Log(this + " WaitMoveCharacterToWaypoint " + waypoint + " canUseRunAction=" + canUseRunAction);
		yield return StartCoroutine(WaitCharacterMoved(waypoint.GetPosition(), waypoint.radius));
		
		if (_selfCharacter.WaypointReached(waypoint))
		{
			if(!waypoint.ignoreRotation)
				yield return StartCoroutine(WaitCharacterLookDirection(waypoint.GetDirection()));
		}
		else
		{
			if (!_selfCharacter.HasMovePoints())
			{
				if (canUseRunAction && _selfCharacter.CanUseAction(CharacterAction.Run))
				{
					if (debugMode > 0) Debug.Log(this + " WaitMoveCharacterToWaypoint use Run Action");
					yield return StartCoroutine(WaitAction(CharacterAction.Run));
					yield return StartCoroutine(WaitMoveCharacterToWaypoint(waypoint, false));//TODO: multiple run actions??
					yield break;
				}
			}
		}
		
		onComplete?.Invoke();
		
		//if(debugMode > 0) Debug.Log(this + " did not reach waypoint");
	}

	private IEnumerator WaitCharacterMoved(Vector3 dest, float reachDistance)
	{
		var currentDistance = NavUtils.NavDistanceRoundY(dest,  _selfCharacter.GetPosition());
		debugString = "WaitCharacterMoved " + reachDistance.ToString("N2");
		if(debugMode > 0) Debug.Log("WaitCharacterMoved " + _selfCharacter + " " + reachDistance + " currentDistance=" + currentDistance);
		
		if (currentDistance <= reachDistance)
		{
			if(debugMode > 0) Debug.Log("character already at reachDistance=" + reachDistance+ ", abort");
			yield break;
		}
			
		var agent = _selfCharacter.GetAgent();
		agent.StopMovement(); // stop any movement
		agent.SetReachDistance(reachDistance);
		
		yield return agent.SearchPathAsync(dest, false);
		yield return StartCoroutine(WaitMoveLastPath(dest, reachDistance));
	}

	private IEnumerator WaitMoveLastPath(Vector3 dest, float forcedReachDistance = 0f)
	{
		if (debugMode > 0) Debug.Log(this + " WaitMoveLastPath " + forcedReachDistance); 
		var agent = _selfCharacter.GetAgent();
		var path = agent.GetCurrentPath();
		if(debugMode > 0) Debug.Log(this + " move to last path: " + path.Length);
		if(debugMode > 0) DebugDrawer.Path(path, false, Color.blue, 1.0f);
		if (path.Length <= 1)
		{
			Debug.LogWarning(_selfCharacter + " path invalid\nfrom " + _selfCharacter.GetPosition() + " points: " + Utils.ArrayStr(path));;
			if(debugMode > 2) Debug.DebugBreak();
			yield return null;
			yield break;
		}

		var firstPoint = path[1];
		var dir = (firstPoint - _selfCharacter.GetPosition()).normalized;
		if (dir.sqrMagnitude < Consts.MinMagnitude)
		{
			Debug.LogWarning(this + " invalid direction to firstPoint, abort move");
			_selfCharacter.StopAgent();
			yield break;
		}
		
		while (!_selfCharacter.IsFacingDirection(dir))
		{
			_selfCharacter.LookDirection(dir, Time.deltaTime);
			yield return null;
		}
		
//		if(debugMode > 0) Debug.Log(this + " agent.StartMove ");
		agent.StartMove();

		if (forcedReachDistance > 0)
			agent.SetReachDistance(forcedReachDistance);
		
		var startTime = Time.time;
		while (!agent.ReachedDestination(dest))
		{
			debugTimeout = (Time.time - startTime);
			
			if (forcedReachDistance > 0)
				agent.SetReachDistance(forcedReachDistance);
			
			if (Time.time - startTime > SearchPathTimeout)
			{
				Debug.LogWarning(_selfCharacter + " TIMED_OUT!");
				_selfCharacter.FloatingText_Cant();
				_selfCharacter.StopAgent();
				yield break;
			}

			if (!_selfCharacter.IsMoving())
			{
				if(debugMode > 0) Debug.Log(_selfCharacter + " something stopped movement");
				yield break;
			}
			
			if (!_selfCharacter.HasMovePoints())
			{
				if(debugMode > 0) Debug.Log(_selfCharacter + " !HasMovePoints");
				_selfCharacter.FloatingText_Cant();
				agent.StopMovement();
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
				//agent.DropOnGround(); // to avoid overlapping obstacles
				startTime = Time.time; // reset timeout
			}
			
			if(debugMode > 2) Debug.Log(this + " moving " + deltaPos);
			
			_selfCharacter.ConsumeMovePoints(GameTurnManager.Instance.IsTurnBasedMode() ? deltaPos : 0f, true);
		}
		
		if(debugMode > 0) Debug.Log(this + " reached destination! " + NavUtils.NavDistanceRoundY(dest, _selfCharacter.GetPosition()));
		agent.StopMovement();
	}
	
	private IEnumerator WaitAction(CharacterAction action)
	{
		debugString = "WaitAction " + action;
		if(debugMode > 0) Debug.Log(">> WaitAction: " + this + " " + action);
		yield return new WaitForSeconds(DefaultWaitSeconds);
		_selfCharacter.UseAction(action);
		_selfCharacter.FloatingText(action.ToString());
	}

	private IEnumerator WaitCharacterTalk(CharacterEntity npc)
	{
		debugString = "WaitCharacterTalk " + npc;
		if(debugMode > 0) Debug.Log(">> WaitCharacterTalk: " + this + " " + npc);

		var talkDistance = npc.GetRadius() + _selfCharacter.GetRadius() + MinReachDistance;
		yield return StartCoroutine(WaitCharacterMoved(npc.GetPosition(),talkDistance+ MinReachDistance));

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

	private IEnumerator WaitCharacterUseItem( ItemData itemToUse,object objectTo)
	{
		var useRange = itemToUse.GetUseRange();
		if (objectTo is CharacterEntity)
		{
			var otherCharacter = objectTo as CharacterEntity;
			if (useRange > 0 && NavUtils.NavDistanceRoundY(_selfCharacter.GetPosition(), otherCharacter.GetPosition()) >
			    useRange)
			{
				if(debugMode > 0) Debug.Log(this + " WaitCharacterUseItem WaitCharacterMoved");
				yield return StartCoroutine(WaitCharacterMoved(otherCharacter.GetPosition(), useRange));
			}

			if (useRange <= 0 ||
			    NavUtils.NavDistanceRoundY(_selfCharacter.GetPosition(), otherCharacter.GetPosition()) <= useRange)
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
				
				itemToUse.UseOn(otherCharacter.gameObject);
				inventory.NotifyUsed(itemToUse, otherCharacter);
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
}