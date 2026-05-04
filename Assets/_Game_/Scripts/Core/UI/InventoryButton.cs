using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InventoryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	private const float HoldDuration = 1.5f;
	
	[SerializeField] private ItemData itemData;
	[SerializeField] private Toggle toggle;
	[SerializeField] private TMP_Text itemLabel;
	[SerializeField] private RawImage iconImage;
	[SerializeField] private Image holdFill;
	[SerializeField] private TMP_Text qtyLabel;
	
	public void SetItem(ItemData newItem, Inventory inventory)
	{
		itemData = newItem;
		
		if (itemData)
		{
			toggle.interactable = true;
			
			iconImage.texture = itemData.GetIcon();
			iconImage.enabled = iconImage.texture;

			itemLabel.enabled = true;
			itemLabel.text = itemData.GetDisplayName();

			qtyLabel.enabled = itemData.ShowQuantity();
			qtyLabel.text = itemData.GetQuantity().ToString();
		}
		else
		{
			toggle.interactable = false;
			
			iconImage.texture = null;
			iconImage.enabled = false;
			
			itemLabel.enabled = true;
			itemLabel.text = string.Empty;

			qtyLabel.enabled = false;
			qtyLabel.text = string.Empty;
		}

		selfInventory = inventory;
	}

	public void SetToggleGroup(ToggleGroup toggleGroup)
	{
		toggle.group = toggleGroup;
		toggle.isOn = false;
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		hold = false;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
			hold = true;
		else
			hold = false;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		hold = false;
	}

	private bool hold;
	private float pressTime;
	private Inventory selfInventory;
	
	private void Awake()
	{
		toggle.isOn = false;
		toggle.onValueChanged.AddListener(OnToggleChanged);
		SetItem(null, null);
	}

	private void OnDestroy()
	{
		toggle.onValueChanged.RemoveListener(OnToggleChanged);
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
		toggle.interactable = itemData && GameTurnManager.Instance.IsPlayerTurn();
		toggle.isOn = itemData && toggle.interactable && GameInput.Instance.GetItemToUse() == itemData;

		if (hold && itemData)
			pressTime += Time.deltaTime;
		else
			pressTime = 0;
		
		holdFill.fillAmount = Mathf.Clamp01(pressTime / HoldDuration);
		holdFill.enabled = pressTime > 0;
		
		if (pressTime / HoldDuration >= 1f)
		{
			pressTime = 0;
			if (itemData && selfInventory 
			             && selfInventory.TryCreateLootBag(itemData, out var bag))
			{
				if (selfInventory.TryRemoveItem(itemData))
				{
					SetItem(null, selfInventory);
					if (Inventory.debugMode > 0) Debug.Log(this + " Created Loot Bag " + bag);
				}
				else
				{
					Debug.LogWarning(this + " !TryRemoveItem "  + itemData + " from inventory " + selfInventory);
				}
			}
			else
			{
				Debug.LogWarning(this + " CreateLootBag not possible");
			}
		}
	}
}