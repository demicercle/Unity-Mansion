using System.Collections;
using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
	[SerializeField] private float delay = 2.0f;

	IEnumerator Start()
	{
		yield return new WaitForSeconds(delay);
		Destroy(gameObject);
	}
}