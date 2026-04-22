using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class CharacterPortrait : MonoBehaviour, IPointerClickHandler
{
	public RawImage rawImage;
	public TMP_Text nameLabel;
	public Toggle toggle;
	public Image healthSprite;
	public TMP_Text healthLabel;
	
	public void OnPointerClick(PointerEventData eventData)
	{
		CharacterSelection.SelectPartyCharacter( transform.GetSiblingIndex());
	}
	
	public void AssignCharacter(CharacterController character)
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
	
	private CharacterController _assignedCharacter;

	private void OnSelectionChanged(bool selected)
	{
		toggle.isOn = selected;
		//UpdateStats();
	}

	private void UpdateStats()
	{
		toggle.isOn = CharacterSelection.IsCharacterSelected(_assignedCharacter);
		toggle.interactable = _assignedCharacter.IsControllableByPlayer();
			
		rawImage.texture = _assignedCharacter.GetPortraitTexture();
		nameLabel.text = _assignedCharacter.GetDisplayName();
		healthSprite.fillAmount = 1f - _assignedCharacter.GetDamageProgress();
		healthLabel.text = _assignedCharacter.GetCurrentHealthInt() + "/" +
		                   _assignedCharacter.GetMaxHealth();
	}

	private void OnDestroy()
	{
		if (_assignedCharacter != null) 
			AssignCharacter(null); // dispose events
	} 
}