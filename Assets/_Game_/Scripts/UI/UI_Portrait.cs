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
			GameView.clickSelectCharacter?.Invoke(assignedCharacter);
		}
	}

	private Character assignedCharacter;
	private GameView gameView;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		gameView.characterAddedToParty += CharacterAddedToParty;
		gameView.characterSelected += CharacterSelected;
		//toggle.onValueChanged.AddListener(OnValueChanged);
		gameObject.SetActive(false);
	}

	private void OnDestroy()
	{
		gameView.characterAddedToParty -= CharacterAddedToParty;
		gameView.characterSelected -= CharacterSelected;
		//toggle.onValueChanged.RemoveListener(OnValueChanged);
	}

	private void OnValueChanged(bool value)
	{
		if (value && assignedCharacter != null)
		{
			GameView.clickSelectCharacter.Invoke(assignedCharacter);
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
			gameObject.SetActive(arg1 && arg2 >= 0);

			if (assignedCharacter && assignedCharacter.TryGetData(out var data))
			{
				rawImage.texture = data.portraitTexture;
				nameLabel.text = data.displayName;
			}
		}
	}
}