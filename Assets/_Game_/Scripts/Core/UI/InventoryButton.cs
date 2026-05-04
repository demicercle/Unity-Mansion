using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InventoryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] private ItemData itemData;
	[SerializeField] private float minTimeToHold = 0.5f;
	[SerializeField] private float holdDuration = 0.5f;
	[SerializeField] private Toggle toggleSelected, toggleDrop;
	[SerializeField] private TMP_Text itemLabel;
	[SerializeField] private RawImage iconImage;
	[SerializeField] private Image holdFill;
	[SerializeField] private TMP_Text qtyLabel;

	public void SetInventory() => SetInventory(selfInventory, selfIndex);
	public void SetInventory(Inventory inventory, int index)
	{
		selfInventory = inventory;
		selfIndex = index;
		
		if (selfInventory && !selfInventory.TryGetItem(index, out itemData))
		{
			toggleSelected.interactable = false;
			
			iconImage.texture = null;
			iconImage.enabled = false;
			
			itemLabel.enabled = true;
			itemLabel.text = string.Empty;

			qtyLabel.enabled = false;
			qtyLabel.text = string.Empty;
		}
		else
		{
			toggleSelected.interactable = true;
			
			iconImage.texture = itemData.GetIcon();
			iconImage.enabled = iconImage.texture;

			itemLabel.enabled = true;
			itemLabel.text = itemData.GetDisplayName();

			qtyLabel.enabled = itemData.ShowQuantity();
			qtyLabel.text = itemData.GetQuantity().ToString();
		}
	}

	public void SetToggleGroup(ToggleGroup toggleGroup)
	{
		toggleSelected.group = toggleGroup;
		toggleSelected.isOn = false;
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (Locked()) return;
		//Debug.Log(this + " OnPointerEnter");
		GameInput.Instance.SetMouseOverItem(itemData);
		mouseOverIndex = selfIndex;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (Locked()) return;
		//Debug.Log(this + " OnPointerExit");
		GameInput.Instance.SetMouseOverItem(null);
		hold = false;
		mouseOverIndex = -1;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (Locked()) return;
		//Debug.Log(this + " OnPointerDown");
		hold = true;
		dropOnGround = eventData.button == PointerEventData.InputButton.Right;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (Locked()) return;
		Debug.Log(this + " OnPointerUp");
		hold = false;
		
		if (draggedIndex >= 0)
		{
			if (mouseOverIndex >= 0)
			{
				// try drop here
				selfInventory.SwapIndex(mouseOverIndex, draggedIndex);
				GameInput.Instance.SetDraggedItem(null);
			}
			else
			{
				GameInput.Instance.AssignDropItem();
			}
			
			draggedIndex = -1;
			mouseOverIndex = -1;
		}
	}

	public bool Dragged()
	{
		return hold;
	}

	public bool Empty()
	{
		return itemData == null;
	}

	public bool Locked() => selfInventory && selfInventory.Locked;

	private static int draggedIndex = -1;
	private static int mouseOverIndex = -1;
	private bool hold;
	private bool dropOnGround;
	private float pressTime;
	private Inventory selfInventory;
	private int selfIndex;
	
	private void Awake()
	{
		toggleSelected.isOn = false;
		toggleSelected.onValueChanged.AddListener(OnToggleChanged);
		toggleDrop.isOn = false;
		toggleDrop.interactable = false;
		SetInventory();
	}

	private void OnDestroy()
	{
		toggleSelected.onValueChanged.RemoveListener(OnToggleChanged);
	}

	private void OnToggleChanged(bool isOn)
	{
		//Debug.LogWarning(this + " " + itemData + " ToggleChanged " + isOn + " TODO");
		//toggle.isOn = false;
		if (isOn && itemData && itemData.IsUsable())
		{
			GameInput.Instance.SetItemToUse(itemData, true);
		}
	}

	private void LateUpdate()
	{
		toggleSelected.interactable = itemData && GameTurnManager.Instance.IsPlayerTurn() && selfInventory && !selfInventory.Locked;
		toggleSelected.isOn = itemData && toggleSelected.interactable && GameInput.Instance.GetItemToUse() == itemData;
	
		toggleDrop.isOn = mouseOverIndex >= 0 && mouseOverIndex == selfIndex && (itemData || draggedIndex >= 0);

		iconImage.color = Utils.ColorAlpha(iconImage.color, GameInput.Instance.IsDragged(itemData) ? 0.5f : 1.0f);

		if (hold && itemData)
			pressTime += Time.deltaTime;
		else
			pressTime = 0;
		
		holdFill.fillAmount = Mathf.Clamp01( (pressTime - minTimeToHold) / (holdDuration - minTimeToHold));
		holdFill.enabled = pressTime > minTimeToHold;

		if (holdFill.enabled)
		{
			toggleSelected.isOn = false;
			toggleSelected.interactable = false;
		}
		
		if (pressTime >= holdDuration)
		{
			pressTime = 0;
			hold = false;
			toggleSelected.isOn = false;

			if (itemData && selfInventory)
			{
				if (dropOnGround)
				{
					if (selfInventory.TryCreateLootBag(itemData, out var bag))
					{
						if (selfInventory.TryRemoveItem(itemData))
						{
							if (Inventory.debugMode > 0) Debug.Log(this + " Created Loot Bag " + bag);
						}
						else
						{
							Debug.LogWarning(this + " !TryRemoveItem "  + itemData + " from inventory " + selfInventory);
						}
					}
				}
				else
				{
					// start drag
					draggedIndex = selfIndex;
					GameInput.Instance.SetDraggedItem(itemData);
				}
			}
		}

		if (!GameInput.Instance.GetDraggedItem() && draggedIndex == selfIndex)
		{
			draggedIndex = -1;
		}
	}
}