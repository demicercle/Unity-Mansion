using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class RoomTrigger : MonoBehaviour
{
	public static List<RoomTrigger> activeRooms = new List<RoomTrigger>();
	
	public static bool IsCharacterVisible(CharacterController character)
	{
		if (activeRooms.Any(room => room.HasCharacter(character)))
			return false;
		
		return true;
	}

	public static System.Action onChanged;
	
	[Header("Settings")]
	[SerializeField] private List<PropComponent> doors = new List<PropComponent>();
	[SerializeField] private List<CharacterTrigger> triggers =  new List<CharacterTrigger>();
	[SerializeField] private float ceilingHeight = 0.1f;
	[SerializeField] private bool createCeilings;
	
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
	
	public bool HasCharacter(CharacterController character)
	{
		return triggers.Any(t => t.HasCharacter(character));
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
		foreach (var trigger in triggers)
		{
			var bounds = trigger.GetWorldBounds();
			bounds.Expand(0.1f); // for "contains" 
			foreach (var door in allDoors)
			{
				if (bounds.Contains(door.transform.position))
				{
					if (!doors.Contains(door))
						doors.Add(door);
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
			door.DrawGizmoBox();
		}

		foreach (var trigger in triggers)
		{
			var bounds = trigger.GetWorldBounds();
			Gizmos.color = Utils.ColorAlpha(Color.yellow, 0.5f);
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}
	#endif

	private void OnEnable()
	{
		activeRooms.Add(this);
		Debug.Log(this + " OnEnable activeRooms=" + activeRooms.Count);
		
		foreach (var door in doors)
		{
			door.onInteracted += Explore;
		}
	}

	private void OnDisable()
	{
		activeRooms.Remove(this);
		Debug.Log(this + " OnEnable activeRooms=" + activeRooms.Count);
		
		foreach (var door in doors)
		{
			door.onInteracted -= Explore;
		}
	}

	private void Start()
	{
		CreateCeilings();
	}

	private void CreateCeilings()
	{
		if (!createCeilings)
			return;
		
		var rebuildNav = ceilings.Count > 0;
		while (ceilings.Count > 0)
		{
			Destroy(ceilings[0].gameObject);
			ceilings.RemoveAt(0);
		}
		
		foreach (var trigger in triggers)
		{
			var boxCollider = trigger.GetComponent<BoxCollider>();
			if(!boxCollider) Debug.LogWarning(this + " invalid collider for " + trigger);
			else
			{
				var newSize = boxCollider.size;
				
				var newCenter = boxCollider.center;
				newCenter.y += newSize.y / 2;
				newSize.y = ceilingHeight;
				
				var ceiling = new GameObject("ceiling_" + ceilings.Count).AddComponent<BoxCollider>();
				ceiling.gameObject.layer = Layers.LayerWall;
				ceiling.transform.SetParent(trigger.transform, false);
				ceiling.isTrigger = false;
				ceiling.size = newSize;
				ceiling.center = newCenter;
				
				ceilings.Add(ceiling);
			}
		}

		if(rebuildNav) 
			NavmeshRebuilder.RebuildNavigationOn(gameObject);
	}
}