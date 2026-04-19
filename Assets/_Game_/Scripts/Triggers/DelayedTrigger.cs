using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class DelayedTrigger : MonoBehaviour
{
	public float delay = 1.0f;
	public UnityEvent trigger;

	IEnumerator Start()
	{
		if (delay > 0)
			yield return new WaitForSeconds(delay);
		
		trigger?.Invoke();
	}
}