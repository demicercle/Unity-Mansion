using System;
using UnityEngine;

public class SnapScale : MonoBehaviour
{
	public float snap = 0.5f;
	public bool ignoreY = false;
	public bool preventNegativeScale = false;
	
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying) return;
		if (!enabled) return;
		var newScale = Snapping.Snap(transform.localScale, snap * Vector3.one);
		if (ignoreY) newScale.y = transform.localScale.y;
		if (newScale.ToString() != transform.localScale.ToString())
		{
			if (preventNegativeScale)
			{
				newScale.x = Mathf.Abs(newScale.x);
				newScale.y = Mathf.Abs(newScale.y);
				newScale.z = Mathf.Abs(newScale.z);
			}
			
			transform.localScale = newScale;
			Utils.SetDirty(transform);
		}
	}
}
