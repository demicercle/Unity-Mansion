using System;
using UnityEngine;
using UnityEngine.UI;


public class InventoryPanel : MonoBehaviour
{
	[SerializeField] private InventoryButton[] buttons;

	[ContextMenu("Collect Buttons")]
	private void CollectButtons()
	{
		buttons = GetComponentsInChildren<InventoryButton>();
		Utils.SetDirty(this);
	}
	
	public void AssignInventory(Inventory newInventory)
	{
		Debug.Log(this + " AssignInventory " + newInventory);
		selfInventory = newInventory;
		if (!selfInventory)
		{
			gameObject.SetActive(false);
		}
		else
		{
			gameObject.SetActive(true);
			for (int i = 0; i < buttons.Length; i++)
			{
				buttons[i].SetInventory(selfInventory, i);
			}
		}
	}

	private Inventory selfInventory;
	private ToggleGroup toggleGroup;

	private void Awake()
	{
		toggleGroup = Utils.GetOrAddComponent<ToggleGroup>(gameObject);
		toggleGroup.allowSwitchOff = true;
		if(buttons == null || buttons.Length == 0)
			CollectButtons();
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].SetToggleGroup(toggleGroup);
		}
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