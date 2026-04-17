using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_SelectedCharacter : MonoBehaviour
{
	public Button actionButton_Run;

	private bool routineStarted;
	private CharacterController lastSelectedCharacter;

	private GameView gameView;
	private GameController gameController;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		gameController = gameView.GetController();
		
		gameController.selectionChanged += SelectionChanged;
		gameView.routinesStarted += RoutinesStarted;

		actionButton_Run.onClick.AddListener(gameView.ClickRunAction);
	}

	private void OnDestroy()
	{
		
		gameController.selectionChanged -= SelectionChanged;
		gameView.routinesStarted -= RoutinesStarted;
		
		actionButton_Run.onClick.RemoveAllListeners();
	}

	private void LateUpdate()
	{
		if (lastSelectedCharacter)
		{
			actionButton_Run.interactable = !routineStarted && lastSelectedCharacter.HasActions();
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	private void RoutinesStarted(bool started)
	{
		routineStarted = started;
	}
	
	private void SelectionChanged()
	{
		lastSelectedCharacter = gameController.GetSelectedCharacter();
		gameObject.SetActive(lastSelectedCharacter && gameController.CanControlCharacter(lastSelectedCharacter));
	}
}