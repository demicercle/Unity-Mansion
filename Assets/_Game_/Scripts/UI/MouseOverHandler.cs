using System;
using UnityEngine;
using UnityEngine.EventSystems;


public class MouseOverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
	
	public event System.Action onClicked;
	public void OnPointerClick(PointerEventData eventData)
	{
		onClicked?.Invoke();
	}
	
	private void OnDestroy()
	{
		onMouseOverChanged = null;
	}
}