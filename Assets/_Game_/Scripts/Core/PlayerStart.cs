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

	private LevelComponent parentLevel;
	
	public LevelComponent GetParentLevel() => parentLevel;

	public void TeleportParty() => TeleportParty(CharacterEntity.GetPartyCharacters());
	public void TeleportParty(CharacterEntity[] partyChars)
	{
		for (int i = 0; i < partyChars.Length; i++)
		{
			partyChars[i].Teleport(waypoints[i].GetPosition(), waypoints[i].GetRotation());
		}
	}

	public IEnumerator SpawnParty()
	{
		for (int i = 0; i < playerPrefabs.Length; i++)
		{
			var player = Instantiate(playerPrefabs[i]).GetComponent<CharacterEntity>();
			player.name = playerPrefabs[i].name;
			yield return null;
			player.Spawn();
			player.SetPartyIndex(i);
			player.Teleport(waypoints[i].GetPosition(), waypoints[i].GetRotation());
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
		parentLevel = GetComponentInParent<LevelComponent>();
		instances.Add(this);
	}

	private void OnDestroy()
	{
		instances.Remove(this);
	}
}