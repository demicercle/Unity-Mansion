using System;
using UnityEngine;
using UnityEngine.Events;

public class GameEventTrigger : MonoBehaviour
{
	public UnityEvent turnEnded, turnStarted;
	public UnityEvent onFightStarts, onFightEnds;
	public UnityEvent<bool> onTurnBasedChanged;
	public UnityEvent<bool> onPlayerChanged;

	GameTurnManager _gameTurnManager;
	
	private void Awake()
	{
		_gameTurnManager = GameTurnManager.Instance;

		_gameTurnManager.onTurnEnded += turnEnded.Invoke;
		_gameTurnManager.onTurnStarted += turnStarted.Invoke;
		_gameTurnManager.onPlayerChanged += onPlayerChanged.Invoke;
		_gameTurnManager.onTurnBasedChanged += OnTurnBasedChanged;
	}

	private void OnDestroy()
	{
		_gameTurnManager.onTurnEnded -= turnEnded.Invoke;
		_gameTurnManager.onTurnStarted -= turnStarted.Invoke;
		_gameTurnManager.onPlayerChanged -= onPlayerChanged.Invoke;
		_gameTurnManager.onTurnBasedChanged -= OnTurnBasedChanged;
	}
	
	private void OnTurnBasedChanged(bool value)
	{
		onTurnBasedChanged?.Invoke(value);
		(value ? onFightStarts : onFightEnds)?.Invoke();
	}
}