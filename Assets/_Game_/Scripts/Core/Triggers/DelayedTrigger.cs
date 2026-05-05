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
		if(delay <= 0) 
			Trigger();
		else
			Invoke("Trigger", delay);
	}

	private void OnDisable()
	{ 
		if(delay > 0)
			CancelInvoke("Trigger");;
	}

	private void Trigger()
	{
		trigger?.Invoke();
	}
}