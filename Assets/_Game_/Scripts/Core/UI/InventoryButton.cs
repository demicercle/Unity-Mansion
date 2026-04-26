using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class InventoryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private ItemData itemData;
	[SerializeField] private Toggle toggle;
	[SerializeField] private TMP_Text itemLabel;
	[SerializeField] private RawImage iconImage;
	[SerializeField] private RectTransform tooltip;

	[ContextMenu("SetItem")]
	public void SetItem() => SetItem(itemData);
	
	public void SetItem(ItemData newItem)
	{
		itemData = newItem;
		
		if (itemData)
		{
			if(iconImage) iconImage.texture = itemData.GetIcon();
			if (itemLabel) itemLabel.text = itemData.GetDisplayName();
		}
		else
		{
			iconImage.texture = null;
			itemLabel.text = string.Empty;
		}

		toggle.interactable = itemData != null;
		iconImage.enabled = iconImage.texture;
		itemLabel.enabled = !string.IsNullOrWhiteSpace(itemLabel.text);
		
		if(!Application.isPlaying)
			Utils.SetDirty(this);
	}

	private void Awake()
	{
		toggle.isOn = false;
		toggle.onValueChanged.AddListener(OnToggleChanged);
		
		SetItem();
	}

	private void OnDestroy()
	{
		toggle.onValueChanged.RemoveListener(OnToggleChanged);
	}

	private void OnToggleChanged(bool isOn)
	{
		//Debug.LogWarning(this + " " + itemData + " ToggleChanged " + isOn + " TODO");
		//toggle.isOn = false;
		if (isOn && itemData)
		{
			GameInput.Instance.SetItemToUse(itemData);
		}
	}

	private void LateUpdate()
	{
		toggle.interactable = itemData && GameTurnManager.Instance.IsPlayerTurn();
		toggle.isOn = itemData && toggle.interactable && GameInput.Instance.GetItemToUse() == itemData;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if(tooltip) tooltip.gameObject.SetActive(true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if(tooltip) tooltip.gameObject.SetActive(false);
	}

	public void SetToggleGroup(ToggleGroup toggleGroup)
	{
		toggle.group = toggleGroup;
		toggle.isOn = false;
	}
}