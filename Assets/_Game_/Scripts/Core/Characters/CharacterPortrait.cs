using System;
using TMPro;
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
		_assignedCharacter = character;
		gameObject.SetActive(true);
	}
	
	private CharacterController _assignedCharacter;

	private void LateUpdate()
	{
		if (_assignedCharacter == null)
		{
			gameObject.SetActive(false);
			return;
		}
			
		toggle.isOn = CharacterSelection.IsCharacterSelected(_assignedCharacter);
		toggle.interactable = _assignedCharacter.IsControllableByPlayer();
			
		rawImage.texture = _assignedCharacter.GetPortraitTexture();
		nameLabel.text = _assignedCharacter.GetDisplayName();
		healthSprite.fillAmount = 1f - _assignedCharacter.GetDamageProgress();
		healthLabel.text = _assignedCharacter.GetCurrentHealthInt() + "/" +
		                   _assignedCharacter.GetMaxHealth();
	}
}