using System.Collections;
using UnityEngine;

public class CoroutineScheduler : MonoBehaviour
{
	private static CoroutineScheduler instance;
	public static Coroutine Start(IEnumerator coroutine)
	{
		if(!instance) instance = new GameObject("CoroutineScheduler").AddComponent<CoroutineScheduler>();
		return instance.StartCoroutine(coroutine);
	}
}