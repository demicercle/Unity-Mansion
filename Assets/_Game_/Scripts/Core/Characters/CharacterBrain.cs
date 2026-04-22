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
	[SerializeField] private bool canChase = true;
	[SerializeField] private bool canPatrol = true;
	[SerializeField] private bool canUseRunAction = false;
	[SerializeField] private float defaultWait = 1.5f;

	public IEnumerator Think()
	{
		Debug.Log(this + " Thinking...");
		yield return new WaitForSeconds(defaultWait);
		
		// try chase
		if (canChase)
			yield return StartCoroutine(TryChase());
		
		// try patrol
		if (canPatrol)
			yield return StartCoroutine(TryPatrol());
		
		// fallback
		
		yield return new WaitForSeconds(defaultWait);
		Debug.Log(this + " ends");
	}

	private IEnumerator TryChase()
	{
		if (_chaseCharacter && _selfCharacter.CanAttackOther(_chaseCharacter))
		{
			Debug.Log(this + " chase " + _chaseCharacter);
			_selfCharacter.Routines.AssignAttack(_chaseCharacter, true, false);
			
			do
			{
				yield return null;
			} while (CharacterRoutines.AnyActiveRoutines());
		}
	}

	private IEnumerator TryPatrol()
	{
		if (!_selfCharacter.HasMovePoints()) 
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
			// todo: chase if character detected
			yield return null;
		} while (CharacterRoutines.AnyActiveRoutines());
	}
	
	private CharacterController _selfCharacter;
	private CharacterDetection _characterDetection;
	private CharacterController _chaseCharacter;
	private PatrolRoute _patrolRoute;
	private List<Waypoint> _waypointList;
	private Waypoint _currentWaypoint;
	
	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterController>();
		if (!_selfCharacter)
		{
			Debug.LogWarning(this + " has no character controller!");
			enabled = false;
			return;
		}
		
		_waypointList = new List<Waypoint>();
		
		_characterDetection = GetComponent<CharacterDetection>();
		if (_characterDetection)
		{
			_characterDetection.onCharacterDetected += OnCharacterDetected;
			_characterDetection.onCharacterLost += OnCharacterLost;
		}
	}

	private void OnDestroy()
	{
		if (_characterDetection)
		{
			_characterDetection.onCharacterDetected -= OnCharacterDetected;
			_characterDetection.onCharacterLost -= OnCharacterLost;
		}
	}

	private void OnCharacterDetected(CharacterController otherCharacter)
	{
		if (!_chaseCharacter && canChase && _selfCharacter.CanAttackOther(otherCharacter))
		{
			_chaseCharacter = otherCharacter;
			Debug.Log(this + " new _chaseCharacter=" + _chaseCharacter);
		}
	}

	private void OnCharacterLost(CharacterController otherCharacter)
	{
		if (_chaseCharacter == otherCharacter)
		{
			Debug.Log(this + " looses target " + _chaseCharacter);
			_chaseCharacter = null;
		}
	}
}