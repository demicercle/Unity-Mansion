using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof (QuickOutline))]
public class OutlineRenderer : MonoBehaviour, ICursorHandler
{
	const float HitBoxSize = 0.5f;
	
	public static void ForceHightlight()
	{
		activeInstances.ForEach(o =>
		{
			foreach (var corner in new Vector3[]
			         {
				         Vector3.forward * HitBoxSize, Vector3.right * HitBoxSize, Vector3.up * HitBoxSize,
				         -Vector3.forward * HitBoxSize, -Vector3.right * HitBoxSize, -Vector3.up * HitBoxSize,
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
		UpdateColorAndSize();
		quickOutline.enabled = true;
		
		StopAllCoroutines();
		StartCoroutine(WaitAndHide());
	}

	private float defaultSize;
	private Color defaultColor;

	private CharacterEntity parentCharacter;

	private void Awake()
	{
		quickOutline = GetComponent<QuickOutline>();
		
		if (!quickOutline)
			quickOutline =gameObject.AddComponent<QuickOutline>();
			
		quickOutline.enabled = false;
		
		activeInstances.Add(this);
		
		parentCharacter = GetComponentInParent<CharacterEntity>();
	}

	private void OnDestroy()
	{
		activeInstances.Remove(this);
	}

	private QuickOutline quickOutline;

	private void UpdateColorAndSize()
	{
		if (parentCharacter)
		{
			defaultColor = ColorSettings.GetCharacterColor(parentCharacter, false, false);
			defaultSize = ColorSettings.GetCharacterOutlineSize(parentCharacter);
		}
		else
		{
			defaultColor = ColorSettings.GetDefaultOutlineColor();
			defaultSize = ColorSettings.GetDefaultOutlineSize();
		}
		
		quickOutline.OutlineWidth = defaultSize;
		quickOutline.OutlineColor = defaultColor;
	}

	IEnumerator WaitAndHide()
	{
		yield return null;
		quickOutline.enabled = false;
	}
}