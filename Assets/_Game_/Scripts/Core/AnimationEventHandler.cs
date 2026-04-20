using UnityEngine;
using UnityEngine.Events;

public class AnimationEventHandler : MonoBehaviour
{
	public UnityEvent onShoot = new();
	public void Shoot() => onShoot.Invoke();

	public UnityEvent onHit = new();
	public void Hit() => onHit.Invoke();

	public UnityEvent onFootL = new();
	public void FootL() => onFootL.Invoke();

	public UnityEvent onFootR = new();
	public void FootR() => onFootR?.Invoke();

	public UnityEvent onWeaponSwitch = new();
	public void WeaponSwitch() => onWeaponSwitch?.Invoke();
	
	public void DisableAnimator() => GetComponent<Animator>().enabled = false;
}