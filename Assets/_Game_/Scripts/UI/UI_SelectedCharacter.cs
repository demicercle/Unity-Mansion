using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_SelectedCharacter : MonoBehaviour
{
	public Button actionButton_Run;

	private bool routineStarted;
	private CharacterView lastSelectedCharacter;
	
	private void Awake()
	{
		var gameView = GameView.GetInstance();
		gameView.selectionChanged += SelectionChanged;
		gameView.routinesStarted += RoutinesStarted;

		actionButton_Run.onClick.AddListener(gameView.ClickRunAction);
	}

	private void OnDestroy()
	{
		var gameView = GameView.GetInstance();
		gameView.selectionChanged -= SelectionChanged;
		gameView.routinesStarted -= RoutinesStarted;
		
		actionButton_Run.onClick.RemoveAllListeners();
	}

	private void LateUpdate()
	{
		if (lastSelectedCharacter)
		{
			actionButton_Run.interactable = !routineStarted && lastSelectedCharacter.HasActions();
		}
	}

	private void RoutinesStarted(bool started)
	{
		routineStarted = started;
	}
	
	private void SelectionChanged(CharacterView newChar, bool canControlCharacter)
	{
		lastSelectedCharacter = newChar;
		gameObject.SetActive(canControlCharacter);
	}

}