using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class GameCanvas : MonoBehaviour
{
	public RectTransform viewportRect;
	public Toggle controlPanel;
	public Button button_actionRun;
	public Button button_endTurn;
	public CharacterPortrait[] portraits;
	
	public bool mouseOverViewport { get; private set; }
	
	void Awake()
	{
		var view = GameView.GetInstance();
		button_endTurn.onClick.AddListener( () => view.ClickEndTurn());
		button_actionRun.onClick.AddListener(() => view.ClickRunAction());

		Utils.GetOrAddComponent<MouseOverHandler>(viewportRect.gameObject).onMouseOverChanged +=
			(mouseOver) => mouseOverViewport = mouseOver;
		
		if(portraits.Length == 0)
			portraits = GetComponentsInChildren<CharacterPortrait>();
	}
	
	public void UpdateUI(GameView view)
	{
		var party = CharacterController.GetPartyCharacters().ToArray();
		for (int i = 0; i < portraits.Length; i++)
		{
			portraits[i].UpdateUI(i < party.Length ? party[i] : null);
		}
		
		controlPanel.isOn = view.IsControlPanelVisible();
		button_actionRun.interactable = view.CanUseRunAction();
		button_endTurn.interactable = view.CanEndTurn();
	}
}