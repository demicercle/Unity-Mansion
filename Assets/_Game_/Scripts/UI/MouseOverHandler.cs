using System;
using UnityEngine;
using UnityEngine.EventSystems;


public class MouseOverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public event System.Action<bool> onMouseOverChanged;
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		onMouseOverChanged?.Invoke(true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		onMouseOverChanged?.Invoke(false);
	}

	private void OnDestroy()
	{
		onMouseOverChanged = null;
	}
}