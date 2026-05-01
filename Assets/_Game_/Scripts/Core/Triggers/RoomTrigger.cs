using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class RoomTrigger : MonoBehaviour
{
	public static List<RoomTrigger> allRooms = new List<RoomTrigger>();
	public static List<RoomTrigger> activeRooms = new List<RoomTrigger>();

	public static bool IsPositionVisible(Vector3 world)
	{
		return !activeRooms.Any(room => room.HasPosition(world));
	}
	
	public static bool IsCharacterVisible(CharacterEntity character)
	{
		if (activeRooms.Any(room => room.HasCharacter(character)))
			return false;
		
		return true;
	}

	public bool IsName(string n) => name == n;
	public string GetName() => name;

	public static bool TryGetRoom(CharacterEntity character, out RoomTrigger inRoom)
	{
		foreach (var room in allRooms)
		{
			if (room.HasCharacter(character))
			{
				inRoom = room;
				return true;
			}
		}

		inRoom = null;
		return false;
	}
	
	public List<CharacterEntity> GetCharacters()
	{
		var outList = new  List<CharacterEntity>();
		foreach (var trigger in triggers)
		{
			outList.AddRange(trigger.GetCharacters());
		}
		return outList;
	}

	public static System.Action onChanged;
	
	[Header("Settings")]
	[SerializeField] private List<GameObject> doors = new List<GameObject>();
	[SerializeField] private List<CharacterTrigger> triggers =  new List<CharacterTrigger>();
	
	private bool createCeilings;
	private List<BoxCollider> ceilings = new List<BoxCollider>();

	public bool IsRevealed() => !gameObject.activeSelf;

	public void Explore()
	{
		if (!gameObject.activeSelf)
		{
			Debug.Log(this + " already explored");
			return;
		}
		
		gameObject.SetActive(false);
		onChanged?.Invoke();
	}
	
	public bool HasCharacter(CharacterEntity character)
	{
		return triggers.Any(t => t.HasCharacter(character));
	}

	public bool HasPosition(Vector3 world)
	{
		return triggers.Any(t => t.HasPosition(world));
	}

	#if UNITY_EDITOR
	
	[ContextMenu("Collect Triggers")]
	private void CollectTriggers()
	{
		UnityEditor.Undo.RecordObject(this, "Collect Triggers");
		triggers.Clear();
		triggers.AddRange(GetComponentsInChildren<CharacterTrigger>());
		Utils.SetDirty(this);
	}
	
	[ContextMenu("Collect Doors")]
	private void CollectDoors()
	{
		UnityEditor.Undo.RecordObject(this, "Collect Doors");
		doors.Clear();
		var allDoors = Utils.FindObjects<PropComponent>()
			.Where(p => p.CompareTag(Tags.TagDoor));
		var allDestructibles = Utils.FindObjects<Destructible>();
		
		foreach (var trigger in triggers)
		{
			var bounds = trigger.GetWorldBounds();
			bounds.Expand(0.1f); // for "contains" 
			foreach (var door in allDoors)
			{
				if (bounds.Contains(door.transform.position))
				{
					if (!doors.Contains(door.gameObject))
						doors.Add(door.gameObject);
				}
			}

			foreach (var destructible in allDestructibles)
			{
				if (bounds.Contains(destructible.transform.position))
				{
					if(!doors.Contains(destructible.gameObject))
						doors.Add(destructible.gameObject);
				}
			}
		}
		Utils.SetDirty(this);
	}
	
	private void OnDrawGizmosSelected()
	{
		foreach (var door in doors)
		{
			if (!door) continue;
			//Gizmos.DrawLine(transform.position, door.transform.position);
			if(door.TryGetComponent<PropComponent>(out var prop)) prop.DrawGizmoBox();
		}

		foreach (var trigger in triggers)
		{
			var bounds = trigger.GetWorldBounds();
			Gizmos.color = Utils.ColorAlpha(Color.yellow, 0.5f);
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}
	#endif

	private void Awake()
	{
		allRooms.Add(this);
	}

	private void OnDestroy()
	{
		allRooms.Remove(this);
	}

	private void OnEnable()
	{
		activeRooms.Add(this);
		//Debug.Log(this + " OnEnable activeRooms=" + activeRooms.Count);
		
		foreach (var doorObj in doors)
		{
			if(doorObj.TryGetComponent(out PropComponent door))
				door.onInteracted += Explore;
			if(doorObj.TryGetComponent(out Destructible destructible))
				destructible.breaks.AddListener(Explore);
		}
	}

	private void OnDisable()
	{
		activeRooms.Remove(this);
		//Debug.Log(this + " OnEnable activeRooms=" + activeRooms.Count);
		
		foreach (var doorObj in doors)
		{
			if(doorObj.TryGetComponent(out PropComponent door))
				door.onInteracted -= Explore;
			if(doorObj.TryGetComponent(out Destructible destructible))
				destructible.breaks.RemoveListener(Explore);
		}
	}
}