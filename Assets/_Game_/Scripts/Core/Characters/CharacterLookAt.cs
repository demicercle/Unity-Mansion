using System;
using System.Collections;
using UnityEngine;

public class CharacterLookAt : MonoBehaviour
{
	[SerializeField] private Transform lookTarget;
	[SerializeField] private Vector3 dotRange = new Vector3(0.01f, 0.2f, 0.5f);
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
		
		if (_selfCharacter.IsDead() || _selfCharacter.IsActing() || _selfCharacter.IsMoving() || !_selfCharacter.IsCharacterRevealed())
		{
			return;
		}
		
		if (GameTurnManager.Instance.IsTurnBasedMode())
		{
			if (_selfCharacter.TryGetClosestEnemy(out CharacterEntity enemy))
			{
				_followTarget = enemy.transform;
			}
		}
		else
		{
			if (_selfCharacter.IsSelected())
			{
				GameInput.Instance.TryGetLookTarget(out _followTarget);
			}
				
			else if (_selfCharacter.IsControllableByPlayer())
			{
				if (CharacterSelection.TryGetSelectedCharacter(out CharacterEntity selected) && selected != _selfCharacter && _selfCharacter.TryGetFollowing(out var following) && following == selected)
				{
					_followTarget = following.transform;
				}
			}
		}

		if (_followTarget == transform)
		{
			_followTarget = null;
		}
	}

	private void LateUpdate ()
	{
		if (!lookTarget)
		{
			if (_selfCharacter.TryGetComponent(out PerfectLookAt pla))
			{
				lookTarget = pla.m_TargetObject?.transform;
			}
			
			if (!lookTarget)
			{
				//Debug.LogWarning(this + " has no look target!");
				enabled = false;
				return;
			}
		}

		worldPosition = _selfCharacter.GetEyesPosition() + _selfCharacter.GetDirection() * (_selfCharacter.GetRadius() * 2);

		UpdateFollows();
		
		if (_followTarget)
		{
			if (Vector3.Distance(_followTarget.position, _selfCharacter.GetPosition()) <= maxDistanceToLookAt
			    && Utils.VDistance(_followTarget.position, _selfCharacter.GetPosition()) <= _selfCharacter.GetHeight())
			{
				worldPosition = _followTarget.position + Vector3.up * _selfCharacter.GetHeight();
			}
		}
		
		_isAiming = _selfCharacter.TryGetAimingPosition(out var aimingPosition);
		
		if (_isAiming)
		{
			worldPosition = aimingPosition;
		}
			
		lookTarget.name = "LookTarget (" + _selfCharacter.name + ")";
		lookTarget.SetParent(null, true);
		lookTarget.position = worldPosition;
			
		worldDir = _selfCharacter.GetDirectionTo(lookTarget.position);
		worldDot = Mathf.Abs(1f - Utils.FlatDot(transform.forward, worldDir));
		minDotToRotate = _isAiming ? dotRange.x : _isRotating ? dotRange.y : dotRange.z;
		if (worldDot > minDotToRotate && !_selfCharacter.IsMoving())
		{
			_isRotating = true;
			_selfCharacter.LookWorldPosition(lookTarget.position, Time.deltaTime);
		}
		else _isRotating = false;
		
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