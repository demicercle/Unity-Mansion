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
		GameView.GetInstance().SelectCharacter(_assignedCharacterData);
	}
	
	private CharacterData _assignedCharacterData;
	private GameView gameView;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		gameView.gameData.partyChanged += PartyChanged;
	}

	private void OnDestroy()
	{
		gameView.gameData.partyChanged -= PartyChanged;
	}

	private void PartyChanged(CharacterData[] partyCharacters)
	{
		_assignedCharacterData = null;
		
		var index = transform.GetSiblingIndex();
		for (int i = 0; i < partyCharacters.Length; i++)
		{
			if (i == index)
			{
				_assignedCharacterData =  partyCharacters[i];
				break;
			}
		}

		gameObject.SetActive(_assignedCharacterData != null);
//		Debug.Log(this + " PartyChanged: " + gameObject.activeSelf);
	}

	private void OnEnable()
	{
		if(_assignedCharacterData) Debug.Log(this + " > " + _assignedCharacterData);
	}

	private void LateUpdate() // TODO use events
	{
		if (_assignedCharacterData)
		{
			toggle.isOn = gameView.IsCharacterSelected(_assignedCharacterData.GetCharacterIndex());
			rawImage.texture = _assignedCharacterData.portraitTexture;
			nameLabel.text = _assignedCharacterData.displayName;
			healthSprite.fillAmount = _assignedCharacterData.GetDamageProgress();
			healthLabel.text = _assignedCharacterData.GetCurrentHealthInt() + "/" + _assignedCharacterData.GetMaxHealth();
			return;
		}
		
		Debug.Log(this + " No party character");
		gameObject.SetActive(false);
	}
}