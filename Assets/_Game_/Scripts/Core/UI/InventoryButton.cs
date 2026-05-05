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
	[SerializeField] private bool isWeapon;

	public void SetInventory() => SetInventory(selfInventory, selfIndex);
	public void SetInventory(Inventory inventory, int index)
	{
		selfInventory = inventory;
		selfIndex = index;
		
		if (selfInventory && selfInventory.TryGetItem(index, out itemData))
		{
			if(toggleSelected) toggleSelected.interactable = true;
			
			iconImage.texture = itemData.GetIcon();
			iconImage.enabled = iconImage.texture;

			itemLabel.enabled = true;
			itemLabel.text = itemData.GetDisplayName();

			qtyLabel.enabled = itemData.ShowQuantity();
			qtyLabel.text = itemData.GetQuantity().ToString();
		}
		else
		{
			if(toggleSelected) toggleSelected.interactable = false;
			
			iconImage.texture = null;
			iconImage.enabled = false;
			
			itemLabel.enabled = true;
			itemLabel.text = string.Empty;

			qtyLabel.enabled = false;
			qtyLabel.text = string.Empty;
		}
	}

	public void SetToggleGroup(ToggleGroup toggleGroup)
	{
		if (toggleSelected)
		{
			toggleSelected.group = toggleGroup;
			toggleSelected.isOn = false;
		}
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
//		Debug.Log(this + " OnPointerUp " + draggedIndex + " " + mouseOverIndex);
		hold = false;

		var useItem = GameInput.Instance.GetItemToUse();
		if (useItem && mouseOverIndex == selfIndex)
		{
			if (mouseOverIndex == Inventory.InventorySlot_Weapon && itemData
			                                                     && itemData.IsWeapon(out var reloadWeapon) 
			                                                     && reloadWeapon.CanBeReloadedWithAmmo(useItem))
			{
				if (selfInventory.TryGetComponent(out CharacterEntity selfCharacter))
				{
					GameInput.Instance.SetItemToUse(null, true); // assign reload
					selfCharacter.Routines.AssignReloadWeapon(reloadWeapon, useItem);
					return;
				}
			}
		}
		
		if (draggedIndex >= 0)
		{
			if (mouseOverIndex >= 0)
			{
				// try drop here
				var draggedItem = GameInput.Instance.GetDraggedItem();
				
				Debug.Log("try drop " + draggedItem + " into " + mouseOverIndex);
				
				
				 if (mouseOverIndex != Inventory.InventorySlot_Weapon || draggedItem.IsWeapon(out var _))
				{
					selfInventory.SwapIndex(mouseOverIndex, draggedIndex);
					GameInput.Instance.SetDraggedItem(null);
				}
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
		if (toggleSelected)
		{
			toggleSelected.isOn = false;
			toggleSelected.onValueChanged.AddListener(OnToggleChanged);
		}
		
		toggleDrop.isOn = false;
		toggleDrop.interactable = false;
	}

	private void OnDestroy()
	{
		if (toggleSelected) toggleSelected.onValueChanged.RemoveListener(OnToggleChanged);
	}
	
	private void OnEnable()
	{
		WeaponData.onWeaponChanged += OnWeaponChanged;
		SetInventory();
	}

	private void OnDisable()
	{
		WeaponData.onWeaponChanged -= OnWeaponChanged;
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

	private void OnWeaponChanged(WeaponData otherWeapon)
	{
		if (itemData && itemData.IsWeapon(out var selfWeapon) && selfWeapon == otherWeapon)
		{
			SetInventory();
		}
	}

	private void LateUpdate()
	{
		if (toggleSelected)
		{
			toggleSelected.interactable = itemData && GameTurnManager.Instance.IsPlayerTurn() && selfInventory && !selfInventory.Locked;
			toggleSelected.isOn = itemData && toggleSelected.interactable && GameInput.Instance.GetItemToUse() == itemData;
		}
	
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
			if (toggleSelected)
			{
				toggleSelected.isOn = false;
				toggleSelected.interactable = false;
			}
		}
		
		if (pressTime >= holdDuration)
		{
			pressTime = 0;
			hold = false;
			if(toggleSelected) toggleSelected.isOn = false;

			if (itemData && selfInventory)
			{
				if (dropOnGround)
				{
					if (!selfInventory.TryCreateLootBag(itemData, out var bag) || !selfInventory.TryRemoveItem(itemData))
					{
						Debug.LogWarning(this + " unable to drop item correctly");
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

	public string GetTooltipDescription()
	{
		return !itemData ? string.Empty : itemData.GetTooltipDescription();
	}

}