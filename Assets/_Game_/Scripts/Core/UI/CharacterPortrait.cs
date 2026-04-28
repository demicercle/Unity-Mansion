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
	public Toggle toggleSelected, toggleHasActions;
	public Image healthSprite;
	public TMP_Text healthLabel;
	public Slider karmaSlider;
	public TMP_Text[] karmaLabels;
	
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
			karmaSlider.value = Mathf.Lerp(karmaSlider.value, _assignedCharacter.GetCurrentKarma(), Time.unscaledDeltaTime);
			foreach (var kt in karmaLabels)
				kt.text = (karmaSlider.value > 0 ? "+" : "") + karmaSlider.value.ToString("N2").Replace(".", "").Replace(",","");
		}
	}
}