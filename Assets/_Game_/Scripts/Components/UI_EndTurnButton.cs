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
		GameEvents.routinesStarted += SetButtonDisabled;
	}

	private void OnDestroy()
	{
		button.onClick.RemoveListener(Clicked);
		GameEvents.routinesStarted -= SetButtonDisabled;
	}

	void Clicked()
	{
		UIEvents.clickEndTurn?.Invoke();
	}
}