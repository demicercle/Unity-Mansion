using System;
using UnityEngine;
using UnityEngine.Events;

public class GameEventHandler : MonoBehaviour
{
	public UnityEvent turnEnded, turnStarted, sceneLoaded;

	GameView gameView;
	GameController gameController;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		gameController = gameView.GetController();

		gameController.turnEnded += TurnEnded;
		gameController.turnStarted += TurnStarted;
	}

	private void OnDestroy()
	{
		gameController.turnEnded -= TurnEnded;
		gameController.turnStarted -= TurnStarted;
	}
	
	void TurnEnded() => turnEnded?.Invoke();
	void TurnStarted() =>  turnStarted?.Invoke();
}