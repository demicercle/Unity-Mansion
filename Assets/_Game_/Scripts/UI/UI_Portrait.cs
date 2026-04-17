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
		GameView.GetInstance().SelectCharacter(_assignedCharacterView);
	}

	private CharacterView _assignedCharacterView;
	private GameView gameView;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		gameView.partyChanged += PartyChanged;
		gameView.selectionChanged += SelectionChanged;
	}

	private void OnDestroy()
	{
		gameView.partyChanged -= PartyChanged;
		gameView.selectionChanged -= SelectionChanged;
	}

	private void SelectionChanged(CharacterView characterView, bool canControlCharacter)
	{
		toggle.isOn = _assignedCharacterView == characterView;
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

	private void OnEnable()
	{
		if(_assignedCharacterView) Debug.Log(this + " > " + _assignedCharacterView);
	}

	private void LateUpdate() // TODO use events
	{
		if (_assignedCharacterView)
		{
			var data = _assignedCharacterView.GetData();
			rawImage.texture = data.portraitTexture;
			nameLabel.text = data.displayName;
			healthSprite.fillAmount = data.GetDamageProgress();
			healthLabel.text = _assignedCharacterView.GetCurrentHealthInt() + "/" + _assignedCharacterView.GetMaxHealth();
			return;
		}
		
		Debug.Log(this + " No character");
		gameObject.SetActive(false);
	}
}