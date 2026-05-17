using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerStart : MonoBehaviour
{
	private static List<PlayerStart> instances = new List<PlayerStart>();
	public static PlayerStart[] GetPlayerStarts() => instances.OrderBy(ps => ps.index).ToArray();

	public static PlayerStart GetPlayerStart(int playerStartIndex)
	{
		return instances.FirstOrDefault(ps => ps.index == playerStartIndex);
	}
	
	[SerializeField] private int index;
	[SerializeField] private Waypoint[] waypoints;
	[SerializeField] private GameObject[] playerPrefabs;

	private LevelComponent parentLevel;

	public void TeleportParty() => TeleportParty(CharacterEntity.GetPartyCharacters());
	public void TeleportParty(CharacterEntity[] partyChars)
	{
		for (int i = 0; i < partyChars.Length; i++)
		{
			partyChars[i].Teleport(waypoints[i].GetPosition(), waypoints[i].GetRotation());
			RoomTrigger.RevealAtPosition(waypoints[i].GetPosition());
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
			RoomTrigger.RevealAtPosition(waypoints[i].GetPosition());
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