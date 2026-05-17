using System;
using UnityEngine;
using UnityEngine.UI;


public class StartScene : MonoBehaviour
{
	public SceneLoader sceneLoader;
	public Button newGameButton;
	public Button loadGameButton;
	public Button quitGameButton;
	public TMPro.TMP_Text errorLabel;

	private void Awake()
	{
		errorLabel.enabled = false;
		newGameButton.onClick.AddListener(ClickNewGame);
		loadGameButton.onClick.AddListener(ClickLoadGame);
		quitGameButton.onClick.AddListener(ClickQuitGame);
	}

	private void OnDestroy()
	{
		newGameButton.onClick.RemoveListener(ClickNewGame);
		loadGameButton.onClick.RemoveListener(ClickLoadGame);
		quitGameButton.onClick.RemoveListener(ClickQuitGame);
	}

	private void LateUpdate()
	{
		loadGameButton.interactable = GameSave.SaveExists();
	}

	private void ClickNewGame()
	{
		GameSave.Current.Clear(true);
		sceneLoader.LoadNow();
	}

	private void ClickLoadGame()
	{
		if (GameSave.Current.LoadFromFile())
		{
			sceneLoader.LoadNow();
		}
		else
		{
			errorLabel.enabled = true;
		}
	}

	private void ClickQuitGame()
	{
		Application.Quit();
	}
}