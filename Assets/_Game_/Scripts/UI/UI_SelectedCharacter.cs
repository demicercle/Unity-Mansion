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

	void ActionUsed(Character character = null) => OnCharacterSelected();
	
	private bool hide;
	private Character selectedCharacter;
	private GameView gameView;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		hide = true;
		
		gameView.characterSelected += OnCharacterSelected;
		gameView.routinesStarted += RoutinesStarted;
		gameView.actionUsed += ActionUsed;

		actionButton_Run.onClick.AddListener(gameView.ClickRunAction);
	}

	private void OnDestroy()
	{
		var gameView = GameView.GetInstance();
		gameView.characterSelected -= OnCharacterSelected;
		gameView.routinesStarted -= RoutinesStarted;
		gameView.actionUsed -= ActionUsed;
		
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