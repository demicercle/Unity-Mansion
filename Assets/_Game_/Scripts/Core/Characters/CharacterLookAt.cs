using System;
using System.Collections;
using UnityEngine;

public class CharacterLookAt : MonoBehaviour
{
	[SerializeField] private Transform lookTarget;
	[SerializeField] private Vector2 dotRange = new Vector2(0f, 0.5f);
	[SerializeField] [Range(0.1f, 1f)] private float tickTime = 0.5f;
	[SerializeField] [Min(0f)] private float lerpSpeed = 1.5f;
	
	private CharacterEntity _selfCharacter;
	private Transform _followTarget;
	private Vector3 worldPosition;
	private Vector3 worldDir;
	private float worldDot;
	private float minDotToRotate;
	private bool _isRotating;

	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
	}

	private IEnumerator Start()
	{
		while (true)
		{
			yield return new WaitForSeconds(tickTime);
			
			if (_selfCharacter.IsDead() || !_selfCharacter.IsCharacterRevealed())
				continue;
			
			UpdateFollows();
		}
	}

	private void UpdateFollows()
	{
		_followTarget = null;

		if (_selfCharacter.Routines.AnyActive())
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
				if (_selfCharacter.IsControllableByPlayer() && CharacterSelection.TryGetSelectedCharacter(out CharacterEntity leader))
				{
					_followTarget = leader.transform;
				}
			}
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
				Debug.LogWarning(this + " has no look target!");
				enabled = false;
				return;
			}
		}
		
		var f = Time.deltaTime;
		if (_followTarget)
		{ 
			worldPosition = _followTarget.position + Vector3.up * _selfCharacter.GetHeight();
			lookTarget.parent = null;
			lookTarget.position = lerpSpeed <= 0 ? worldPosition : Vector3.Lerp(lookTarget.position, worldPosition, f * lerpSpeed);
			
			 worldDir = _selfCharacter.GetDirectionTo(lookTarget.position);
			 worldDot = Utils.FlatDot(transform.forward, worldDir);
			 minDotToRotate = _isRotating ? dotRange.y : dotRange.x;
			if (worldDot < minDotToRotate && !_selfCharacter.Routines.AnyActive())
			{
				_isRotating = true;
				_selfCharacter.LookWorldPosition(lookTarget.position, f);
			}
			else _isRotating = false;
		}
		else
		{
			lookTarget.parent = _selfCharacter.transform;
			lookTarget.localPosition = Vector3.forward * (_selfCharacter.GetRadius() * 2) + Vector3.up * _selfCharacter.GetHeight();
		}
	}
}