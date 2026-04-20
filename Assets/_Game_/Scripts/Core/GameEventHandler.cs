using System;
using UnityEngine;
using UnityEngine.Events;

public class GameEventHandler : MonoBehaviour
{
	public UnityEvent turnEnded, turnStarted;

	GameTurnManager _gameTurnManager;
	
	private void Awake()
	{
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