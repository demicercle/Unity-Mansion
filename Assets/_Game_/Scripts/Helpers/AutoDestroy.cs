using System.Collections;
using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
	public float delay = 2.0f;

	IEnumerator Start()
	{
		yield return new WaitForSeconds(delay);
		Destroy(gameObject);
	}
}