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

	public void OnPointerClick(PointerEventData eventData)
	{
		if (assignedCharacter != null)
		{
			UIEvents.clickSelectCharacter?.Invoke(assignedCharacter);
		}
	}

	private Character assignedCharacter;
	
	private void Awake()
	{
		GameEvents.characterAddedToParty += CharacterAddedToParty;
		GameEvents.characterSelected += CharacterSelected;
		//toggle.onValueChanged.AddListener(OnValueChanged);
		gameObject.SetActive(false);
	}

	private void OnDestroy()
	{
		GameEvents.characterAddedToParty -= CharacterAddedToParty;
		GameEvents.characterSelected -= CharacterSelected;
		//toggle.onValueChanged.RemoveListener(OnValueChanged);
	}

	private void OnValueChanged(bool value)
	{
		if (value && assignedCharacter != null)
		{
			UIEvents.clickSelectCharacter.Invoke(assignedCharacter);
		}
	}

	private void CharacterSelected(Character character, bool selected)
	{
		if (assignedCharacter == character)
		{
			toggle.isOn = selected;
		}
	}

	private void CharacterAddedToParty(Character arg1, int arg2)
	{
		var index = transform.GetSiblingIndex();
		if (index == arg2)
		{
			assignedCharacter = arg1;
			gameObject.SetActive(true);

			rawImage.texture = arg1.GetStats().portraitTexture;
			nameLabel.text = arg1.GetStats().displayName;
		}
	}
}