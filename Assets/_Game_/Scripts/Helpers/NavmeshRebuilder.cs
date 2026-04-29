using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;


public class NavmeshRebuilder : MonoBehaviour
{
	public static int debugMode { get; set; }

	public static void SetNavigationDirty() => navmeshDirty = true;
	public static bool IsNavigationDirty() => navmeshDirty || isNavmeshRebuild;
	
	public bool isMainInstance { get; set; }

	private static bool navmeshDirty;
	private static bool isNavmeshRebuild;
	
	private void OnEnable()
	{
		SetNavigationDirty();
	}

	private void OnDisable()
	{
		SetNavigationDirty();
	}

	private IEnumerator Start()
	{
		if(!isMainInstance)
			yield break;

		while (true)
		{
			if (!navmeshDirty) yield return null;
			else
			{
				navmeshDirty = false;
				isNavmeshRebuild = true;
				foreach (var pr in AstarPath.active.ScanAsync())
				{
					if (NavmeshRebuilder.debugMode > 0) Debug.Log(this + " Scanning... " + pr.progress);
					yield return null;
				}
				isNavmeshRebuild = false;
			}
		}
	}
}