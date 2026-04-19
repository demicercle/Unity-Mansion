using System;
using UnityEngine;


public class ForceSiblingIndex : MonoBehaviour
{
	public int forcedIndex;

	private void LateUpdate()
	{
		if (!transform.parent) return;
		if(forcedIndex < 0)
			transform.SetAsLastSibling();
		else 
			transform.SetSiblingIndex(forcedIndex % transform.parent.childCount);
	}
}