using System;
using UnityEngine;

public class ForceSetSiblingIndex : MonoBehaviour
{
	public int siblingIndex = -1;
	
	private void OnDrawGizmosSelected()
	{
		Start();
	}

	private void Start()
	{
		if (siblingIndex >= 0)
			transform.SetSiblingIndex(siblingIndex);
		else 
			transform.SetAsFirstSibling();
	}
}