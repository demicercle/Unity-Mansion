using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Turn Changed")]
public class NodeTurnChanged : GameNode, ISceneLoaded
{
	[Input] public SceneReference _character;
	[Output] public NodeExec _turnStarted;
	[Output] public NodeExec _turnEnded;
	[Output] public bool _isPlayerTurn;
	[Output] public int _turnCount;

	private CharacterEntity _initCharacter;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_turnStarted") return _turnStarted;
		if (port.fieldName == "_turnEnded") return _turnEnded;
		if (port.fieldName == "_isPlayerTurn") return _isPlayerTurn;
		if (port.fieldName == "_turnCount") return _turnCount;
		return base.GetValue(port);
	}

	public void OnSceneLoaded()
	{
		_character = GetInputValue("_character", _character);
		if (_character.TryGetComponent(this, out _initCharacter))
		{
			DebugLogThis("init character: "  + _initCharacter);
			_initCharacter.turnStarted += TurnStarted;
			_initCharacter.turnEnded += TurnEnded;
		}
		else
		{
			GameTurnManager.Instance.onTurnStarted += TurnStarted;
			GameTurnManager.Instance.onTurnEnded += TurnEnded;
		}
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();

		if (_initCharacter)
		{
			_initCharacter.turnStarted -= TurnStarted;
			_initCharacter.turnEnded -= TurnEnded;
		}
		else
		{
			GameTurnManager.Instance.onTurnStarted -= TurnStarted;
			GameTurnManager.Instance.onTurnEnded -= TurnEnded;
		}
	}

	private void TurnStarted()
	{
		_isPlayerTurn = GameTurnManager.Instance.IsPlayerTurn();
		_turnCount = GameTurnManager.Instance.GetCurrentTurn();
		_turnStarted.Invoke();
	}

	private void TurnEnded()
	{
		_isPlayerTurn = GameTurnManager.Instance.IsPlayerTurn();
		_turnCount = GameTurnManager.Instance.GetCurrentTurn();
		_turnEnded.Invoke();
	}
}