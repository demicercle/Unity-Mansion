using System;
using System.Collections;
using UnityEngine;


public class GUILabel : MonoBehaviour
{
	public bool destroyGameObject = true;
	public string message = "";
	public float duration = 0.0f;
	public Transform target;
	public Vector2 size = Vector2.one * 100;
	public Vector3 worldPosition = Vector3.zero;

	private IEnumerator Start()
	{
		if (duration < 0)
			yield break;

		if (target != null)
		{
			transform.SetParent(target);
			transform.localPosition = worldPosition;
		}
		else
		{
			transform.position = worldPosition;
		}
		
		if (duration > 0f)
			yield return new WaitForSeconds(duration);
		else
			yield return new WaitForEndOfFrame();
		
		if (destroyGameObject) Destroy(gameObject);
	}

	private void OnGUI()
	{
		worldPosition = Camera.main.WorldToScreenPoint(transform.position);
		GUI.Label(new Rect(worldPosition.x, Screen.height - worldPosition.y, size.x, size.y), message);
	}
}