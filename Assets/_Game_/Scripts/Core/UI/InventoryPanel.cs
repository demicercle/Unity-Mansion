using System;
using UnityEngine;


public class InventoryPanel : MonoBehaviour
{
	[SerializeField] private InventoryButton[] buttons;

	[ContextMenu("Collect Buttons")]
	private void CollectButtons()
	{
		buttons = GetComponentsInChildren<InventoryButton>();
		Utils.SetDirty(this);
	}
	
	public void AssignInventory(Inventory inventory)
	{
		selfInventory = inventory;
		if (!selfInventory)
		{
			gameObject.SetActive(false);
		}
		else
		{
			gameObject.SetActive(true);
			for (int i = 0; i < buttons.Length; i++)
			{
				if (selfInventory.TryGetItem(i, out var itemData))
				{
					buttons[i].SetItem(itemData);
				}
				else
				{
					buttons[i].SetItem(null);
				}
			}
		}
	}

	private Inventory selfInventory;

	private void Awake()
	{
		AssignInventory(null);
	}

	private void OnEnable()
	{
		Inventory.onAnyInventoryChanged += OnAnyInventoryChanged;
	}

	private void OnDisable()
	{
		Inventory.onAnyInventoryChanged -= OnAnyInventoryChanged;
	}

	private void OnAnyInventoryChanged(Inventory other)
	{
		if (other == selfInventory)
		{
			AssignInventory(other);
		}
	}
}