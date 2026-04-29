using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class PauseCanvas : MonoBehaviour
{
	public static PauseCanvas instance;
	public static PauseCanvas Instance => instance;

	[SerializeField] private Toggle toggleAllowSave;

	public void TogglePaused()
	{
		if(gameObject.activeSelf)
			ResumeGame();
		else 
			PauseGame();
	}

	public void PauseGame()
	{
		gameObject.SetActive(true);
		Time.timeScale = 0.0f;
	}

	public void ResumeGame()
	{
		gameObject.SetActive(false);
		Time.timeScale = 1f;
	}

	public void QuitGame(bool doSave)
	{
		if(doSave)
			GameSave.Current.SaveGame(true);
		
		SceneManager.LoadScene(0);
	}
	
	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		ResumeGame();
	}

	private void LateUpdate()
	{
		toggleAllowSave.isOn = !GameSave.IsDisabled() && GameTurnManager.Instance.PlayerCanSave();
	}
}