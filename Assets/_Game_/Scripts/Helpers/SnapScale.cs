using System;
using UnityEngine;

public class SnapScale : MonoBehaviour
{
	public float snap = 0.5f;
	public bool ignoreY = false;
	public bool preventNegativeScale = false;

	[ContextMenu("Do Snap")]
	private void DoSnap() => DoSnap(true);
	private void DoSnap(bool setDirty)
	{
		var newScale = Snapping.Snap(transform.localScale, snap * Vector3.one);
		
		if (ignoreY) newScale.y = transform.localScale.y;
		
		if (preventNegativeScale)
		{
			newScale.x = Mathf.Abs(newScale.x);
			newScale.y = Mathf.Abs(newScale.y);
			newScale.z = Mathf.Abs(newScale.z);
		}
			
		transform.localScale = newScale;
		
		if(setDirty)
			Utils.SetDirty(transform);
	}
	
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying) return;
		if (!enabled) return;
		var oldScale = transform.localScale;
		DoSnap();
		if (oldScale != transform.localScale)
			Utils.SetDirty(transform);
	}
}
