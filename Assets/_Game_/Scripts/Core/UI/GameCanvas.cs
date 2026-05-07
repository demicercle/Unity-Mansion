using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
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
	[Header("Toggles")]
	[SerializeField] private Toggle toggleGroupMode;
	[SerializeField] private Toggle toggleTurnBased;
	[SerializeField] private Toggle toggleActions;
	[SerializeField] private Toggle toggleShouldEndTurn;
	[Header("Buttons")]
	[SerializeField] private Button buttonEndTurn;
	[SerializeField] private Button buttonNextCharacter;
	//[SerializeField] private Button buttonActionRun;
	//[SerializeField] private Button buttonActionDefend;
	[Header("Portraits")]
	[SerializeField] private CharacterPortrait[] portraits;
	[Header("Inventory")]
	[SerializeField] private InventoryPanel inventoryPanel;
	[SerializeField] private RawImage itemCursorImage;
	[Header("Cursor")] 
	[SerializeField] private TMP_Text cursorText;
	[SerializeField] private bool displayLastResultDice;

	[Header("References")] 
	[SerializeField] private FloatingText floatingText;
	[Header("UnityEvents")]
	public UnityEvent<bool> onPlayerTurnChanged;
	
	//public bool mouseOverViewport { get; private set; }

	public Vector2 referenceResolution
	{
		get
		{
			if (canvasScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
			{
				return canvasScaler.referenceResolution;
			}
			else
			{
				return new Vector2(Screen.width, Screen.height);
			}
		}
	}

	public void FloatingText(Component obj, string label) => floatingText.Clone(obj.transform, label);
	public void FloatingText(GameObject obj, string label) => floatingText.Clone(obj.transform, label);
	public void FloatingText(Vector3 pos, string label) => floatingText.Clone(pos, label);

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
	private ItemData currentDraggedItem;
	private CanvasScaler canvasScaler;
	private int calculatedHitChance;
	private bool willHit;
	private bool hitBlocked;
	private CharacterEntity selectedCharacter;
	private CharacterEntity targetCharacter;
	
	private void Awake()
	{
		if (instance) Debug.LogError(this + " duplicated!");
		instance = this;
		
		canvasScaler = GetComponent<CanvasScaler>();
		
		floatingText.gameObject.SetActive(false);
		
		buttonEndTurn.onClick.AddListener(ClickEndTurn);
		buttonNextCharacter.onClick.AddListener(ClickNextCharacter);
		//buttonActionRun.onClick.AddListener(ClickRunAction);
		//buttonActionDefend.onClick.AddListener(ClickDefendAction);

		viewportMouseHandler = viewportRect.GetComponent<MouseOverHandler>();
		if(!viewportMouseHandler) viewportMouseHandler = viewportRect.gameObject.AddComponent<MouseOverHandler>();
		viewportMouseHandler.onMouseOverChanged += OnViewportRectMouseOver;
		viewportMouseHandler.onClicked += OnViewportRectClicked;

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
		buttonEndTurn.onClick.RemoveListener( ClickEndTurn);
		buttonNextCharacter.onClick.RemoveListener(ClickNextCharacter);
		//buttonActionRun.onClick.RemoveListener(ClickRunAction);
		//buttonActionDefend.onClick.RemoveListener(ClickDefendAction);
		
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
		if (arg2 is Component)
		{
			if (arg3 == CharacterEntity.AttackResult.Undefined)
				FloatingText(arg2 as Component, LanguageSettings.Main.Cant);
			else if (arg3 == CharacterEntity.AttackResult.Dodge)
				FloatingText(arg2 as Component, LanguageSettings.Main.Dodge);
			else if (arg3 == CharacterEntity.AttackResult.Miss)
				FloatingText(arg2 as Component, LanguageSettings.Main.Miss);
		}
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

		currentDraggedItem = gameInput.GetDraggedItem();
		if (!currentDraggedItem) currentDraggedItem = gameInput.GetItemToUse();
		itemCursorImage.gameObject.SetActive(currentDraggedItem);
		if (itemCursorImage.gameObject.activeSelf)
		{
			itemCursorImage.texture = currentDraggedItem.GetIcon();
			itemCursorImage.color = Utils.ColorAlpha(Color.white, gameInput.IsBlocked() ? 0.30f : 0.90f);
			itemCursorImage.transform.position = gameInput.GetMousePosition();
			itemCursorImage.transform.SetAsLastSibling();
		}

		targetCharacter = null;
		cursorText.gameObject.SetActive(false);
		
		if (CharacterSelection.TryGetSelectedCharacter(out selectedCharacter) && selectedCharacter)
		{
			if ( gameInput.GetAimedObject(out var mouseOverObject)	&& mouseOverObject.TryGetComponent(out targetCharacter) 
																	&& selectedCharacter != targetCharacter
																	&& (selectedCharacter.IsEnemyTo(targetCharacter) 
																	    || (selectedCharacter.IsAiming() && selectedCharacter.WillHit(targetCharacter.gameObject))))
			{
				cursorText.transform.position = gameInput.GetMousePosition();
				cursorText.raycastTarget = false;
				calculatedHitChance = 20 - Mathf.Clamp(selectedCharacter.CalculateHitChance(targetCharacter), 1, 20);
				if(CharacterEntity.debugStats > 1) Debug.Log(this + " calculated hit chance " + calculatedHitChance);
				willHit = (DicePool.LastResult > 1 && calculatedHitChance < DicePool.LastResult) || DicePool.LastResult >= 20;
				hitBlocked = selectedCharacter.IsUsingRangeWeapon() && !selectedCharacter.LOSCheck(targetCharacter);
				cursorText.text = calculatedHitChance + "+";
				if (displayLastResultDice) cursorText.text += " (" + DicePool.LastResult + ")";
				cursorText.text += "\n" + (willHit ?  LanguageSettings.Main.WillHit : LanguageSettings.Main.WillMiss);
				if (hitBlocked) cursorText.text += "\n" + LanguageSettings.Main.HitBlocked;
				cursorText.gameObject.SetActive(true);
			}
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
				selected.Routines.AssignUseAction(CharacterAction.Run);
			}
			else
			{
				selected.FloatingText_Cant();
			}
		}
	}

	private void ClickDefendAction()
	{
		if (CharacterSelection.TryGetSelectedCharacter(out var selected))
		{
			if (selected.CanUseAction(CharacterAction.Defend))
			{
				selected.Routines.AssignUseAction(CharacterAction.Defend);
			}
			else
			{
				selected.FloatingText_Cant();
			}
		}
	}

	private void ClickEndTurn() => gameTurnManager.TryEndPlayerTurn(false);

	private void ClickNextCharacter() => CharacterSelection.SelectNextCharacter(true);

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
		toggleShouldEndTurn.isOn = gameTurnManager.PlayerShouldEndTurn();
		//buttonActionRun.interactable = gameTurnManager.SelectedCharacterCanUseAction(CharacterAction.Run);
		//buttonActionDefend.interactable = gameTurnManager.SelectedCharacterCanUseAction(CharacterAction.Defend);
		
		if (gameTurnManager.PlayerCanEndTurn() && !gameTurnManager.IsWaiting())
		{
			buttonEndTurn.interactable = true;
		}
		else
		{
			buttonEndTurn.interactable = false;
		}

		buttonNextCharacter.interactable = CharacterEntity.CountLivingPartyCharacters() > 1;
		buttonNextCharacter.gameObject.SetActive(buttonEndTurn.interactable);
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