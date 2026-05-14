using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FogCube : MonoBehaviour
{
	[SerializeField] private float raycastDistance = 0.5f;

	[SerializeField] private Renderer[] planesRenderers = new Renderer[4]
	{
		null, null, null, null
	};
	
	[SerializeField] private Vector3[] raycastDirections = new Vector3[4]
	{
		Vector3.forward,
		Vector3.back,
		Vector3.right,
		Vector3.left
	};
	
	private static List<FogCube> activeCubes  = new List<FogCube>();
	private new Collider collider;
	
	private Ray GetRay(int dirIndex) => new Ray(collider.bounds.center - raycastDirections[dirIndex] * raycastDistance * 0.5f + Vector3.Scale(collider.bounds.extents, raycastDirections[dirIndex]), raycastDirections[dirIndex]);

	private void Awake()
	{
		collider =  GetComponent<Collider>();
	}

	private void OnEnable()
	{
		activeCubes.Add(this);
		activeCubes.ForEach(c => c.DelayedChanged());
	}

	private void OnDisable()
	{
		activeCubes.Remove(this);
		activeCubes.ForEach(c => c.DelayedChanged());
	}

	private void DelayedChanged()
	{
		CancelInvoke("Changed");
		Invoke("Changed", 0.1f);
	}
	
	[ContextMenu("Changed")]
	private void Changed()
	{
		if(!collider) collider = GetComponent<Collider>();
		for (int i = 0; i < raycastDirections.Length; i++)
		{
			if (planesRenderers[i])
			{
				var ray = GetRay(i);
				planesRenderers[i].enabled = !Physics.Raycast(ray, out var hit, raycastDistance, Layers.MaskFog);
			}
		}
	}

	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if(!collider) collider = GetComponent<Collider>();
		Gizmos.color = Color.white;
		for (int i = 0; i < raycastDirections.Length; i++)
		{
			var ray = GetRay(i);
			Gizmos.DrawLine(ray.origin, ray.GetPoint(raycastDistance) );
			UnityEditor.Handles.Label(ray.origin, i.ToString());
		}
	}
	#endif
}