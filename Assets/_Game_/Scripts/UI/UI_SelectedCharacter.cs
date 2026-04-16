using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_SelectedCharacter : MonoBehaviour
{
	public Button actionButton_Run;

	void RoutinesStarted(bool started)
	{
		actionButton_Run.interactable = !started && selectedCharacter && selectedCharacter.HasActions();
	}

	void ActionUsed(CharacterStats stats = null) => OnCharacterSelected();
	
	private bool hide;
	private Character selectedCharacter;
	
	private void Awake()
	{
		hide = true;
		
		GameEvents.characterSelected += OnCharacterSelected;
		GameEvents.routinesStarted += RoutinesStarted;
		GameEvents.actionUsed += ActionUsed;
		
		actionButton_Run.onClick.AddListener(() =>
		{
			UIEvents.clickRunAction?.Invoke();
		});
	}

	private void OnDestroy()
	{
		GameEvents.characterSelected -= OnCharacterSelected;
		GameEvents.routinesStarted -= RoutinesStarted;
		GameEvents.actionUsed -= ActionUsed;
		
		actionButton_Run.onClick.RemoveAllListeners();
	}

	private void LateUpdate()
	{
		if (hide)
		{
			gameObject.SetActive(false);
		}
	}

	void OnCharacterSelected() => OnCharacterSelected(selectedCharacter, selectedCharacter != null);
	private void OnCharacterSelected(Character character, bool selected)
	{
		//Debug.Log(this + " OnCharacterSelected " + character + " " + selected);
		if (selected && character != null)
		{
//			Debug.Log(this + " Show " + character);
			selectedCharacter = character;
			hide = false;
			gameObject.SetActive(true);

			actionButton_Run.interactable = character.HasActions();
		}
		else if (selectedCharacter == character && !selected)
		{
//			Debug.Log(this + " Hide");
			hide = true;
		}
	}
}