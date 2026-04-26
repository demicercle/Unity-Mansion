using System;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class AnimatorHandler : MonoBehaviour
{
	public bool startValue;
	public string parameterName;
	
	public void SetBool(bool value) => animator.SetBool(parameterName, value);
	public void ToggleBool() => animator.SetBool(parameterName, !animator.GetBool(parameterName));

	private Animator animator;
	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	private void Start()
	{
		SetBool(startValue);
	}
}