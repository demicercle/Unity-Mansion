using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_SelectedCharacter : MonoBehaviour
{
	public Button actionButton_Run;

	void RoutinesStarted(bool started)
	{
		actionButton_Run.interactable = !started && _selectedCharacterView && _selectedCharacterView.HasActions();
	}

	void ActionUsed(CharacterView characterView = null) => OnCharacterSelected();
	
	private bool hide;
	private CharacterView _selectedCharacterView;
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

	void OnCharacterSelected() => OnCharacterSelected(_selectedCharacterView, _selectedCharacterView != null);
	private void OnCharacterSelected(CharacterView characterView, bool selected)
	{
		//Debug.Log(this + " OnCharacterSelected " + character + " " + selected);
		if (selected && characterView != null)
		{
//			Debug.Log(this + " Show " + character);
			_selectedCharacterView = characterView;
			hide = false;
			gameObject.SetActive(true);

			actionButton_Run.interactable = characterView.HasActions();
		}
		else if (_selectedCharacterView == characterView && !selected)
		{
//			Debug.Log(this + " Hide");
			hide = true;
		}
	}
}