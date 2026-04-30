using System;
using UnityEngine;
using UnityEngine.Events;

public class GameEventTrigger : MonoBehaviour
{
	public UnityEvent turnEnded, turnStarted, turnChanged;
	public UnityEvent onFightStarts, onFightEnds;
	public UnityEvent<bool> onTurnBasedChanged;

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
		onTurnBasedChanged?.Invoke(value);
		(value ? onFightStarts : onFightEnds)?.Invoke();
	}

	void TurnEnded()
	{
		turnEnded?.Invoke();
		turnChanged?.Invoke();
	}
	
	void TurnStarted()
	{ 
		turnStarted?.Invoke();
		turnChanged?.Invoke();
	}
}