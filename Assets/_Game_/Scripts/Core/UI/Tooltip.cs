using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private bool followMousePosition = true;
	[SerializeField] private GameObject tooltipObject;
	[SerializeField] private TMPro.TMP_Text tooltipLabel;
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		pointerOver = true;
		
		if (tooltipLabel)
		{
			if (inventoryButton)
				tooltipLabel.text = inventoryButton.GetTooltipDescription();

			if (actionButton)
				tooltipLabel.text = actionButton.GetTooltipDescription(); // TODO Interface "ITooltipDescription"
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
	private ActionButton actionButton;
	private bool pointerOver;

	private void Awake()
	{
		parentCanvas = GetComponentInParent<Canvas>();
		inventoryButton = GetComponent<InventoryButton>();
		actionButton = GetComponent<ActionButton>();

		if (tooltipObject)
		{
			tooltipParent = tooltipObject.transform.parent;
			tooltipObject.SetActive(false);
		}
	}

	private void LateUpdate()
	{
		if (pointerOver && followMousePosition)
			tooltipObject.transform.position = GameInput.Instance.GetMousePosition();
	}
}