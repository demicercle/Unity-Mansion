using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class GameCanvas : MonoBehaviour
{
	static private GameCanvas instance;
	static public GameCanvas Instance => instance;
	
	public RectTransform viewportRect;
	public Toggle controlPanel;
	public Button button_actionRun;
	public Button button_endTurn;
	public CharacterPortrait[] portraits;

	public void SetRenderTexture(RenderTexture rt)
	{
		viewportRect.GetComponentInChildren<RawImage>().texture = rt;
	}
	
	public bool mouseOverViewport { get; private set; }
	
	void Awake()
	{
		instance = this;
		
		var view = GameView.GetInstance();
		button_endTurn.onClick.AddListener( () => view.ClickEndTurn());
		button_actionRun.onClick.AddListener(() => view.ClickRunAction());

		var mouseHandler = Utils.GetOrAddComponent<MouseOverHandler>(viewportRect.gameObject);
		mouseHandler.onMouseOverChanged += (mouseOver) => mouseOverViewport = mouseOver;
		mouseHandler.onClicked += () => GameView.GetInstance().OnApplicationFocus(Application.isEditor); // TODO
		
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