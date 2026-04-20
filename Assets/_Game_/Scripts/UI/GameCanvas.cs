using System;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;


public class GameCanvas : MonoBehaviour
{
	static private GameCanvas instance;

	static public GameCanvas Instance
	{
		get
		{
			if (!instance) instance = Utils.FindFirstObject<GameCanvas>();
			if (!instance) Instantiate(Resources.Load<GameObject>("GameCanvas")).TryGetComponent(out instance);
			if (!instance) Debug.LogError("No Game Canvas!");
			return instance;
		}
	}
	
	[Header("References")]
	public RectTransform viewportRect;
	public Toggle controlPanel;
	public Button button_actionRun;
	public Button button_endTurn;
	public CharacterPortrait[] portraits;
	public TextReference floatingText;
	
	[Header("Animation")]
	public Animator animator;
	public float lerpSpeed;
	public int endTurnLayer;

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
		//#if UNITY_EDITOR
		mouseHandler.onClicked += () => GameView.GetInstance().OnApplicationFocus(true);
		//#endif
		
		if(portraits.Length == 0)
			portraits = GetComponentsInChildren<CharacterPortrait>();
		
		floatingText.SetActive(false);
	}

	public TextReference FloatingText(Object obj, string label)
	{
		if (obj is CharacterController)
		{
			return floatingText.Duplicate().SetParent(transform).SetText(label).SetActive()
				.SetFollows((obj as CharacterController).transform, (obj as CharacterController).GetCenterPosition(true));
		}
		
		if (obj is Component)
		{
			return floatingText.Duplicate().SetParent(transform).SetText(label).SetActive()
				.SetFollows((obj as Component).transform, default);
		}
		
		Debug.LogWarning(this + " invalid floating text object: " + obj + " label=" + label);
		return default;
	}
	
	public TextReference FloatingText(Vector3 worldPosition, string label)
	{
		return floatingText.Duplicate().SetParent(transform).SetWorldPosition(worldPosition).SetText(label).SetActive();
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

		if (animator && lerpSpeed > 0)
		{
			if(endTurnLayer > 0) 
				animator.SetLayerWeight(endTurnLayer, Mathf.Lerp(animator.GetLayerWeight(endTurnLayer), view.ShouldEndTurn() ? 1f : 0f, Time.unscaledDeltaTime * lerpSpeed));
		}
	}
}