using UnityEngine;
using UnityEngine.EventSystems;


public class UI_Viewport : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	private GameView gameView;

	void Awake()
	{
		gameView = GameView.GetInstance();
	}
	
	public void OnPointerEnter(PointerEventData eventData)
	{
		if(gameView) gameView.mouseOverViewport = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if(gameView) gameView.mouseOverViewport = false;
	}
}