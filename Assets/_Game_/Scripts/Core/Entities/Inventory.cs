using System;
using System.Collections.Generic;
using UnityEngine;


public class Inventory : MonoBehaviour
{
	public static event System.Action<Inventory> onAnyInventoryChanged;
	
	[SerializeField] private List<ItemData> items = new List<ItemData>();
	[SerializeField] [Range(1, 32)] private int maxSize = 6;

	public int GetMaxSize() => maxSize;
	
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
}