using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class RoomTrigger : MonoBehaviour
{
	static public List<RoomTrigger> activeRooms = new List<RoomTrigger>();
	
	[Header("Settings")]
	[SerializeField] private List<PropComponent> doors = new List<PropComponent>();
	[SerializeField] private List<CharacterTrigger> triggers =  new List<CharacterTrigger>();
	[SerializeField] private float ceilingHeight = 0.1f;
	
	private List<BoxCollider> ceilings = new List<BoxCollider>();
	
	public bool HasCharacter(CharacterController character)
	{
		return triggers.Any(t => t.HasCharacter(character));
	}
	
	[ContextMenu("Collect Triggers")]
	void CollectTriggers()
	{
		triggers.Clear();
		triggers.AddRange(GetComponentsInChildren<CharacterTrigger>());
		Utils.SetDirty(this);
	}

	void OnDrawGizmosSelected()
	{
		foreach (var door in doors)
		{
			if (!door) continue;
			//Gizmos.DrawLine(transform.position, door.transform.position);
			door.DrawGizmoBox();
		}
	}

	void OnEnable()
	{
		activeRooms.Add(this);
		
		foreach (var door in doors)
		{
			door.interacted.AddListener(OnDoorOpened);
		}
	}

	void OnDisable()
	{
		activeRooms.Remove(this);
		
		foreach (var door in doors)
		{
			door.interacted.RemoveListener(OnDoorOpened);
		}
	}

	void Start()
	{
		CreateCeilings();
	}

	void OnDoorOpened()
	{
		gameObject.SetActive(false);
	}

	void CreateCeilings()
	{
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
				ceiling.gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_WALL);
				ceiling.transform.SetParent(trigger.transform, false);
				ceiling.isTrigger = false;
				ceiling.size = newSize;
				ceiling.center = newCenter;
				
				ceilings.Add(ceiling);
			}
		}

		if(rebuildNav) NavUtility.RebuildNavigationAsync(0.1f);
	}
}