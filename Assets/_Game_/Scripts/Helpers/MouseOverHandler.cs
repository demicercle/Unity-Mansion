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
	//public bool ignoreDebug = true;
	
	public void OnPointerClick(PointerEventData eventData)
	{
		//if (ignoreDebug && (DebugGUI.IsEnabled || IngameDebugConsole.DebugLogManager.Instance.IsLogWindowVisible)) return;
		onClicked?.Invoke();
	}
	
	private void OnDestroy()
	{
		onMouseOverChanged = null;
	}
}