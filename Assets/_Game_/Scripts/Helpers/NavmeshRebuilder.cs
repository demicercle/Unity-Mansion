using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;


public class NavmeshRebuilder : MonoBehaviour
{
	public static int debugMode { get; set; }

	public static bool IsRebuildingAsync() => instances.Count > 0;

	public static void RebuildNavigationOn(GameObject go, float delay = 0.1f)
	{
		go.AddComponent<NavmeshRebuilder>().delay = delay;
	}
	
	private static List<NavmeshRebuilder> instances = new List<NavmeshRebuilder>();
	
	public void Activate() => enabled = true; // To be used in unity animation

	[SerializeField] private bool destroySelf = true;
	[SerializeField] private bool destroyGameObject = false;
	
	private float delay;
	private float progress;
	private IEnumerable<Progress> scanProgress;

	private void Awake()
	{
		if(debugMode > 0) Debug.Log(this + " Awake");
		
		foreach (var other in GetComponents<NavmeshRebuilder>())
		{
			if (other != this)
				Destroy(other); // take priority
		}
	}

	private void OnEnable()
	{
		StartCoroutine(DoRebuild());
	}

	private void OnDisable()
	{
		if (instances.Contains(this))
			instances.Remove(this);
		
		StopAllCoroutines();
	}

	IEnumerator DoRebuild()
	{
		instances.Add(this);
		if(debugMode >0) Debug.Log(this + " RebuildNavigationAsync Start " + delay);
		
		if(delay > 0f) 
			yield return new WaitForSeconds(delay);
		
		while (instances[0] != this) 
			yield return null;

		foreach (Progress pr in AstarPath.active.ScanAsync())
		{
			progress = pr.progress;
			if(debugMode > 1) Debug.Log(this + " Scanning... " + pr.ToString());
			yield return null;
		}

		instances.Remove(this);
		
		if (destroyGameObject) 
			Destroy(gameObject);
		else if (destroySelf)
			Destroy(this);
		else
			enabled = false;

		foreach (var character in CharacterEntity.AllCharacters)
		{
			var agent = character.GetAgent();
			if (agent.IsActive())
			{
				agent.RecalculatePath();
			}
		}
	}

	private void OnGUI()
	{
		if (debugMode > 0)
		{
			Utils.DrawWorldGUI(transform.position, progress.ToString("N2"));
		}
	}
}