using System.Collections;
using UnityEngine;

public class OutlineRenderer : MonoBehaviour, ICursorHandler
{
	public void CursorOver()
	{
		foreach (var quickOutline in GetComponents<QuickOutline>())
			quickOutline.enabled = true;
		StopAllCoroutines();
		StartCoroutine(WaitAndHide());
	}

	public bool ignorePresets;

	void Awake()
	{
		var color = Color.yellow;
		var size = 5f;
		
		foreach (var quickOutline in GetComponents<QuickOutline>())
		{
			if (!ignorePresets)
			{
				quickOutline.OutlineWidth = size;
				quickOutline.OutlineColor = color;
			}
			
			quickOutline.enabled = false;
		}
	}

	IEnumerator WaitAndHide()
	{
		yield return null;
		
		foreach (var quickOutline in GetComponents<QuickOutline>())
			quickOutline.enabled = false;
		
//		Debug.Log("Hide: " + this);
	}
}