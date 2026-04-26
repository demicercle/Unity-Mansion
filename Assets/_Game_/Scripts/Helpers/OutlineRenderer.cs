using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof (QuickOutline))]
public class OutlineRenderer : MonoBehaviour, ICursorHandler
{
	public void CursorOver()
	{
		quickOutline.enabled = true;
		
		StopAllCoroutines();
		StartCoroutine(WaitAndHide());
	}

	public bool ignorePresets;

	void Awake()
	{
		var color = Color.yellow;
		var size = 5f;

		quickOutline = GetComponent<QuickOutline>();
		
		if (!quickOutline)
			quickOutline =gameObject.AddComponent<QuickOutline>();
		
		if (!ignorePresets)
		{
			quickOutline.OutlineWidth = size;
			quickOutline.OutlineColor = color;
		}
			
		quickOutline.enabled = false;
	}
	
	private QuickOutline quickOutline;

	IEnumerator WaitAndHide()
	{
		yield return null;
		quickOutline.enabled = false;
	}
}