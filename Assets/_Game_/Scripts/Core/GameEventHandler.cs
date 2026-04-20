using System;
using UnityEngine;
using UnityEngine.Events;

public class GameEventHandler : MonoBehaviour
{
	public UnityEvent turnEnded, turnStarted, sceneLoaded;

	GameView gameView;
	GameTurnManager _gameTurnManager;
	
	private void Awake()
	{
		gameView = GameView.GetInstance();
		_gameTurnManager = GameTurnManager.Instance;

		_gameTurnManager.turnEnded += TurnEnded;
		_gameTurnManager.turnStarted += TurnStarted;
	}

	private void OnDestroy()
	{
		_gameTurnManager.turnEnded -= TurnEnded;
		_gameTurnManager.turnStarted -= TurnStarted;
	}
	
	void TurnEnded() => turnEnded?.Invoke();
	void TurnStarted() =>  turnStarted?.Invoke();
}