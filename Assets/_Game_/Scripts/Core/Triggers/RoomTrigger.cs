using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class RoomTrigger : MonoBehaviour
{
	public static List<RoomTrigger> activeRooms = new List<RoomTrigger>();
	
	public static bool IsCharacterVisible(CharacterController character)
	{
		if (activeRooms.Any(t => t.HasCharacter(character)))
			return false;
		
		return true;
	}

	public static System.Action onChanged;
	
	[Header("Settings")]
	[SerializeField] private List<PropComponent> doors = new List<PropComponent>();
	[SerializeField] private List<CharacterTrigger> triggers =  new List<CharacterTrigger>();
	[SerializeField] private float ceilingHeight = 0.1f;
	
	private List<BoxCollider> ceilings = new List<BoxCollider>();

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
	
	[ContextMenu("Collect Triggers")]
	private void CollectTriggers()
	{
		triggers.Clear();
		triggers.AddRange(GetComponentsInChildren<CharacterTrigger>());
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
	}

	private void OnEnable()
	{
		activeRooms.Add(this);
		
		foreach (var door in doors)
		{
			door.onInteracted += Explore;
		}
	}

	private void OnDisable()
	{
		activeRooms.Remove(this);
		
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

		if(rebuildNav) 
			NavmeshRebuilder.RebuildNavigationOn(gameObject);
	}
}