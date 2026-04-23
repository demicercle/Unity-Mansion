using System;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class AnimatorHandler : MonoBehaviour
{
	public string parameterName;
	
	public void SetBool(bool value) => animator.SetBool(parameterName, value);

	private Animator animator;
	private void Awake()
	{
		animator = GetComponent<Animator>();
	}
}