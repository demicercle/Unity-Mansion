using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_SelectedCharacter : MonoBehaviour
{
	public Button actionButton_Run;
	
	private bool hide;
	private CharacterView lastSelectedCharacter;
	
	private void Awake()
	{
		var gameView = GameView.GetInstance();
		hide = true;
		
		gameView.characterSelected += SetSelectedCharacter;
		gameView.routinesStarted += RoutinesStarted;

		actionButton_Run.onClick.AddListener(gameView.ClickRunAction);
	}

	private void OnDestroy()
	{
		var gameView = GameView.GetInstance();
		gameView.characterSelected -= SetSelectedCharacter;
		gameView.routinesStarted -= RoutinesStarted;
		
		actionButton_Run.onClick.RemoveAllListeners();
	}

	private void LateUpdate()
	{
		if (hide)
		{
			gameObject.SetActive(false);
		}
	}

	void RoutinesStarted(bool started)
	{
		actionButton_Run.interactable = !started;
		ActionChanged(0);
	}
	
	void ActionChanged(int q) => SetSelectedCharacter(lastSelectedCharacter, lastSelectedCharacter != null);
	
	private void SetSelectedCharacter(CharacterView newChar, bool selected)
	{
		if (selected && newChar)
		{
			lastSelectedCharacter = newChar;
			hide = false;
			gameObject.SetActive(true);

			var data = lastSelectedCharacter.GetData();
			actionButton_Run.interactable = data.HasActions();

			data.actionsChanged += ActionChanged;
		}
		else
		{
			if (lastSelectedCharacter != null)
			{
				var data = lastSelectedCharacter.GetData();
				data.actionsChanged -= ActionChanged;
			}

			lastSelectedCharacter = null;
			hide = true;
		}
	}

}