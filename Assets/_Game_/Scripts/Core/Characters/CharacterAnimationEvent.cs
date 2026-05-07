using System;
using UnityEngine;
using UnityEngine.Events;
using EventType = CharacterAnimation.EventType;

public class CharacterAnimationEvent : MonoBehaviour
{
	public UnityEvent onFootL = new();
	public void FootL() => onFootL.Invoke();

	public UnityEvent onFootR = new();
	public void FootR() => onFootR?.Invoke();

	public void DisableAnimator() => GetComponent<Animator>().enabled = false;

	public System.Action<EventType> onCharacterAnimationEvent;
	
	public UnityEvent onWeaponSwitch = new();
	public void WeaponSwitch()
	{
		onCharacterAnimationEvent?.Invoke(EventType.WeaponSwitch);
		onWeaponSwitch?.Invoke();
	}

	public UnityEvent onHit = new();
	public void Hit()
	{
		onCharacterAnimationEvent?.Invoke(EventType.Hit);
		onHit.Invoke();
		foreach (var wp in GetComponentsInChildren<WeaponRenderer>()) // OnHit
		{
			wp.OnHit();
		}
	}
	
	public UnityEvent onShoot = new();
	public void Shoot()
	{
		onCharacterAnimationEvent?.Invoke(EventType.Shoot);
		onShoot.Invoke();
	}
	
	public UnityEvent onScream = new();
	public void Scream()
	{
		onCharacterAnimationEvent?.Invoke(EventType.Scream);
		onScream.Invoke();
	}
}