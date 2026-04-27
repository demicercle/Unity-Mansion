using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class DelayedTrigger : MonoBehaviour
{
	public float delay = 1.0f;
	public UnityEvent trigger;

	private void OnEnable()
	{
		Invoke("Trigger", delay);
	}

	private void OnDisable()
	{ 
		CancelInvoke("Trigger");;
	}

	private void Trigger()
	{
		trigger?.Invoke();
	}
}