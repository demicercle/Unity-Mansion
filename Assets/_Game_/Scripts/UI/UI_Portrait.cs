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
		gameView.partyChanged += PartyChanged;
		gameView.characterSelected += CharacterSelected;
		
		toggle.onValueChanged.AddListener(OnValueChanged);
		gameObject.SetActive(false);
	}

	private void OnDestroy()
	{
		gameView.partyChanged -= PartyChanged;
		gameView.characterSelected -= CharacterSelected;
		
		toggle.onValueChanged.RemoveListener(OnValueChanged);
	}

	private void OnValueChanged(bool value)
	{
		if (value && _assignedCharacterView && !gameView.IsCharacterSelected(_assignedCharacterView))
		{
			gameView.SelectCharacter(_assignedCharacterView);
		}
	}

	private void CharacterSelected(CharacterView characterView, bool selected)
	{
		if (_assignedCharacterView == characterView)
		{
			toggle.isOn = selected;
		}
	}

	private void PartyChanged(CharacterView[] partyCharacters)
	{
		_assignedCharacterView = null;
		
		var index = transform.GetSiblingIndex();
		for (int i = 0; i < partyCharacters.Length; i++)
		{
			if (i == index)
			{
				_assignedCharacterView = partyCharacters[i];
				break;
			}
		}

		gameObject.SetActive(_assignedCharacterView != null);
	}

	private void LateUpdate() // TODO use events
	{
		if (_assignedCharacterView)
		{
			if (_assignedCharacterView.TryGetData(out var data)) // TODO selected character should be in data
			{
				rawImage.texture = data.portraitTexture;
				nameLabel.text = data.displayName;
				healthSprite.fillAmount = data.GetDamageProgress();
				healthLabel.text = _assignedCharacterView.GetCurrentHealthInt() + "/" + _assignedCharacterView.GetMaxHealth();
			}
		}
	}
}