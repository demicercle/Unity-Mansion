using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private GameObject tooltipObject;
	[SerializeField] private TMPro.TMP_Text tooltipLabel;

	private string GetLabel()
	{
		if (inventoryButton)
			return "";

		else return "";
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerOver = true;
		
		if (tooltipLabel)
		{
			if (inventoryButton)
				tooltipLabel.text = inventoryButton.GetTooltipDescription();
		}

		if (tooltipObject)
		{
			tooltipObject.SetActive(!string.IsNullOrEmpty(tooltipLabel.text));
			tooltipObject.transform.SetParent(parentCanvas.transform, true);
			tooltipObject.transform.SetAsLastSibling();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		pointerOver = false;
		
		if (tooltipObject)
		{
			tooltipObject.transform.SetParent(tooltipParent, true);
			tooltipObject.SetActive(false);
		}
	}

	private Canvas parentCanvas;
	private Transform tooltipParent;
	private InventoryButton inventoryButton;
	private bool pointerOver;

	private void Awake()
	{
		parentCanvas = GetComponentInParent<Canvas>();
		inventoryButton = GetComponent<InventoryButton>();

		if (tooltipObject)
		{
			tooltipParent = tooltipObject.transform.parent;
			tooltipObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		if (pointerOver)
			tooltipObject.transform.position = GameInput.Instance.GetMousePosition();
	}
}