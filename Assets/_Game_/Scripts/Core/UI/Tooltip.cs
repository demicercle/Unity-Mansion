using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[SerializeField] private GameObject tooltipObject;
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		if (tooltipObject)
		{
			tooltipObject.SetActive(true);
			tooltipObject.transform.SetParent(parentCanvas.transform, true);
			tooltipObject.transform.SetAsLastSibling();
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (tooltipObject)
		{
			tooltipObject.transform.SetParent(tooltipParent, true);
			tooltipObject.SetActive(false);
		}
	}

	private Canvas parentCanvas;
	private Transform tooltipParent;

	private void Awake()
	{
		parentCanvas = GetComponentInParent<Canvas>();

		if (tooltipObject)
		{
			tooltipParent = tooltipObject.transform.parent;
			tooltipObject.SetActive(false);
		}
	}
}