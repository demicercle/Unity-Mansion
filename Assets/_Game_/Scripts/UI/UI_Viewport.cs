using UnityEngine;
using UnityEngine.EventSystems;


public class UI_Viewport : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public void OnPointerEnter(PointerEventData eventData)
	{
		GameView.GetInstance().mouseOverViewport = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		GameView.GetInstance().mouseOverViewport = false;
	}
}