using System;
using System.Collections;
using UnityEngine;

public class CoroutineScheduler : MonoBehaviour
{
	public static CoroutineScheduler Start(GameObject holder, IEnumerator coroutine)
	{
		var instance = holder.AddComponent<CoroutineScheduler>();
		instance._coroutine = coroutine;
		return instance;
	}

	public void StopAndDestroy()
	{
		Destroy(this);
	}

	private IEnumerator _coroutine;

	private IEnumerator Start()
	{
		yield return StartCoroutine(_coroutine);
		Destroy(this);
	}
}