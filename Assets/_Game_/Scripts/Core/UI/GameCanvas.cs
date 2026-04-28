using System;
using System.Collections;
using System.Collections.Generic;
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
	
	public GameLog gameLog => GetComponentInChildren<GameLog>();
	
	[Header("Viewport")]
	[SerializeField] private RectTransform viewportRect;
	[SerializeField] private RawImage viewportOverlay;
	[Header("Buttons")]
	[SerializeField] private Toggle toggleGroupMode;
	[SerializeField] private Toggle toggleTurnBased;
	[SerializeField] private Toggle toggleActions;
	[SerializeField] private Button button_actionRun;
	[SerializeField] private Button button_endTurn;
	[Header("Portraits")]
	[SerializeField] private CharacterPortrait[] portraits;
	[Header("Inventory")]
	[SerializeField] private InventoryPanel inventoryPanel;
	[SerializeField] private RawImage itemCursorImage;

	[Space] [Header("Animation")]
	[SerializeField] [Tooltip("Blend Weight Layer1")] private List<Animator> endTurnAnimators;
	[SerializeField] private int endTurnLayer;
	[SerializeField] private float lerpSpeed;
	[Space]
	[Header("References")]
	[SerializeField] private TextReference floatingText;
	[Space]
	[Header("UnityEvents")]
	public UnityEvent<bool> onPlayerTurnChanged;
	
	//public bool mouseOverViewport { get; private set; }

	public TextReference FloatingText(object obj, string label)
	{
		if (obj is CharacterEntity)
		{
			return floatingText.Duplicate().SetParent(transform).SetText(label).SetActive()
				.SetFollows((obj as CharacterEntity).transform, (obj as CharacterEntity).GetCenterPosition(true));
		}

		if (obj is IAttackable)
		{
			return floatingText.Duplicate().SetParent(transform).SetText(label).SetActive()
				.SetWorldPosition((obj as IAttackable).GetPosition());
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
	private static GameInput gameInput => GameInput.Instance;

	private CharacterEntity[] partyCharacters;
	private MouseOverHandler viewportMouseHandler;
	private ItemData currentItemToUse;
	
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

		CharacterEntity.onPartyChanged += UpdatePortraits;
		CharacterEntity.onAttackResult += OnAttackResult;
		CharacterEntity.onAnyCharacterDamaged += OnAnyCharacterDamaged;
		
		CharacterSelection.selectionChanged += OnSelectionChanged;
		
		Destructible.onAnyDestructibleHit += OnAnyDestructibleHit;
		
		gameTurnManager.onLoadingGame += OnLoadingGame;
		gameTurnManager.onPlayerChanged += OnPlayerChanged;
	}

	private void OnDestroy()
	{
		button_endTurn.onClick.RemoveListener( ClickEndTurn);
		button_actionRun.onClick.RemoveListener(ClickRunAction);
		
		viewportMouseHandler.onMouseOverChanged -= OnViewportRectMouseOver;
		viewportMouseHandler.onClicked -= OnViewportRectClicked;
		
		CharacterEntity.onPartyChanged -= UpdatePortraits;
		CharacterEntity.onAttackResult -= OnAttackResult;
		CharacterEntity.onAnyCharacterDamaged -= OnAnyCharacterDamaged;

		Destructible.onAnyDestructibleHit -= OnAnyDestructibleHit;
		
		CharacterSelection.selectionChanged -= OnSelectionChanged;
		
		gameTurnManager.onLoadingGame -= OnLoadingGame;
		gameTurnManager.onPlayerChanged -= OnPlayerChanged;
	}

	private void OnAnyDestructibleHit(Destructible arg1, DamageInfo arg2, object arg3)
	{
		FloatingText(arg1, Mathf.CeilToInt(arg2.amount).ToString());
	}

	private void OnAnyCharacterDamaged(CharacterEntity target, DamageInfo damageInfo, object objectFrom)
	{
		FloatingText(target, Mathf.CeilToInt(damageInfo.amount).ToString());
	}

	private void OnAttackResult(CharacterEntity arg1, IAttackable arg2, CharacterEntity.AttackResult arg3)
	{
		if (arg3 == CharacterEntity.AttackResult.Undefined)
			FloatingText(arg2, LanguageSettings.Main.Cant);
		else if (arg3 == CharacterEntity.AttackResult.Dodge)
			FloatingText(arg2, LanguageSettings.Main.Dodge);
		else if (arg3 == CharacterEntity.AttackResult.Miss)
			FloatingText(arg1, LanguageSettings.Main.Miss);
	}

	private void Start()
	{
		UpdateSelectables();
		
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
		UpdateSelectables(); // TODO with events

		currentItemToUse = gameInput.GetItemToUse();
		itemCursorImage.gameObject.SetActive(currentItemToUse);
		if (itemCursorImage.gameObject.activeSelf)
		{
			itemCursorImage.texture = currentItemToUse.GetIcon();
			itemCursorImage.color = Utils.ColorAlpha(Color.white, gameInput.IsBlocked() ? 0.30f : 0.90f);
			itemCursorImage.transform.position = gameInput.GetMousePosition();
			itemCursorImage.transform.SetAsLastSibling();
		}
	}

	private void OnPlayerChanged(bool isPlayerTurn)
	{
		Debug.Log(this + " OnPlayerChanged " + isPlayerTurn);
		onPlayerTurnChanged?.Invoke(isPlayerTurn);
	}

	private void OnLoadingGame(bool isLoading)
	{
		gameObject.SetActive(!isLoading);
	}

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
			if (selected.CanUseAction(CharacterAction.Run))
			{
				selected.Routines.AssignRunAction();
			}
			else
			{
				selected.FloatingText_Cant();
			}
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
		
		partyCharacters = CharacterEntity.GetPartyCharacters().ToArray();
		//Debug.Log(this + " UpdatePortraits partyCharacters=" + partyCharacters.Length);
		for (int i = 0; i < portraits.Length; i++)
		{
			portraits[i].AssignCharacter(i < partyCharacters.Length ? partyCharacters[i] : null);
		}
	}

	private void UpdateSelectables() // todo LateUpdate?
	{
		toggleGroupMode.isOn = gameInput.IsLeaderMode();
		toggleTurnBased.isOn = gameTurnManager.IsTurnBasedMode();
		toggleActions.isOn = gameTurnManager.SelectedCharacterIsControllable();
		button_actionRun.interactable = gameTurnManager.SelectedCharacterCanUseRunAction();
		
		if (gameTurnManager.PlayerCanEndTurn())
		{
			button_endTurn.interactable = true;
		}
		else
		{
			button_endTurn.interactable = false;
		}

		if (lerpSpeed > 0)
		{
			endTurnAnimators.ForEach(anim => anim.SetLayerWeight(endTurnLayer, Mathf.Lerp(anim.GetLayerWeight(endTurnLayer), gameTurnManager.PlayerShouldEndTurn() ? 1f : 0f, Time.unscaledDeltaTime * lerpSpeed)));
		}
	}

	private void OnSelectionChanged()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			if (selected.TryGetComponent(out Inventory inventory))
			{
				inventoryPanel.AssignInventory(inventory);
				return;
			}
		}
		
		inventoryPanel.AssignInventory(null);
	}
	#endregion
}