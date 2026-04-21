using System;
using System.Collections;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;


public class GameCanvas : MonoBehaviour
{
	private static GameCanvas instance;

	public static GameCanvas Instance => instance;

	public event System.Action onViewportClicked;
	
	public event System.Action<bool> onViewportMouseOver;
	
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
	
	//public bool mouseOverViewport { get; private set; }

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
	private MouseOverHandler viewportMouseHandler;
	
	private void Awake()
	{
		if (instance) Debug.LogError(this + " duplicated!");
		instance = this;
		
		button_endTurn.onClick.AddListener(ClickEndTurn);
		button_actionRun.onClick.AddListener(ClickRunAction);

		viewportMouseHandler = viewportRect.GetComponent<MouseOverHandler>();
		if(!viewportMouseHandler) viewportMouseHandler = viewportRect.gameObject.AddComponent<MouseOverHandler>();
		viewportMouseHandler.onMouseOverChanged += OnViewportRectMouseOver;
		viewportMouseHandler.onClicked += OnViewportRectClicked;
		
		floatingText.SetActive(false);

		CharacterController.partyChanged += UpdatePortraits;

		gameTurnManager.onLoadingGame += OnLoadingGame;
	}

	private void OnDestroy()
	{
		button_endTurn.onClick.RemoveListener( ClickEndTurn);
		button_actionRun.onClick.RemoveListener(ClickRunAction);
		
		viewportMouseHandler.onMouseOverChanged -= OnViewportRectMouseOver;
		viewportMouseHandler.onClicked -= OnViewportRectClicked;
		
		CharacterController.partyChanged -= UpdatePortraits;
		
		gameTurnManager.onLoadingGame -= OnLoadingGame;
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
		UpdateButtons(); // TODO with events
	}
	
	private void OnLoadingGame(bool isLoading) => gameObject.SetActive(!isLoading);

	private void OnViewportRectClicked()
	{
		onViewportClicked?.Invoke();
	}

	private void OnViewportRectMouseOver(bool value)
	{
		onViewportMouseOver?.Invoke(value);
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
		controlPanel.isOn = gameTurnManager.SelectedCharacterIsControllable();
		button_actionRun.interactable = gameTurnManager.SelectedCharacterCanUseRunAction();
		
		if (gameTurnManager.PlayerCanEndTurn())
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
				animator.SetLayerWeight(endTurnLayer, Mathf.Lerp(animator.GetLayerWeight(endTurnLayer), gameTurnManager.PlayerShouldEndTurn() ? 1f : 0f, Time.unscaledDeltaTime * lerpSpeed));
		}
	}
	#endregion
}