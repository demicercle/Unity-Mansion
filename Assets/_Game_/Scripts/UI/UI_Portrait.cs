using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_Portrait : MonoBehaviour, IPointerClickHandler
{
	public RawImage rawImage;
	public TMP_Text nameLabel;
	public Toggle toggle;
	public Image healthSprite;
	public TMP_Text healthLabel;

	public int GetPartyIndex() => transform.GetSiblingIndex();
	
	public void OnPointerClick(PointerEventData eventData)
	{
		Selection.SelectPartyCharacter(GetPartyIndex());
	}
	
	public void UpdateUI(CharacterController _assignedCharacter) // TODO use events
	{
		if (_assignedCharacter)
		{
			gameObject.SetActive(true);
			
			toggle.isOn = Selection.IsCharacterSelected(_assignedCharacter);
			toggle.interactable = _assignedCharacter.IsControllable();
			
			rawImage.texture = _assignedCharacter.GetPortraitTexture();
			nameLabel.text = _assignedCharacter.GetDisplayName();
			healthSprite.fillAmount = 1f - _assignedCharacter.GetDamageProgress();
			healthLabel.text = _assignedCharacter.GetCurrentHealthInt() + "/" +
			                   _assignedCharacter.GetMaxHealth();
			return;
		}
		
		Debug.Log(this + " No party character");
		gameObject.SetActive(false);
	}
}