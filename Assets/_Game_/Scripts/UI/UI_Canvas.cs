using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class UI_Canvas : MonoBehaviour
{
	public Toggle controlPanel;
	public Button button_actionRun;
	public Button button_endTurn;
	public UI_Portrait[] portraits;
	
	void Awake()
	{
		var view = GameView.GetInstance();
		button_endTurn.onClick.AddListener( () => view.ClickEndTurn());
		button_actionRun.onClick.AddListener(() => view.ClickRunAction());
		
		if(portraits.Length == 0)
			portraits = GetComponentsInChildren<UI_Portrait>();
	}
	
	public void UpdateUI(GameView view)
	{
		var party = CharacterController.GetPartyCharacters().ToArray();
		for (int i = 0; i < portraits.Length; i++)
		{
			portraits[i].UpdateUI(i < portraits.Length ? party[i] : null);
		}
		
		controlPanel.isOn = view.IsControlPanelVisible();
		button_actionRun.interactable = view.CanUseRunAction();
		button_endTurn.interactable = view.CanEndTurn();
	}
}