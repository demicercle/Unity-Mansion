using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UI_Portrait : MonoBehaviour
{
	public RawImage rawImage;
	public TMP_Text nameLabel;

	private void Awake()
	{
		GameEvents.characterAddedToParty += CharacterAddedToParty;
		gameObject.SetActive(false);
	}

	private void OnDestroy()
	{
		GameEvents.characterAddedToParty -= CharacterAddedToParty;
	}

	private void CharacterAddedToParty(Character arg1, int arg2)
	{
		var index = transform.GetSiblingIndex();
		if (index == arg2)
		{
			gameObject.SetActive(true);

			rawImage.texture = arg1.GetPortraitTexture();
			nameLabel.text = arg1.GetDisplayName();
		}
	}
}