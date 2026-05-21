using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Turn Changed")]
public class NodeTurnChanged : GameNode
{
	[Input] public GameObject _character;
	[Output] public NodeExec _turnStarted;
	[Output] public NodeExec _turnEnded;
	[Output] public int _currentTurn;

	private CharacterEntity selfCharacter;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_currentTurn") return _currentTurn;
		if (port.fieldName == "_turnStarted") return _turnStarted;
		if (port.fieldName == "_turnEnded") return _turnEnded;
		return base.GetValue(port);
	}

	protected override void Init()
	{
		base.Init();
		selfCharacter = null;
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_character = GetInputValue("_character", _character);
		if (_character && _character.TryGetComponent(out selfCharacter))
		{
			DebugLog("init character: "  + selfCharacter);
			selfCharacter.turnStarted += TurnStarted;
			selfCharacter.turnEnded += TurnEnded;
		}
		else
		{
			DebugLog("init GTM");
			GameTurnManager.Instance.onTurnStarted += TurnStarted;
			GameTurnManager.Instance.onTurnEnded += TurnEnded;
		}
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();

		if (selfCharacter)
		{
			selfCharacter.turnStarted -= TurnStarted;
			selfCharacter.turnEnded -= TurnEnded;
		}
		else
		{
			GameTurnManager.Instance.onTurnStarted -= TurnStarted;
			GameTurnManager.Instance.onTurnEnded -= TurnEnded;
		}
	}

	private void TurnStarted()
	{
		if (selfCharacter && selfCharacter.IsDead()) return;
		_currentTurn = GameTurnManager.Instance.GetCurrentTurn();
		DebugLog("turn started #" + _currentTurn);
		_turnStarted.Invoke();
	}

	private void TurnEnded()
	{
		if (selfCharacter && selfCharacter.IsDead()) return;
		_currentTurn = GameTurnManager.Instance.GetCurrentTurn();
		DebugLog("turn ended #" + _currentTurn);
		_turnEnded.Invoke();
	}
}