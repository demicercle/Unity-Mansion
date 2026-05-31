using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Actions/Patrol Switch")]
public class NodeSwitchPatrol : GameNode
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _character;
	[Input] public ObjectReference _patrol;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_character = GetInputValue("_character", new ObjectReference(Script.gameObject));
			_patrol = GetInputValue("_patrol", new ObjectReference(Script.gameObject));
			if (_character.TryGetComponent(out CharacterEntity character) &&
			    _patrol.TryGetComponent(out PatrolRoute patrol))
			{
				DebugLog("assign patrol " + patrol + " to " + character);
				patrol.AssignPatrol(character);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}