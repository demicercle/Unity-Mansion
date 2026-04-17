using System;
using UnityEngine;
using UnityEngine.UI;


public class UI_EndTurnButton : MonoBehaviour
{
	public Button button;

	private GameView view;

	private void Awake()
	{
		view = GameView.GetInstance();
		button.onClick.AddListener(Clicked);
	}

	private void OnDestroy()
	{
		button.onClick.RemoveListener(Clicked);
	}

	void Clicked()
	{
		GameView.GetInstance().ClickEndTurn(); // todo prefab
	}

	void LateUpdate()
	{
		button.interactable = !view.HasCoroutines() && view.GetController().IsPlayerTurn();
	}
}