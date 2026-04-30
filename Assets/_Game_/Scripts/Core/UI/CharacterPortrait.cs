using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class CharacterPortrait : MonoBehaviour, IPointerClickHandler
{
	public RawImage rawImage;
	public TMP_Text nameLabel;
	public Toggle toggleSelected, toggleHasActions, toggleTurnBased;
	public Image healthSprite;
	public TMP_Text healthLabel;
	public TMP_Text dodgeLabel1, dodgeLabel2;
	
	public void OnPointerClick(PointerEventData eventData)
	{
		CharacterSelection.SelectPartyCharacter( transform.GetSiblingIndex());
	}
	
	public void AssignCharacter(CharacterEntity character)
	{
		if (_assignedCharacter != null)
		{
			_assignedCharacter.onStatsChanged -= UpdateStats;
			_assignedCharacter.onFactionChanged -= UpdateStats;
			_assignedCharacter.onSelectionChanged -= OnSelectionChanged;
		}
		else
		{
			gameObject.SetActive(true);
		}
		
		_assignedCharacter = character;
		
		if (_assignedCharacter == null)
		{
			gameObject.SetActive(false);
			return;
		}
		
		_assignedCharacter.onStatsChanged += UpdateStats;
		_assignedCharacter.onFactionChanged += UpdateStats;
		_assignedCharacter.onSelectionChanged += OnSelectionChanged;

		UpdateStats();
	}
	
	private CharacterEntity _assignedCharacter;
	private int karmaInt;
	private float calculatedDodge;

	private void OnSelectionChanged(bool selected)
	{
		Invoke("UpdateStats", 0.1f);
	}

	private void UpdateStats()
	{
		toggleSelected.isOn = CharacterSelection.IsCharacterSelected(_assignedCharacter);
		toggleSelected.interactable = _assignedCharacter.IsControllableByPlayer();

		toggleHasActions.isOn = GameTurnManager.Instance.IsTurnBasedMode() 
		                        && GameTurnManager.Instance.IsPlayerTurn()
		                        && !_assignedCharacter.IsDead()
		                        && _assignedCharacter.HasActions();
			
		rawImage.texture = _assignedCharacter.GetPortraitTexture();
		nameLabel.text = _assignedCharacter.GetDisplayName();
		healthSprite.fillAmount = 1f - _assignedCharacter.GetDamageProgress();
		healthLabel.text = _assignedCharacter.GetCurrentHealth() + "/" +
		                   _assignedCharacter.GetMaxHealth();
	}

	private void Awake()
	{
		GameTurnManager.Instance.onTurnStarted += UpdateStats;
		GameTurnManager.Instance.onTurnEnded += UpdateStats;
	}

	private void OnDestroy()
	{
		GameTurnManager.Instance.onTurnStarted -= UpdateStats;
		GameTurnManager.Instance.onTurnEnded -= UpdateStats;
		
		if (_assignedCharacter != null) 
			AssignCharacter(null); // dispose events
	}

	private void LateUpdate()
	{
		if (_assignedCharacter)
		{
			toggleTurnBased.isOn = GameTurnManager.Instance.IsTurnBasedMode();
			calculatedDodge = _assignedCharacter.CalculateDodgeChance();
			if (calculatedDodge > 0)
			{
				dodgeLabel1.text = Mathf.RoundToInt(Mathf.Lerp(20, 1, calculatedDodge)) + "+";
				dodgeLabel2.text = "(" + Mathf.RoundToInt( calculatedDodge * 100) + "%)";
			}
			else
			{
				dodgeLabel1.text = "X";
				dodgeLabel2.text = "";
			}
		}
	}
}