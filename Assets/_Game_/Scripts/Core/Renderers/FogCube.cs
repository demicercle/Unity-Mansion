using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FogCube : MonoBehaviour
{
	public static void UpdateAll() => activeCubes.ForEach(c => c.DelayedChanged());
	
	[SerializeField] private float raycastDistance = 0.5f;
	[SerializeField] private ParticleSystem fogParticles;
	[SerializeField] private Renderer[] planesRenderers = new Renderer[4]
	{
		null, null, null, null
	};
	
	private static List<FogCube> activeCubes  = new List<FogCube>();
	private new Collider collider;

	private Ray GetRay(int i)
	{
		return planesRenderers[i]
			? new Ray(planesRenderers[i].transform.position + planesRenderers[i].transform.forward * raycastDistance * 0.5f, -planesRenderers[i].transform.forward)
			: new Ray(transform.position, transform.forward);
	}
	
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

	[SerializeField] private float fogBoxGrows = -0.5f;
	[ContextMenu("Update Particles")]
	private void UpdateParticles()
	{
		if (!fogParticles) return;
		#if UNITY_EDITOR
		if(!Application.isPlaying) UnityEditor.Undo.RecordObject(fogParticles, "Update Particles");
		#endif
		var shapeModule = fogParticles.shape;
		shapeModule.scale = transform.localScale * 0.5f + Vector3.one * fogBoxGrows;
		fogParticles.transform.position = collider.bounds.center;
		var emissionModule = fogParticles.emission;
		emissionModule.rateOverTime = Mathf.RoundToInt(shapeModule.scale.x * shapeModule.scale.y * shapeModule.scale.z / 5f);
		if(!Application.isPlaying) Utils.SetDirty(fogParticles);
	}
	
	[ContextMenu("Changed")]
	private void Changed()
	{
//		Debug.Log(this + " Changed");
		if(!collider) collider = GetComponent<Collider>();
		for (int i = 0; i < planesRenderers.Length; i++)
		{
			if (planesRenderers[i])
			{
				var ray = GetRay(i);
				planesRenderers[i].enabled = !Physics.Raycast(ray, out var hit, raycastDistance, Layers.MaskFog);
			}
		}
		
		UpdateParticles();
	}

	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if(!collider) collider = GetComponent<Collider>();
		Gizmos.color = Color.white;
		for (int i = 0; i < planesRenderers.Length; i++)
		{
			var ray = GetRay(i);
			Gizmos.DrawLine(ray.origin, ray.GetPoint(raycastDistance) );
			UnityEditor.Handles.Label(ray.origin, i.ToString());
		}
	}
	#endif
}