using System;
using UnityEngine;
using UnityEngine.Events;

public class GameEventTrigger : MonoBehaviour
{
	public UnityEvent turnEnded, turnStarted;
	public UnityEvent onFightStarts, onFightEnds;

	GameTurnManager _gameTurnManager;
	
	private void Awake()
	{
		_gameTurnManager = GameTurnManager.Instance;

		_gameTurnManager.onTurnEnded += TurnEnded;
		_gameTurnManager.onTurnStarted += TurnStarted;
		_gameTurnManager.onTurnBasedChanged += OnTurnBasedChanged;
	}

	private void OnDestroy()
	{
		_gameTurnManager.onTurnEnded -= TurnEnded;
		_gameTurnManager.onTurnStarted -= TurnStarted;
		_gameTurnManager.onTurnBasedChanged -= OnTurnBasedChanged;
	}
	
	private void OnTurnBasedChanged(bool value)
	{
		(value ? onFightStarts : onFightEnds)?.Invoke();
	}
	
	void TurnEnded() => turnEnded?.Invoke();
	void TurnStarted() =>  turnStarted?.Invoke();
}