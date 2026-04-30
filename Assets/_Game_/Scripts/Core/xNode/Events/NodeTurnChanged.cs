using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Turn Changed")]
public class NodeTurnChanged : GameNode
{
	[Input] public SceneReference _character;
	[Output] public NodeExec _turnStarted;
	[Output] public NodeExec _turnEnded;

	private CharacterEntity _initCharacter;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_turnStarted") return _turnStarted;
		if (port.fieldName == "_turnEnded") return _turnEnded;
		return base.GetValue(port);
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_character = GetInputValue("_character", _character);
		if (_character.TryGetComponent(this, out _initCharacter))
		{
			DebugLogThis("init character: "  + _initCharacter);
			_initCharacter.turnStarted += TurnStarted;
			_initCharacter.turnEnded += TurnEnded;
		}
		else
		{
			DebugLogThis("init GTM");
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
		_turnStarted.Invoke();
	}

	private void TurnEnded()
	{
		_turnEnded.Invoke();
	}
}