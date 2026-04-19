using System;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[System.Serializable]
public struct UILabelWrapper
{
	[SerializeField] private TMPro.TMP_Text tmpText;
	public GameObject GetGameObject() => tmpText.gameObject;

	public UILabelWrapper SetActive(bool value = true)
	{
		GetGameObject().SetActive(value);
		return this;
	}

	public UILabelWrapper SetWorldPosition(Vector3 position)
	{
		GetGameObject().transform.position = Camera.main.WorldToScreenPoint(position);
		return this;
	}

	public UILabelWrapper SetFollows(Transform target)
	{
		Utils.GetOrAddComponent<ScreenFollowsWorld>(GetGameObject()).target = target;
		return this;
	}

	public UILabelWrapper SetParent(Transform parent)
	{
		GetGameObject().transform.SetParent(parent);
		return this;
	}
	
	public UILabelWrapper SetText(string newText)
	{
		tmpText.text = newText;
		return this;
	}

	public UILabelWrapper Duplicate()
	{
		return new UILabelWrapper()
		{
			tmpText = Object.Instantiate(tmpText.gameObject).GetComponent<TMPro.TMP_Text>(),
		};
	}
}

public class GameCanvas : MonoBehaviour
{
	static private GameCanvas instance;

	static public GameCanvas Instance
	{
		get
		{
			if (!instance) instance = Utils.FindFirstObject<GameCanvas>();
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
	public UILabelWrapper floatingText;
	
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

	public UILabelWrapper FloatingText(Object obj, string label)
	{
		if (obj is Component)
		{
			return floatingText.Duplicate().SetParent(transform).SetFollows((obj as Component).transform).SetText(label).SetActive();
		}
		
		Debug.LogWarning(this + " invalid floating text object: " + obj + " label=" + label);
		return default;
	}
	
	public UILabelWrapper FloatingText(Vector3 worldPosition, string label)
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