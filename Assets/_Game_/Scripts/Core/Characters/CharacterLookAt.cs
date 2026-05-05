using System;
using System.Collections;
using UnityEngine;

public class CharacterLookAt : MonoBehaviour
{
	[SerializeField] private Transform lookTarget;
	[SerializeField] private Vector3 dotRange = new Vector3(0.01f, 0.2f, 0.5f);
	[SerializeField] [Range(0.1f, 1f)] private float tickTime = 0.5f;
	[SerializeField] [Min(0f)] private float lerpSpeed = 1.5f;
	[SerializeField] private float maxDistanceToLookAt = 5.0f;
	
	private CharacterEntity _selfCharacter;
	private CharacterFollows _selfFollows;
	private Transform _followTarget;
	private Vector3 worldPosition;
	private Vector3 worldDir;
	private float worldDot;
	private float minDotToRotate;
	private bool _isRotating;
	private bool _isAiming;

	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
		_selfFollows = GetComponent<CharacterFollows>();
	}

	private void UpdateFollows()
	{
		_followTarget = null;
		
		if (_selfCharacter.IsDead() || !_selfCharacter.IsCharacterRevealed())
		{
			return;
		}
		
		if (_selfCharacter.IsSelected())
		{
			GameInput.Instance.TryGetNavigationTarget(out _followTarget);
		}
		else
		{
			if (GameTurnManager.Instance.IsTurnBasedMode())
			{
				if (_selfCharacter.TryGetClosestEnemy(out CharacterEntity enemy))
				{
					_followTarget = enemy.transform;
				}
			}
			else
			{
				if (_selfCharacter.IsControllableByPlayer())
				{
					if (CharacterSelection.TryGetSelectedCharacter(out CharacterEntity leader) && leader != _selfCharacter)
					{
						_followTarget = leader.transform;
					}
				}
			}
		}
	}

	private void LateUpdate ()
	{
		UpdateFollows();
		
		if (!lookTarget)
		{
			if (_selfCharacter.TryGetComponent(out PerfectLookAt pla))
			{
				lookTarget = pla.m_TargetObject?.transform;
			}
			
			if (!lookTarget)
			{
				Debug.LogWarning(this + " has no look target!");
				enabled = false;
				return;
			}
		}

		_isAiming = _selfCharacter.TryGetAimingPosition(out var aimingPosition);
		
		if( _selfCharacter.IsActing() || (!_followTarget && !_isAiming))
		{
			_isRotating = false;
			lookTarget.parent = _selfCharacter.transform; // no target, back to default pos
			lookTarget.localPosition = Vector3.forward * (_selfCharacter.GetRadius() * 2) + Vector3.up * _selfCharacter.GetHeight();
		}
		else
		{
			if (_followTarget && Vector3.Distance(_followTarget.position, transform.position) <= maxDistanceToLookAt)
				worldPosition = _followTarget.position + Vector3.up * _selfCharacter.GetHeight();
			
			if (_isAiming) 
				worldPosition = aimingPosition;
			
			var f = Time.deltaTime;
			lookTarget.parent = null;
			lookTarget.position = lerpSpeed <= 0 ? worldPosition : Vector3.Lerp(lookTarget.position, worldPosition, f * lerpSpeed);
			
			worldDir = _selfCharacter.GetDirectionTo(lookTarget.position);
			worldDot = Mathf.Abs(1f - Utils.FlatDot(transform.forward, worldDir));
			minDotToRotate = _isAiming ? dotRange.x : _isRotating ? dotRange.y : dotRange.z;
			if (worldDot > minDotToRotate)
			{
				_isRotating = true;
				_selfCharacter.LookWorldPosition(lookTarget.position, f);
			}
			else _isRotating = false;
		}
		
		if (CharacterFollows.debugMode > 1 )
		{
			if(_followTarget) DebugDrawer.Line(_selfCharacter.GetEyesPosition(), _followTarget.position, Color.orange);
			DebugDrawer.Line(_selfCharacter.GetEyesPosition(), lookTarget.position, Color.red);
			DebugDrawer.Sphere(lookTarget.position, 0.1f, Color.red);
		}
	}

	private void OnGUI()
	{
		if (CharacterFollows.debugMode > 1)
		{
			Utils.DrawWorldGUI(_selfCharacter.GetEyesPosition(), worldDot.ToString("N2"));
		}
	}
}