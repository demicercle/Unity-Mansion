using UnityEngine;
using UnityEngine.EventSystems;


public class UI_Viewport : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	static public bool mouseOverViewport { get; private set; }
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		mouseOverViewport = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		mouseOverViewport = false;
	}
}