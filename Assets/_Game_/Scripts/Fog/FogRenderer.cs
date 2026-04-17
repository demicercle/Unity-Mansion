using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class FogRenderer : MonoBehaviour
{
	public UnityEvent<bool> onVisibilityChanged;
	public UnityEvent onBecameVisible;
	public UnityEvent onBecameInvisible;

	public Vector3[] GetCorners() => new[]
	{
		transform.position + transform.forward * radius,
		transform.position - transform.forward * radius,
		transform.position + transform.right * radius,
		transform.position - transform.right * radius,
	};

	private Vector3Int lastCell;
	private bool wasVisible;
	private float radius;

	private void Start()
	{
		var capsule = GetComponent<CapsuleCollider>();
		if (capsule) radius = capsule.radius;
		
		var sphere = GetComponent<SphereCollider>();
		if(sphere) radius = sphere.radius;
		
		Debug.Log(this + " radius " + radius);
	}

	private void LateUpdate()
	{
		var fogProjector = FogProjector.GetInstance();
		var currentCell = fogProjector.WorldToCell(transform.position);
		if (currentCell != lastCell)
		{
			// moved
			var isVisible = GetCorners().Any(c => fogProjector.IsPositionVisible(c));
		
			if (isVisible != wasVisible)
			{
				(isVisible ? onBecameVisible : onBecameInvisible)?.Invoke();
				onVisibilityChanged?.Invoke(isVisible);
			
				wasVisible = isVisible;
			}
		}
	}
}