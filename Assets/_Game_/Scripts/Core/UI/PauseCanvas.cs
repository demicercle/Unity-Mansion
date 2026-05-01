using System;
using System.Collections;
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

	public void QuitGame()
	{
		StopAllCoroutines();
		StartCoroutine(BackToFirstScene(0.1f));
	}
	
	public void SaveAndQuit()
	{
		StopAllCoroutines();
		GameSave.Current.SaveGame(true);
		StartCoroutine(BackToFirstScene(0.5f));
	}

	private IEnumerator BackToFirstScene(float time)
	{
		yield return new WaitForSecondsRealtime(time);
		Debug.Log(this + " Back to first scene");
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