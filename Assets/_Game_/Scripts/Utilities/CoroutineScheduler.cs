using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CoroutineScheduler : MonoBehaviour
{
	public static CoroutineScheduler CreateNewInstance(string name = "Clone") => new GameObject("CoroutineScheduler (" + name + ")").AddComponent<CoroutineScheduler>();
	
	private static List<CoroutineScheduler> _instances = new List<CoroutineScheduler>();
	
	public bool HasCoroutines() => coroutines.Count > 0;
	
	public CoroutineScheduler Queue(IEnumerator routine)
	{ 
		coroutines.Add(routine);
		return this;
	}

	public CoroutineScheduler SetDebug(bool value)
	{
		debugLog = value;
		return this;
	}

	public Coroutine Wait()
	{
		return StartCoroutine(DoWait());
	}

	private IEnumerator DoWait()
	{
		while (coroutines.Count > 0)
		{
			if(debugLog) Debug.Log(this + " StartCoroutine: " + coroutines.Count);
			lastCoroutine = StartCoroutine(coroutines[0]);
			yield return lastCoroutine;
			coroutines.RemoveAt(0);
		}
		
		Destroy(gameObject);
	}
	
	private List<IEnumerator> coroutines = new List<IEnumerator>();
	private Coroutine lastCoroutine;
	private bool debugLog;
	
	private void Awake()
	{
		if(debugLog) Debug.Log(this + " Awake");
		_instances.Add(this);
	}

	private void OnDestroy()
	{
		if(debugLog) Debug.Log(this + " OnDestroy");
		_instances.Remove(this);
	}
}