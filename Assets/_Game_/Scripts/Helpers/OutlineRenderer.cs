using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof (QuickOutline))]
public class OutlineRenderer : MonoBehaviour, ICursorHandler
{
	public static void ForceHightlight()
	{
		activeInstances.ForEach(o =>
		{
			foreach (var corner in new Vector3[]
			         {
				         Vector3.forward * o.boxSize, Vector3.right * o.boxSize, Vector3.up * o.boxSize,
				         -Vector3.forward * o.boxSize, -Vector3.right * o.boxSize, -Vector3.up * o.boxSize,
			         })
			{
				if (RoomTrigger.IsPositionVisible(o.transform.position + corner))
				{
					o.CursorOver();
					return;
				}
			}
		});
	}
	
	private static List<OutlineRenderer> activeInstances = new List<OutlineRenderer>();
	
	public void CursorOver()
	{
		quickOutline.enabled = true;
		
		StopAllCoroutines();
		StartCoroutine(WaitAndHide());
	}

	public float boxSize = 0.5f;
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
		
		activeInstances.Add(this);
	}

	private void OnDestroy()
	{
		activeInstances.Remove(this);
	}

	private QuickOutline quickOutline;

	IEnumerator WaitAndHide()
	{
		yield return null;
		quickOutline.enabled = false;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(transform.position, new Vector3(boxSize, boxSize, boxSize));
	}
}