using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;


public class NavmeshRebuilder : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	private void OnEnable()
	{
		GameTurnManager.Instance.SetNavigationDirty();
	}

	private void OnDisable()
	{
		GameTurnManager.Instance.SetNavigationDirty();
	}
}