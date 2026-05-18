using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class CharacterBrain : MonoBehaviour
{
	private const float WanderingSearchTimeout = 6.0f;
	private const float SearchWaypointSeconds = 0.5f;
	private const float ThinkingTimeout = 10.0f;
	private const float AggroHDistanceScale = 0.9f;
	private const float AggroVDistanceScale = 1.9f;
	private const float MaxAggro = 10.0f;
	private const float DefaultWaitSeconds = 0.25f;
	private const float MinDistanceToDetectMovement = 0.20f;
	
	public static int debugMode { get; set; }
	
	[SerializeField] private bool canChase = true;
	[SerializeField] private bool canPatrol = true;
	[SerializeField] private bool turnHostileWhenDamaged = false;
	[SerializeField] private bool canUseRunAction = true;
	[SerializeField] private bool canUseDefendAction = false;
	[SerializeField] private bool canUseRiposteAction = false;
	[SerializeField] private float aggroAlliesModifier = 0.8f;
	[SerializeField] private Waypoint wanderingPoint;

	public UnityEvent onStartThink;
	
	public bool IsThinking { get; private set; }
	
	public void StopThink()
	{
		if (!IsThinking) 
			return;
		
		if (debugMode > 1) Debug.Log(this + " StopThink");
		
		StopAllCoroutines();
		IsThinking = false;
	}
	
	public void StartThink()
	{
		if (debugMode > 1) Debug.Log(this + " StartThink");
		
		StopAllCoroutines();
		StartCoroutine(Think());
		StartCoroutine(ThinkTimeoutCoroutine());
	}

	public void AssignWanderingPoint(Waypoint newWp)
	{
		wanderingPoint = newWp;
	}

	public bool HasWanderingPoint(Waypoint wp) => wanderingPoint == wp;
	
	#region PRIVATE
	private CharacterEntity _selfCharacter;
	private CharacterDetection _characterDetection;
	private CharacterEntity _chaseCharacter;
	private float _chaseDistance;
	private PatrolRoute _patrolRoute;
	private List<Waypoint> _waypointList;
	private Waypoint _currentWaypoint;
	private Vector3 _lastTargetKnownPosition;
	private Vector3 _wanderingPos, _lastWanderingPos;
	private Dictionary<CharacterEntity, float> _aggroMap = new Dictionary<CharacterEntity, float>();
	private float _startedThinkingTime;
	private Vector3 _lastPosition;
	private CharacterAgent _agent;
	
	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
		if (!_selfCharacter)
		{
			Debug.LogWarning(this + " has no character controller!");
			enabled = false;
			return;
		}

		_selfCharacter.onPartyIndexChanged += OnPartyIndexChanged;
		_selfCharacter.onDead += OnCharacterDead;
		_selfCharacter.onDamagedBy += OnDamagedBy;
		
		_waypointList = new List<Waypoint>();
		_agent = GetComponent<CharacterAgent>();
	}

	private void OnDestroy()
	{
		_selfCharacter.onPartyIndexChanged -= OnPartyIndexChanged;
		_selfCharacter.onDead -= OnCharacterDead;
		_selfCharacter.onDamagedBy -= OnDamagedBy;
	}

	private void OnEnable()
	{
		if (!_characterDetection) _characterDetection = GetComponent<CharacterDetection>();
		if (_characterDetection)
		{
			_characterDetection.onCharacterDetected += OnCharacterDetected;
			_characterDetection.onCharacterLost += OnCharacterLost;
		}
	}

	private void OnDisable()
	{
		if (_characterDetection)
		{
			_characterDetection.onCharacterDetected -= OnCharacterDetected;
			_characterDetection.onCharacterLost -= OnCharacterLost;
		}
	}

	private IEnumerator Think()
	{
		IsThinking = true;
		
		if (debugMode > 0) Debug.Log(this + " Thinking...");
		yield return new WaitForSeconds(DefaultWaitSeconds);
		
		onStartThink?.Invoke();
		
		// try chase
		if (canChase)
			yield return StartCoroutine(TryChase());
		
		// try patrol
		if (canPatrol)
			yield return StartCoroutine(TryPatrol());

		if (wanderingPoint)
			yield return StartCoroutine(TryWander());
		
		// fallback
		
		yield return new WaitForSeconds(DefaultWaitSeconds);
		if(debugMode > 1) Debug.Log(this + " ends");

		IsThinking = false;
	}

	private void ResetTimeout()
	{
		_startedThinkingTime = Time.time;
	}

	private IEnumerator ThinkTimeoutCoroutine()
	{
		_startedThinkingTime = Time.time;
		_lastPosition = _selfCharacter.GetPosition();
		
		while (IsThinking)
		{
			if (Time.time - _startedThinkingTime > ThinkingTimeout)
			{
				Debug.LogWarning(this + " Think Timeout!");
				_selfCharacter.FloatingText_Cant();
				_selfCharacter.Routines.StopAllRoutines();
				StopThink();
				yield break;
			}
			
			if (_selfCharacter.IsMoving() || NavUtils.NavDistance(_lastPosition, _selfCharacter.GetPosition()) >= MinDistanceToDetectMovement)
				_startedThinkingTime = Time.time; // reset timeout
			
			yield return null;
			
			_lastPosition = _selfCharacter.GetPosition();
		}
	}

	private IEnumerator TryChase()
	{
		if (GameTurnManager.Instance.IsTurnBasedMode() == false)
		{
			//if(debugMode > 0) Debug.Log(this + " TryChase !IsTurnBasedMode");
			yield break;
		}
		
		ResetTimeout();
		while (_selfCharacter.HasActions())
		{
			if (_selfCharacter.WeaponNeedsReload())
			{
				if (debugMode > 0) Debug.Log(this + " weapon needs reload");
				if (_selfCharacter.TryFindAmmo(out var ammoItem))
				{
					_selfCharacter.Routines.AssignReloadWeapon(_selfCharacter.GetCurrentWeapon(), ammoItem);
				}
				else
				{
					if(debugMode > 0) Debug.Log(this + " can't reload weapon, switching default weapon");
					_selfCharacter.FloatingText_Cant();
					yield return new WaitForSeconds(0.5f);
					_selfCharacter.SwitchDefaultWeapon();

					if (_selfCharacter.TryGetAnimation(out var anim))
					{
						do yield return null;
						while (anim.IsSwitchingWeapons);
					}
				}
				
				do yield return null;
				while (_selfCharacter.Routines.AnyActive());
				
				if(!_selfCharacter.HasActions())
				{
					if (debugMode > 0) Debug.Log(this + " no actions after reload, abort");
					StopThink();
					yield break;
				}
			}
			
			if (_chaseCharacter && _chaseCharacter.IsDead())
			{
				if (debugMode > 0) Debug.Log(this + " forget dead target:" + _chaseCharacter);
				ForgetTarget();
			}
			
			yield return StartCoroutine(YieldTryFindNewTarget());

			if (!_chaseCharacter)
			{
				if (debugMode > 0) Debug.Log(this + " has no target to chase");
				yield break;
			}
			
			if (debugMode > 0) Debug.Log(this + " chase " + _chaseCharacter);
			SaveTargetPosition();
			
			if (!_selfCharacter.HasMovePoints())
			{
				ThinkNoMovePoints();
				
				do yield return null;
				while (_selfCharacter.Routines.AnyActive());
			}
				
			if (_selfCharacter.IsAtAttackRange(_chaseCharacter, true))
			{
				_selfCharacter.Routines.AssignPerformAttack(_chaseCharacter);
				
				do yield return null;
				while (_selfCharacter.Routines.AnyActive());
			}
			else if (_selfCharacter.HasMovePoints())
			{
				_selfCharacter.Routines.AssignMoveAndAttack(_chaseCharacter, true, GameTurnManager.Instance.IsTurnBasedMode());
				
				do yield return null;
				while (_selfCharacter.Routines.AnyActive());
			}
		}

		if (debugMode > 0) Debug.Log(this + " end chase");
	}

	private void ThinkNoMovePoints()
	{
		var dist = _selfCharacter.GetDistanceTo(_chaseCharacter.GetPosition());
		if (dist < _selfCharacter.GetMaxMovePoints())
		{
			if (canUseDefendAction && _selfCharacter.CanUseAction(CharacterAction.Defend)
			    && canUseRiposteAction && _selfCharacter.CanUseAction(CharacterAction.Riposte))
			{
				_selfCharacter.Routines.AssignUseAction(DicePool.RollDice() < _selfCharacter.GetCurrentHealth() ? CharacterAction.Riposte : CharacterAction.Defend);
				return;
			}
			
			if (canUseDefendAction && _selfCharacter.CanUseAction(CharacterAction.Defend))
			{
				_selfCharacter.Routines.AssignUseAction(CharacterAction.Defend);
				return;
			}
			
			if (canUseRiposteAction && _selfCharacter.CanUseAction(CharacterAction.Riposte))
			{
				_selfCharacter.Routines.AssignUseAction(CharacterAction.Riposte);
				return;
			}
		}
				
		if (canUseRunAction && _selfCharacter.CanUseAction(CharacterAction.Run))
		{
			_selfCharacter.Routines.AssignUseAction(CharacterAction.Run);
		}
	}
	
	private IEnumerator TryPatrol()
	{
		if (!_selfCharacter.HasMovePoints())
		{
			if(debugMode > 0) Debug.Log(this + " TryPatrol !HasMovePoints");
			yield break;
		}

		if (_selfCharacter.HasDetectedAnyPartyCharacter() && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " TryPatrol !HasActions");
			yield break;
		}

		if (_patrolRoute && !_patrolRoute.IsAssignedTo(_selfCharacter))
		{
			if (debugMode > 0) Debug.Log(this + " patrol has changed");
			_patrolRoute = null;
		}
		
		if (!_patrolRoute && PatrolRoute.FindRoute(_selfCharacter, out var newPatrol))
		{
			_patrolRoute = newPatrol;
			_currentWaypoint = null;
			_waypointList.Clear();
			if (debugMode > 0) Debug.Log(this + " new patrol: " + _patrolRoute);
		}

		if (!_patrolRoute)
		{
			//if (debugMode > 0) Debug.Log(this + " TryPatrol !_patrolRoute");
			yield break;
		}

		if (_currentWaypoint && _selfCharacter.WaypointReached(_currentWaypoint))
		{
			_waypointList.Remove(_currentWaypoint);
			_currentWaypoint = null;
		}

		if (_waypointList.Count == 0)
		{
			_waypointList.AddRange(_patrolRoute.GetWaypoints());
			if (_waypointList.Count == 0)
			{
				Debug.LogWarning(this + " has empty patrol route " + _patrolRoute);
				yield break;
			}
		}

		if (!_currentWaypoint)
		{
			_currentWaypoint = _waypointList.First();
			if (debugMode > 0) Debug.Log(this + " new waypoint " + _currentWaypoint);
		}
		
		_selfCharacter.Routines.AssignMoveToWaypoint(_currentWaypoint);
		
		do
		{
			if (_chaseCharacter )
			{
				if(debugMode > 0) Debug.Log(this + " breaks patrol to chase");
				_selfCharacter.StopAgent();
				yield return StartCoroutine(TryChase());
				break;
			}
			
			yield return new WaitForSeconds(DefaultWaitSeconds);
		} while (CharacterRoutines.AnyActiveRoutines());
	}

	private IEnumerator TryWander()
	{
		if (!_selfCharacter.HasMovePoints())
		{
			if(debugMode > 0) Debug.Log(this + " TryWander !HasMovePoints");
			yield break;
		}

		if (_selfCharacter.HasDetectedAnyPartyCharacter() && !_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " TryWander !HasActions");
			yield break;
		}

		float startTime = Time.time;
		float wanderingDistance = wanderingPoint.radius;
		_lastWanderingPos = _wanderingPos;
		while (!NavUtils.TryGetRandomPointOnNavmesh(wanderingPoint.GetPosition(), wanderingDistance,
			       _selfCharacter.GetRadius(), out _wanderingPos)
		       || Vector3.Distance(_wanderingPos, _selfCharacter.GetPosition()) < wanderingDistance / 2f
		       || Vector3.Distance(_wanderingPos, _lastWanderingPos) < wanderingDistance / 2f)
		{
			yield return new WaitForSeconds(SearchWaypointSeconds);
			if (Time.time - startTime > WanderingSearchTimeout)
			{
				Debug.LogWarning(this + " wandering Timeout!");
				yield break;
			}
		}

		_lastWanderingPos = _wanderingPos;
		_selfCharacter.Routines.AssignMoveToDestination(_wanderingPos, _selfCharacter.GetAgent().GetMinReachDistance());
		do
		{
			if (_chaseCharacter && canChase)
			{
				if(debugMode > 0) Debug.Log(this + " stops wandering because _chaseCharacter=" + _chaseCharacter);
				break;
			}
			
			if(debugMode > 0) DebugDrawer.Line(_selfCharacter.GetCenterPosition(), _wanderingPos, Color.blue);
			if(debugMode > 0) DebugDrawer.WireDisc(_selfCharacter.GetCenterPosition(), _wanderingPos, _selfCharacter.GetRadius(), Color.blue);
			
			yield return null;
		} while (_selfCharacter.Routines.AnyActive());

		yield return new WaitForSeconds(DefaultWaitSeconds);
	}

	private const int MaxAttemptToFindTarget = 6;
	private IEnumerator YieldTryFindNewTarget(int attempt = 0)
	{
		if (attempt >= MaxAttemptToFindTarget)
		{
			if(debugMode > 0) Debug.Log(this + " cannot find any target");
			ForgetTarget();
			yield break;
		}

		//if(attempt == 0)
		//	ResetTimeout();
		
		var bestTarget = default(CharacterEntity);
		var bestPath = 0f;
		var allTargets = _aggroMap.Keys.OrderBy(CalculateAggro).ToList();
		while(allTargets.Count > 0)
		{
			var newTarget = allTargets.LastOrDefault();
			allTargets.Remove(newTarget);
			if (!newTarget || newTarget.IsDead()) continue;

			if (_selfCharacter.IsAtAttackRange(newTarget, true))
			{
				if (debugMode > 0) Debug.Log(this + " at attack range of " + newTarget + ", chase this");
				bestTarget = newTarget;
				break;
			}
			
			_agent.StopMovement();
			_agent.ClearDestination();
			_agent.SetReachDistance(_selfCharacter.GetAttackRange() + attempt * _selfCharacter.GetRadius() * 2f);
			if (debugMode > 0) Debug.Log(this + " search path to possible target " + newTarget + " attempt=" + attempt + " reachDistance=" + _agent.GetReachDistance());
			yield return _agent.AssignDestination(newTarget.GetPosition());
			if (_agent.hasDestination)
			{
				var currentPath = _agent.GetCurrentPath();
				if (currentPath.Length > 0 &&
				    Mathf.Abs(currentPath.Last().y - newTarget.GetPosition().y) > _agent.GetHeight())
				{
					if (debugMode > 0) Debug.Log(this + " path height is invalid to " + newTarget);
					continue;
				}
				
				var pathLength = Utils.PathLength(currentPath);
				if (debugMode > 0) Debug.Log(this + " found path length=" + pathLength);
				if (!bestTarget || pathLength < bestPath)
				{
					bestTarget = newTarget;
					bestPath = pathLength;
				}
			}
			else
			{
				if(debugMode > 0) Debug.Log(this + " cannot find destination to " + newTarget + ", pick next one: " + allTargets.Count);
			}
		}

		if (!bestTarget)
		{
			if (debugMode > 0) Debug.Log(this + " cannot find destination best target, next attempt");
			yield return StartCoroutine(YieldTryFindNewTarget(attempt + 1));
		}
		else
		{
			_chaseCharacter = bestTarget;
			_chaseDistance = _agent.GetReachDistance();
			if(debugMode > 0) Debug.Log(this + " gets new target: " + _chaseCharacter);
			if(debugMode > 0) DebugDrawer.Line(_selfCharacter.GetEyesPosition(), _chaseCharacter.GetCenterPosition(), Color.red, 1.0f);
			while (!_selfCharacter.IsFacingPosition(_chaseCharacter.GetPosition()))
			{
				_selfCharacter.LookWorldPosition(_chaseCharacter.GetPosition(), Time.deltaTime);
				yield return null;
			}
		}
	}

	private float CalculateAggro(CharacterEntity other)
	{
		var maxDist = _selfCharacter.GetMaxVisionRange();
		var hDist = Utils.HDistance(_selfCharacter.GetPosition(), other.GetPosition());
		var vDist = Utils.VDistance(_selfCharacter.GetPosition(), other.GetPosition());
		if (hDist + vDist > maxDist * 2)
			return -Mathf.Infinity;

		hDist *= CountAlliesChasing(other) * aggroAlliesModifier + 1;
			
		var aggro = _aggroMap[other];
		return aggro - hDist * AggroHDistanceScale - vDist * AggroVDistanceScale;
	}

	private int CountAlliesChasing(CharacterEntity otherChar)
	{
		return CharacterEntity.AllCharacters.Count(allyChar =>
		{
			return allyChar != otherChar 
			       && allyChar != _selfCharacter 
			       && allyChar.IsCharacterRevealed()
			       && !allyChar.IsDead() 
			       && _selfCharacter.IsAllyTo(allyChar) 
			       && allyChar.TryGetComponent(out CharacterBrain brain)
			       && brain.canChase
			       && brain._chaseCharacter == otherChar;
		});
	}

	private void SaveTargetPosition()
	{
		if (!_chaseCharacter)
		{
			Debug.LogWarning(this + " SaveTargetPosition() no target");
			return;
		}
		
		_lastTargetKnownPosition = _chaseCharacter.GetPosition();
		if(debugMode > 0) DebugDrawer.WireDisc(_lastTargetKnownPosition, _chaseCharacter.GetRadius(), Color.red, DefaultWaitSeconds);
	}

	private void ForgetTarget()
	{
		if (!_chaseCharacter)
		{
			Debug.LogWarning(this + " ForgetTarget() no target");
			return;
		}
		
		_aggroMap.Remove(_chaseCharacter);

		if(debugMode > 0) Debug.Log(this + " ForgetTarget " + _chaseCharacter);
		_chaseCharacter = null;
	}

	private void OnPartyIndexChanged(int index, int _)
	{
		enabled = index < 0; // not in party
	}

	private void OnCharacterDead()
	{
		ForgetTarget();
		StopThink();
		_aggroMap.Clear();
	}

	private void OnDamagedBy(CharacterEntity other, float amount)
	{
		if(debugMode > 0) Debug.Log(this + " OnDamagedBy " + other + " amount="  + amount);

		if (turnHostileWhenDamaged && amount > 0 && other.IsPlayer())
		{
			_selfCharacter.SetCurrentFaction(CharacterFaction.Enemy);
		}
		
		AddAggro(other, amount);
	}

	private void AddAggro(CharacterEntity other, float amount)
	{
		if (!_aggroMap.ContainsKey(other))
			_aggroMap.Add(other, amount);
		else
			_aggroMap[other] += amount;

		_aggroMap[other] = Mathf.Min(_aggroMap[other], MaxAggro);
	}

	private void OnCharacterDetected(CharacterEntity otherCharacter)
	{
		if(_selfCharacter.IsEnemyTo(otherCharacter))
			AddAggro(otherCharacter, 0f);
		
		if (!_chaseCharacter && canChase && _selfCharacter.CanAttackOther(otherCharacter))
		{
			_chaseCharacter = otherCharacter;
			SaveTargetPosition();
			if(debugMode > 0) Debug.Log(this + " new _chaseCharacter=" + _chaseCharacter);
		}
	}

	private void OnCharacterLost(CharacterEntity otherCharacter) // TODO move to last known position?
	{
		/*if (looseTargetDistance > 0)
		{
			if (_targetDetected && _chaseCharacter == otherCharacter)
			{
				if(debugMode > 0) Debug.Log(this + " looses target " + _chaseCharacter);
				SaveTargetPosition();
				_targetDetected = false;
			}
		}*/
	}

	private void LateUpdate()
	{
		if (debugMode > 0)
		{
			if (_chaseCharacter)
			{
				DebugDrawer.Line(_selfCharacter.GetEyesPosition(), _chaseCharacter.GetCenterPosition(), Color.red);
				DebugDrawer.WireDisc(_chaseCharacter.GetPosition(), _chaseCharacter.transform.forward, _chaseCharacter.GetRadius(), Color.red);
			}
			
			debugString = "";
			
			if (_aggroMap.Count > 0)
			{
				foreach (var other in _aggroMap.Keys)
				{
					var totalAggro = CalculateAggro(other);
					debugString += (_chaseCharacter == other ? ">" : " ") + totalAggro.ToString("N2") + " " + other.name + "\n";
				}
			}
		}
	}
	#endregion

	private string debugString;
	private void OnGUI()
	{
		if (debugMode > 0)
		{
			if (IsThinking)
				Utils.DrawWorldGUI(_selfCharacter.GetPosition() + Vector3.up * _selfCharacter.GetHeight(), 
					"Thinks... (" + (Time.time - _startedThinkingTime).ToString("N2") + ")");
			
			if(_chaseCharacter)
				Utils.DrawWorldGUI((Vector3)_lastTargetKnownPosition, _chaseCharacter.name + " chased by " + name);

			if (!string.IsNullOrEmpty(debugString))
			{
				Utils.DrawWorldGUI(_selfCharacter.GetPosition(), debugString.Trim('\n'));
			}
		}
	}
	
	private void OnDrawGizmos()
	{
		if (wanderingPoint )
		{
			float wanderingDistance = wanderingPoint.radius;
			Gizmos.color = Color.blue;
			wanderingPoint.color = Gizmos.color;
			Gizmos.DrawLine(transform.position, wanderingPoint.GetPosition());
			Utils.DrawGizmosCircle(wanderingPoint.GetPosition(), Vector3.up, wanderingDistance);
		}
	}
}