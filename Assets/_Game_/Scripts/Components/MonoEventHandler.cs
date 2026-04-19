using System;
using UnityEngine;
using UnityEngine.Events;


public class MonoEventHandler : MonoBehaviour
{
	public UnityEvent onEnable, onDisable;

	private void OnEnable()
	{
		onEnable?.Invoke();
	}

	private void OnDisable()
	{
		onDisable?.Invoke();
	}
}