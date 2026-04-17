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

	public void OnPointerClick(PointerEventData eventData)
	{
		GameView.GetInstance().SelectCharacter(transform.GetSiblingIndex());
	}
	
	public void UpdateUI(GameView view) // TODO use events
	{
		var _assignedCharacter = view.GetPartyCharacter(transform.GetSiblingIndex());
		if (_assignedCharacter)
		{
			gameObject.SetActive(true);
			toggle.interactable = _assignedCharacter.IsControllable();
			toggle.isOn = view.IsCharacterSelected(_assignedCharacter);
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