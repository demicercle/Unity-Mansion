using System;
using System.Collections;
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
	[SerializeField] private bool displayHitChance;
	[SerializeField] private bool displayLastResultDice;
	[SerializeField] private bool displayHitBlocked;
	[SerializeField] private bool displayHeightMod, displayRangeMod, displayOtherMod;

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
	private int heightMod, rangeMod, otherMod;
	private CharacterEntity selectedCharacter;
	private CharacterEntity targetCharacter;
	
	private void Awake()
	{
		if (instance) Debug.LogError(this + " duplicated!");
		instance = this;
		
		for(int i = 0; i < portraits.Length; i++)
			portraits[i].gameObject.SetActive(false);
		
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

	private IEnumerator Start()
	{
		do yield return null;
		while (GameTurnManager.Instance.IsLoading());
		
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

	private Color hitColor => Color.softGreen;
	private Color missColor => Color.softRed;
	
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
		
		if (GameTurnManager.Instance.IsPlayerTurn() && CharacterSelection.TryGetSelectedCharacter(out selectedCharacter) && selectedCharacter)
		{
			if ( gameInput.GetAimedObject(out var mouseOverObject)	&& mouseOverObject.TryGetComponent(out targetCharacter) 
																	&& selectedCharacter != targetCharacter
																	&& (selectedCharacter.IsEnemyTo(targetCharacter) 
																	    || (selectedCharacter.IsAiming() && selectedCharacter.WillHit(targetCharacter.gameObject))))
			{
				calculatedHitChance = 20 - Mathf.Clamp(selectedCharacter.CalculateHitChance(targetCharacter), 1, 20);
				willHit = (DicePool.LastResult > 1 && calculatedHitChance < DicePool.LastResult) || DicePool.LastResult >= 20;
				hitBlocked = selectedCharacter.IsUsingRangeWeapon() && !selectedCharacter.LOSCheck(targetCharacter);
				
				cursorText.text = "<size=14><b>" + targetCharacter.GetDisplayName() + "</b></size>\n";
				cursorText.text += StringUtils.Greyscale("<size=9>(" + targetCharacter.GetHealthDescription() + ")</size>\n", Mathf.Lerp(0.2f, 0.8f, targetCharacter.GetDamageProgress()));

				//cursorText.text += "<size=11>";
				if (displayHitChance)
				{
					cursorText.text += StringUtils.Size("<b>" + LanguageSettings.Main.DescribeHitChance(calculatedHitChance) + "</b>", 13);
					if (displayLastResultDice) cursorText.text += " / " + DicePool.LastResult;
					cursorText.text += " > " + StringUtils.Colorize(
						willHit ? LanguageSettings.Main.WillHit : LanguageSettings.Main.WillMiss,
						willHit ? hitColor : missColor);
				}
				//cursorText.text += "</size>";
				
				cursorText.text += "<size=8>";
				if (selectedCharacter.IsUsingRangeWeapon())
				{
					if (!hitBlocked && displayRangeMod)
					{
						rangeMod = selectedCharacter.GetHitChanceModifiedByRange(targetCharacter);
						if(rangeMod > selectedCharacter.GetHitChance()) cursorText.text += StringUtils.Colorize("\nRange +" + Mathf.Abs(rangeMod - selectedCharacter.GetHitChance()), hitColor);
						if(rangeMod < selectedCharacter.GetHitChance()) cursorText.text += StringUtils.Colorize("\nRange -" + Mathf.Abs(rangeMod - selectedCharacter.GetHitChance()), missColor);
					}
					
					if (!hitBlocked && displayHeightMod)
					{
						heightMod = selectedCharacter.GetHitChanceHeightModifier(targetCharacter.GetPosition().y - selectedCharacter.GetPosition().y);
						if (heightMod != 0)
							cursorText.text += StringUtils.Colorize(
								"\nHeight " + (heightMod > 0 ? "+" : "-") + Mathf.Abs(heightMod),
								heightMod > 0 ? hitColor : missColor);
					}
				}
				cursorText.text += "</size>";

				if (displayHitBlocked && hitBlocked)
				{
					cursorText.text += StringUtils.Colorize(StringUtils.Size("\n" + LanguageSettings.Main.HitBlocked, 11), missColor);
				}
				
				cursorText.gameObject.SetActive(true);
			}
		}

		if (GameInput.Instance.TryGetMouseOver(out PropComponent overProp) && overProp.TryGetComponent(out Inventory overInventory) && overInventory.IsLoot())
		{
			if (overInventory.TryGetItem(0, out var itemData))
			{
				cursorText.gameObject.SetActive(true);
				cursorText.text = StringUtils.Bold(itemData.GetDisplayName());
			}
		}

		if (cursorText.gameObject.activeSelf)
		{
			cursorText.transform.position = gameInput.GetMousePosition();
			cursorText.raycastTarget = false;
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

	private void OnGUI()
	{
		if (CharacterEntity.debugStats > 0 && selectedCharacter&& targetCharacter)
		{
			var debugString = "";
			debugString += "hitChance Raw " + selectedCharacter.GetHitChance();
			debugString += "\nhitChance Range " + selectedCharacter.GetHitChanceModifiedByRange(targetCharacter);
			debugString += "\nhitChance Height " + selectedCharacter.GetHitChanceHeightModifier(targetCharacter);
			debugString += "\nhitChance = " + selectedCharacter.CalculateHitChance(targetCharacter);
			
			Utils.DrawWorldGUI(targetCharacter.GetPosition(), debugString);
		}
	}
}