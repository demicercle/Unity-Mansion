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
		if (_assignedCharacterView != null)
		{
			GameView.clickSelectCharacter?.Invoke(_assignedCharacterView);
		}
	}

	private CharacterView _assignedCharacterView;
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
		if (value && _assignedCharacterView != null)
		{
			GameView.clickSelectCharacter.Invoke(_assignedCharacterView);
		}
	}

	private void CharacterSelected(CharacterView characterView, bool selected)
	{
		if (_assignedCharacterView == characterView)
		{
			toggle.isOn = selected;
		}
	}

	private void CharacterAddedToParty(CharacterView arg1, int arg2)
	{
		var index = transform.GetSiblingIndex();
		if (index == arg2)
		{
			_assignedCharacterView = arg1;
			gameObject.SetActive(arg1 && arg2 >= 0);

			if (_assignedCharacterView && _assignedCharacterView.TryGetData(out var data))
			{
				rawImage.texture = data.portraitTexture;
				nameLabel.text = data.displayName;
			}
		}
	}
}