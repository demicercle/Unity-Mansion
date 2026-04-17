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
	
	private CharacterController _assignedCharacter;
	private GameView gameView;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		gameView.GetController().partyChanged += PartyChanged;
	}

	private void OnDestroy()
	{
		gameView.GetController().partyChanged -= PartyChanged;
	}

	private void PartyChanged(CharacterController[] partyCharacters)
	{
		_assignedCharacter = null;
		
		var index = transform.GetSiblingIndex();
		for (int i = 0; i < partyCharacters.Length; i++)
		{
			if (i == index)
			{
				_assignedCharacter =  partyCharacters[i];
				break;
			}
		}

		gameObject.SetActive(_assignedCharacter != null);
	}

	private void OnEnable()
	{
		if(_assignedCharacter) Debug.Log(this + " > " + _assignedCharacter);
	}

	private void LateUpdate() // TODO use events
	{
		if (_assignedCharacter)
		{
			toggle.isOn = _assignedCharacter.IsSelected();
			rawImage.texture = _assignedCharacter.GetPortraitTexture();
			nameLabel.text = _assignedCharacter.GetDisplayName();
			healthSprite.fillAmount = _assignedCharacter.GetDamageProgress();
			healthLabel.text = _assignedCharacter.GetCurrentHealthInt() + "/" +
			                   _assignedCharacter.GetMaxHealth();
			return;
		}
		
		Debug.Log(this + " No party character");
		gameObject.SetActive(false);
	}
}