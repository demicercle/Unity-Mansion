using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;

public interface ICharacterBrain
{
	IEnumerator Think();
}

public class CharacterBrain : MonoBehaviour, ICharacterBrain
{
	public static int debugMode { get; set; }
	
	[SerializeField] private bool canChase = true;
	[SerializeField] private bool canPatrol = true;
	[SerializeField] private bool canUseRunAction = false;
	[SerializeField] private bool lookAtDetectedEnemies = true;
	[SerializeField] [Min(0f)] private float defaultWait = 1.5f;
	[SerializeField] [Min(0f)] private float looseTargetTime = 1.5f;

	public bool IsThinking { get; private set; }
	
	public void StopThinking()
	{
		StopAllCoroutines();
		IsThinking = false;
		if(debugMode > 0) Debug.Log(this + " StopThinking");
	}
	
	public void StartThink()
	{
		StopAllCoroutines();
		StartCoroutine(Think());
	}

	public IEnumerator Think()
	{
		IsThinking = true;
		
		if (debugMode > 0) Debug.Log(this + " Thinking...");
		yield return new WaitForSeconds(defaultWait);
		
		// try chase
		if (canChase)
			yield return StartCoroutine(TryChase());
		
		// try patrol
		if (canPatrol)
			yield return StartCoroutine(TryPatrol());
		
		// fallback
		
		yield return new WaitForSeconds(defaultWait);
		if(debugMode > 0) Debug.Log(this + " ends");

		IsThinking = false;
	}

	private IEnumerator TryChase()
	{
		if (!_selfCharacter.CanAttack())
		{
			yield return StartCoroutine(YieldCantAttack());
			yield break;
		}
		
		if (_chaseCharacter && _chaseCharacter.IsDead())
		{
			ForgetTarget();
			if (!TryFindTarget())
				yield break;
		}
		
		while (_chaseCharacter && _selfCharacter.CanAttackOther(_chaseCharacter))
		{
			if (!_selfCharacter.CanAttack())
			{
				yield return StartCoroutine(YieldCantAttack());
				yield break;
			}
			
			if (!_targetDetected)
			{
				if (_selfCharacter.PositionReached(_lastTargetKnownPosition))
				{
					ForgetTarget();
					if (!TryFindTarget())
						yield break;
				}
				
				if(debugMode > 0) Debug.Log(this + " go last known position " + _chaseCharacter);
				_selfCharacter.Routines.AssignMoveToDestination((Vector3)_lastTargetKnownPosition); //TODO "canUseRunAction"

				do
				{
					yield return null;

					if (_targetDetected)
					{
						if(debugMode > 0) Debug.Log(this + " chase AGAIN " + _chaseCharacter);
						break;
					}
				} while (CharacterRoutines.AnyActiveRoutines());
			}
			
			if (_targetDetected)
			{
				if(debugMode > 0) Debug.Log(this + " chase " + _chaseCharacter);
				_selfCharacter.Routines.AssignAttack(_chaseCharacter, true, false);
				
				do
				{
					SaveTargetPosition();
				
					yield return null;
				
					if (!_targetDetected)
						break;
				
				} while (CharacterRoutines.AnyActiveRoutines());
			}
			
			yield return null;
		}
	}

	private IEnumerator TryPatrol()
	{
		if (!_selfCharacter.HasMovePoints() || !_selfCharacter.HasActions())
			yield break;
		
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
			yield return null;

			if (_chaseCharacter && _targetDetected)
			{
				if(debugMode > 0) Debug.Log(this + " breaks patrol to chase");
				yield return StartCoroutine(TryChase());
				break;
			}
			
		} while (CharacterRoutines.AnyActiveRoutines());
	}

	private IEnumerator YieldCantAttack()
	{
		if(debugMode > 0) Debug.Log(this + " YieldCantAttack");
		yield return new WaitForSeconds(defaultWait);
	}
	
	private CharacterController _selfCharacter;
	private CharacterDetection _characterDetection;
	private CharacterController _chaseCharacter;
	private PatrolRoute _patrolRoute;
	private List<Waypoint> _waypointList;
	private Waypoint _currentWaypoint;
	private Vector3 _lastTargetKnownPosition;
	private bool _targetDetected;
	
	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterController>();
		if (!_selfCharacter)
		{
			Debug.LogWarning(this + " has no character controller!");
			enabled = false;
			return;
		}

		_selfCharacter.onPartyIndexChanged += OnPartyIndexChanged;
		
		_waypointList = new List<Waypoint>();
	}

	private void OnDestroy()
	{
		_selfCharacter.onPartyIndexChanged -= OnPartyIndexChanged;
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
	}

	private void ForgetTarget()
	{
		if (!_chaseCharacter)
		{
			Debug.LogWarning(this + " ForgetTarget() no target");
			return;
		}

		if(debugMode > 0) Debug.Log(this + " ForgetTarget " + _chaseCharacter);
		_chaseCharacter = null;
	}

	private void OnPartyIndexChanged(int index)
	{
		enabled = index < 0; // not in party
	}

	private void OnCharacterDetected(CharacterController otherCharacter)
	{
		if (!_chaseCharacter && canChase && _selfCharacter.CanAttackOther(otherCharacter))
		{
			_chaseCharacter = otherCharacter;
			_targetDetected = true;
			SaveTargetPosition();
			if(debugMode > 0) Debug.Log(this + " new _chaseCharacter=" + _chaseCharacter);
			
			if (lookAtDetectedEnemies) 
				CharacterRoutines.DoLookPositionOn(_selfCharacter, _lastTargetKnownPosition);
		}
	}

	private void OnCharacterLost(CharacterController otherCharacter)
	{
		if (_targetDetected && _chaseCharacter == otherCharacter)
		{
			if(debugMode > 0) Debug.Log(this + " looses target " + _chaseCharacter);
			SaveTargetPosition();
			_targetDetected = false;
		}
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
				var color = _targetDetected ? Color.red : Color.orange;
				DebugDrawer.Line(_selfCharacter.GetEyesPosition(), _chaseCharacter.GetCenterPosition(), color);
				DebugDrawer.WireDisc(_chaseCharacter.GetPosition(), _chaseCharacter.transform.forward, _chaseCharacter.GetRadius(), color);
			}
		}
	}

	private void OnGUI()
	{
		if (debugMode > 0)
		{
			if (IsThinking)
				Utils.DrawWorldGUI(_selfCharacter.GetPosition() + Vector3.up * _selfCharacter.GetHeight(), "Thinks...");
			
			if(_chaseCharacter)
				Utils.DrawWorldGUI((Vector3)_lastTargetKnownPosition, _chaseCharacter.name + " chased by " + name);
		}
	}
}