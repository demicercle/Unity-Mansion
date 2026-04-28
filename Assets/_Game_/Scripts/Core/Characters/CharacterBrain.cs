using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public interface ICharacterBrain
{
	void StartThink();
	void StopThink();
}

public class CharacterBrain : MonoBehaviour, ICharacterBrain
{
	private const float WanderingSearchTimeout = 6.0f;
	private const float SearchWaypointSeconds = 0.5f;
	private const float ThinkingTimeout = 10.0f;
	private const float AggroDistanceScale = 0.9f;
	private const float MaxAggro = 10.0f;
	private const float DefaultWaitSeconds = 0.25f;
	private const float MinDistanceToDetectMovement = 0.20f;
	
	public static int debugMode { get; set; }
	
	[SerializeField] private bool canChase = true;
	[SerializeField] private bool canPatrol = true;
	//[SerializeField] [Min(0f)] private float defaultWait = 1.5f;
	[SerializeField] private Waypoint wanderingPoint;
	private bool canUseRunAction => GameTurnManager.Instance.IsTurnBasedMode();

	public UnityEvent onStartThink;
	
	public bool IsThinking { get; private set; }
	
	public void StopThink()
	{
		if (!IsThinking) 
			return;
		
		if (debugMode > 0) Debug.Log(this + " StopThink");
		
		StopAllCoroutines();
		IsThinking = false;
	}
	
	public void StartThink()
	{
		if (debugMode > 0) Debug.Log(this + " StartThink ; wasThinking=" + IsThinking);
		
		StopAllCoroutines();
		StartCoroutine(Think());
		StartCoroutine(ThinkTimeoutCoroutine());
	}

	public void AssignWanderingPoint(Waypoint newWp)
	{
		wanderingPoint = newWp;
	}

	public bool HasWanderingPoint(Waypoint wp) => wanderingPoint == wp;

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
		if(debugMode > 0) Debug.Log(this + " ends");

		IsThinking = false;
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
			
			if (_selfCharacter.IsMoving() || NavUtils.NavDistanceRoundY(_lastPosition, _selfCharacter.GetPosition()) >= MinDistanceToDetectMovement)
				_startedThinkingTime = Time.time; // reset timeout
			
			yield return null;
			
			_lastPosition = _selfCharacter.GetPosition();
		}
	}

	private IEnumerator TryChase()
	{
		if (!_selfCharacter.CanAttack())
		{
			if(debugMode > 0) Debug.Log(this + " can't attack");
			yield return StartCoroutine(YieldCantAttack());
			yield break;
		}

		yield return StartCoroutine(YieldTryFindNewTarget());
		
		while (_chaseCharacter && _selfCharacter.CanAttackOther(_chaseCharacter))
		{
			if (!_selfCharacter.CanAttack())
			{
				yield return StartCoroutine(YieldCantAttack());
				yield break;
			}
		
			if (_chaseCharacter.IsDead())
			{
				ForgetTarget();
				if (!TryFindTarget())
					yield break;
			}
		
			if (!_characterDetection.IsCharacterDetected(_chaseCharacter))
			{
				ForgetTarget();
				if (!TryFindTarget())
					yield break;
			}
			
			yield return StartCoroutine(YieldTryFindNewTarget());
			
			if(debugMode > 0) Debug.Log(this + " chase " + _chaseCharacter);
				
			_selfCharacter.Routines.AssignAttack(_chaseCharacter, true, _selfCharacter.IsUsingRangeWeapon());
				
			do
			{
				SaveTargetPosition();
				
				yield return null;
				
			} while (CharacterRoutines.AnyActiveRoutines());
			
			yield return null;
		}

		if (!_chaseCharacter)
		{
			if(debugMode > 1) Debug.Log(this + " !_chaseCharacter");
		}
	}

	private IEnumerator TryPatrol()
	{
		if (!_selfCharacter.HasMovePoints())
		{
			if(debugMode > 0) Debug.Log(this + " TryPatrol !HasMovePoints");
			yield break;
		}
		
		if (!_selfCharacter.HasActions())
		{
			if(debugMode > 0) Debug.Log(this + " TryPatrol !HasActions");
			yield break;
		}
		
		if (!_patrolRoute)
		{
			if(!PatrolRoute.FindRoute(_selfCharacter, out var newPatrol))
				yield break;
			
			_patrolRoute = newPatrol;
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
			_currentWaypoint = _waypointList.First();
		
		_selfCharacter.Routines.AssignMoveToWaypoint(_currentWaypoint, canUseRunAction);
		
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
		if (!_selfCharacter.HasMovePoints() || !_selfCharacter.HasActions())
			yield break;

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
		_selfCharacter.Routines.AssignMoveToDestination(_wanderingPos);
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

	private IEnumerator YieldCantAttack()
	{
		if(debugMode > 0) Debug.Log(this + " Yield CantAttack");
		yield return new WaitForSeconds(DefaultWaitSeconds);
	}

	private IEnumerator YieldTryFindNewTarget()
	{
		if (TryFindNewTarget(out var newTarget))
		{
			_chaseCharacter = newTarget;
			Debug.Log(this + " gets new target: " + _chaseCharacter);
			yield return CharacterRoutines.DoLookPositionOn(_selfCharacter, newTarget.GetPosition());
		}
	}
	
	private CharacterEntity _selfCharacter;
	private CharacterDetection _characterDetection;
	private CharacterEntity _chaseCharacter;
	private PatrolRoute _patrolRoute;
	private List<Waypoint> _waypointList;
	private Waypoint _currentWaypoint;
	private Vector3 _lastTargetKnownPosition;
	private Vector3 _wanderingPos, _lastWanderingPos;
	private Dictionary<CharacterEntity, float> _aggroMap = new Dictionary<CharacterEntity, float>();
	private float _startedThinkingTime;
	private Vector3 _lastPosition;
	
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

	private bool TryFindNewTarget(out CharacterEntity newTarget)
	{
		newTarget = _aggroMap.Keys.OrderBy(other =>
		{
			var dist = Vector3.Distance(other.GetPosition(), _selfCharacter.GetPosition());
			var aggro = _aggroMap[other];
			return aggro - dist * AggroDistanceScale;
		}).LastOrDefault();

		return newTarget && newTarget != _chaseCharacter;
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

	private bool TryFindTarget()
	{
		if (_characterDetection)
		{
			foreach (var otherCharacter in _characterDetection.GetDetectedCharacters())
			{
				OnCharacterDetected(otherCharacter);
				
				if (_chaseCharacter == otherCharacter)
					return true;
			}
		}
		
		return false;
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
		}
	}

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

			if (_aggroMap.Count > 0)
			{
				debugString = "";
				
				foreach (var other in _aggroMap.Keys)
				{
					var dist = Vector3.Distance(other.GetPosition(), _selfCharacter.GetPosition());
					var totalAggro = _aggroMap[other] - dist * AggroDistanceScale;
					debugString += (_chaseCharacter == other ? ">" : " ") + totalAggro.ToString("N2") + " " + other.name + "\n";
				}

				Utils.DrawWorldGUI(_selfCharacter.GetPosition(), debugString.Trim('\n'));
			}
		}
	}

	private void OnDrawGizmos()
	{
		if (wanderingPoint)
		{
			float wanderingDistance = wanderingPoint.radius;
			Gizmos.color = Color.blue;
			wanderingPoint.color = Gizmos.color;
			Gizmos.DrawLine(transform.position, wanderingPoint.GetPosition());
			Utils.DrawGizmosCircle(wanderingPoint.GetPosition(), Vector3.up, wanderingDistance);
		}
	}
}