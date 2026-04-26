using System;
using System.Collections.Generic;
using UnityEngine;


public class Inventory : MonoBehaviour
{
	public static event System.Action<Inventory> onAnyInventoryChanged;
	public static event System.Action<Inventory, ItemData> onAnyItemAdded, onAnyItemRemoved;
	
	[SerializeField] [Range(1, 32)] private int maxSize = 6;
	[SerializeField] private List<ItemData> items = new List<ItemData>();
	
	public int GetMaxSize() => maxSize;

	public void TransfertToSelectedCharacter()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter))
		{
			if (selectedCharacter.TryGetComponent(out Inventory otherInventory))
			{
				for (int i = 0; i < items.Count; i++)
				{
					if (items[i])
					{
						if (!otherInventory.TryAddItem(items[i]))
						{
							Debug.LogWarning(this + " inventory full " + otherInventory);
							return;
						}
						
						items[i] = null; // remove from this inventory
					}
				}
			}
		}
	}
	
	public bool TryGetItem(int index, out ItemData item)
	{
		return (item = index >= 0 && index < items.Count ? items[index] : null);
	}

	public bool TryAddItem(ItemData itemData)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (!items[i])
			{
				items[i] = itemData;
				onAnyItemAdded?.Invoke(this, items[i]);
				NotifyChanged();
				return true;
			}
		}

		return false;
	}
	
	[ContextMenu("Update Size")]
	private void UpdateSize()
	{
		while (items.Count > maxSize)
		{
			items.RemoveAt(items.Count - 1);
		}

		while (items.Count < maxSize)
		{
			items.Add(null);
		}
		
		Utils.SetDirty(this);
	}

	private void NotifyChanged()
	{
		onAnyInventoryChanged?.Invoke(this);
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		var label = "";
		
		foreach (var item in items)
		{
			if (item)
				label += item.name + "\n";
		}

		label = label.Trim('\n');
		
		if (!string.IsNullOrEmpty(label))
		{
			var bounds = new Bounds(Vector3.zero, transform.position);
			
			if (TryGetComponent(out Collider coll))
			{
				bounds = coll.bounds;
			}
			
			UnityEditor.Handles.Label(bounds.center, label);
		}
	}
#endif
}