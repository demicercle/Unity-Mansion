using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerStart : MonoBehaviour
{
	private static List<PlayerStart> instances = new List<PlayerStart>();

	public static bool FindCurrent(out PlayerStart ps)
	{
		if (instances.Count > 0)
		{
			ps = instances.FirstOrDefault();
			return true;
		}

		ps = null;
		return false;
	}
	
	[SerializeField] private Waypoint[] waypoints;
	[SerializeField] private GameObject[] playerPrefabs;

	public IEnumerator Spawn()
	{
		for (int i = 0; i < waypoints.Length; i++)
		{
			var prefab = playerPrefabs[i];
			var instance = Instantiate(prefab).GetComponent<CharacterEntity>();
			instance.name = prefab.name;
			yield return null;
			instance.Spawn();
			instance.SetPartyIndex(i);
			instance.Teleport(waypoints[i].GetPosition(), waypoints[i].GetRotation());
		}
	}
	
	[ContextMenu("Collect Waypoints")]
	private void CollectWaypoints()
	{
		waypoints = GetComponentsInChildren<Waypoint>();
		Utils.SetDirty(this);
	}

	private void Awake()
	{
		instances.Add(this);
	}

	private void OnDestroy()
	{
		instances.Remove(this);
	}
}