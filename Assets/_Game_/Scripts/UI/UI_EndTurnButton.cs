using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_EndTurnButton : MonoBehaviour
{
	public Button button;

	private void SetButtonDisabled(bool b) => button.interactable = !b;

	private void Awake()
	{
		button.onClick.AddListener(Clicked);
		GameView.GetInstance().routinesStarted += SetButtonDisabled;
	}

	private void OnDestroy()
	{
		button.onClick.RemoveListener(Clicked);
		GameView.GetInstance().routinesStarted -= SetButtonDisabled;
	}

	void Clicked()
	{
		GameView.GetInstance().EndTurn();
	}
}