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
	public Slider karmaSlider;
	public TMP_Text karmaLabel;
	public float karmaLerpSpeed = 1f;
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
			
			karmaSlider.gameObject.SetActive(_assignedCharacter.HasKarma());
			
			if (karmaSlider.gameObject.activeSelf)
			{
				karmaSlider.minValue = -999;
				karmaSlider.maxValue = 999;
				karmaSlider.value = Mathf.MoveTowards(karmaSlider.value, _assignedCharacter.GetCurrentKarma() * 999, Time.unscaledDeltaTime * 999 * karmaLerpSpeed);
			
				karmaInt = Mathf.Abs(Mathf.RoundToInt(karmaSlider.value));
			}
			
			karmaLabel.gameObject.SetActive(karmaSlider.gameObject.activeSelf);
			if (karmaLabel.gameObject.activeSelf)
			{
				karmaLabel.text = (karmaSlider.value > 0 ? "+" : karmaSlider.value < 0 ? "-" : "")
				                  + (karmaInt < 100 ? "0" : "")
				                  + (karmaInt < 10 ? "0" : "")
				                  + (karmaInt);
			}

			calculatedDodge = _assignedCharacter.CalculateDodgeChance();
			dodgeLabel1.text = Mathf.RoundToInt(Mathf.Lerp(20, 1, calculatedDodge)) + "+";
			dodgeLabel2.text = "(" + Mathf.RoundToInt( calculatedDodge * 100) + "%)";
		}
	}
}