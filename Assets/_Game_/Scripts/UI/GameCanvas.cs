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

	private ColorBlock endTurnColors;
	private Color normalColor;

	public void SetRenderTexture(RenderTexture rt)
	{
		var rawImage = viewportRect.GetComponentInChildren<RawImage>();
		rawImage.texture = rt;
		rawImage.enabled = true;
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
		#if UNITY_EDITOR
		mouseHandler.onClicked += () => GameView.GetInstance().OnApplicationFocus(Application.isEditor); // TODO
		#endif
		
		if(portraits.Length == 0)
			portraits = GetComponentsInChildren<CharacterPortrait>();

		endTurnColors = button_endTurn.colors;
		normalColor = endTurnColors.normalColor;
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
		
		if (view.CanEndTurn())
		{
			button_endTurn.interactable = true;
		}
		else
		{
			button_endTurn.interactable = false;
		}
		
		if (view.ShouldEndTurn())
		{
			endTurnColors.normalColor = Color.Lerp(Color.clear, normalColor, Mathf.PingPong(Time.time*Mathf.PI, 1));
			button_endTurn.transform.localScale = Vector3.Lerp(Vector3.one * 1.1f, Vector3.one, Mathf.PingPong(Time.time*Mathf.PI, 1));
		}
		else
		{
			endTurnColors.normalColor = normalColor;
			button_endTurn.transform.localScale = Vector3.one;
		}
		
		button_endTurn.colors = endTurnColors;
	}
}