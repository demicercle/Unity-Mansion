using System;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
			//if (!instance) Instantiate(Resources.Load<GameObject>("GameCanvas")).TryGetComponent(out instance);
			if (!instance) Debug.LogError("No Game Canvas!");
			return instance;
		}
	}
	
	[Header("Viewport")]
	[SerializeField] private RectTransform viewportRect;
	[SerializeField] private RawImage viewportOverlay;
	[Header("Buttons")]
	[SerializeField] private Toggle controlPanel;
	[SerializeField] private Button button_actionRun;
	[SerializeField] private Button button_endTurn;
	[Header("Portraits")]
	[SerializeField] private CharacterPortrait[] portraits;
	[Space]
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float lerpSpeed;
	[SerializeField] private int endTurnLayer;
	[Space]
	[SerializeField] private TextReference floatingText;
	
	public bool mouseOverViewport { get; private set; }

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

	public void SetOverlayRenderTexture(RenderTexture rt)
	{
		if(viewportOverlay.texture && viewportOverlay.texture != rt) Destroy(viewportOverlay.texture);
		viewportOverlay.texture = rt;
		viewportOverlay.enabled = true;
	}
	
	#region PRIVATE
	private static GameTurnManager gameTurnManager => GameTurnManager.Instance;

	private CharacterController[] partyCharacters;
	private MouseOverHandler mouseHandler;
	
	private void Awake()
	{
		instance = this;
		
		button_endTurn.onClick.AddListener( ClickEndTurn);
		button_actionRun.onClick.AddListener(ClickRunAction);

		mouseHandler = viewportRect.GetComponent<MouseOverHandler>();
		if(!mouseHandler) mouseHandler = viewportRect.gameObject.AddComponent<MouseOverHandler>();
		mouseHandler.onMouseOverChanged += OnMouseOverViewport;
		mouseHandler.onClicked += OnViewportClicked;
		
		floatingText.SetActive(false);

		CharacterController.partyChanged += UpdatePortraits;
	}

	private void OnDestroy()
	{
		button_endTurn.onClick.RemoveListener( ClickEndTurn);
		button_actionRun.onClick.RemoveListener(ClickRunAction);
		
		mouseHandler.onMouseOverChanged -= OnMouseOverViewport;
		mouseHandler.onClicked -= OnViewportClicked;
		
		CharacterController.partyChanged -= UpdatePortraits;
	}

	private void Start()
	{
		// event system checks
		var selfEventSystem = GetComponentInChildren<EventSystem>();
		if (selfEventSystem)
		{
			foreach (var otherEventSystem in FindObjectsByType<EventSystem>(default))
			{
				if (otherEventSystem != selfEventSystem)
				{
					Debug.Log(this + " destroys duplicated event system: " + selfEventSystem);
					Destroy(selfEventSystem.gameObject);
					break;
				}
			}
		}
	}

	private void LateUpdate()
	{
		UpdateButtons();
	}

	private void OnViewportClicked()
	{
		GameView.GetInstance().OnApplicationFocus(true);
	}

	private void OnMouseOverViewport(bool value)
	{
		mouseOverViewport = value;
	}

	private void ClickRunAction()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			selected.TryUseAction(CharacterAction.RunAction, true);
		}
	}

	private void ClickEndTurn() => gameTurnManager.TryEndPlayerTurn();

	private void UpdatePortraits()
	{
		if (portraits.Length == 0)
		{
			Debug.LogWarning(this + " has no portraits assigned in inspector!");
			return;
		}
		
		partyCharacters = CharacterController.GetPartyCharacters().ToArray();
		//Debug.Log(this + " UpdatePortraits partyCharacters=" + partyCharacters.Length);
		for (int i = 0; i < portraits.Length; i++)
		{
			portraits[i].AssignCharacter(i < partyCharacters.Length ? partyCharacters[i] : null);
		}
	}

	private void UpdateButtons()
	{
		controlPanel.isOn = IsControlPanelVisible();
		button_actionRun.interactable = CanUseRunAction();
		
		if (CanEndTurn())
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
				animator.SetLayerWeight(endTurnLayer, Mathf.Lerp(animator.GetLayerWeight(endTurnLayer), ShouldEndTurn() ? 1f : 0f, Time.unscaledDeltaTime * lerpSpeed));
		}
	}

	private bool IsControlPanelVisible() =>
		gameTurnManager.IsPlayerTurn() && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter.IsControllableByPlayer();
	
	private bool CanUseRunAction() =>
		!CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsPlayerTurn() && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) && selectedCharacter.CanUseAction(CharacterAction.RunAction);
	
	private bool ShouldEndTurn() => CanEndTurn() && CharacterController.GetPartyCharacters().All(pc =>
	{
		return !pc.HasActions() && pc.GetRemainingMovePoints() < 0.5f;
	});
	
	private bool CanEndTurn() => !CharacterRoutines.AnyActiveRoutines() && gameTurnManager.IsPlayerTurn();
	#endregion
}