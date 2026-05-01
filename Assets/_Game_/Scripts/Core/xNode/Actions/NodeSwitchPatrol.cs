using XNode;

[Node.CreateNodeMenu("Actions/Patrol Switch")]
public class NodeSwitchPatrol : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public SceneReference _patrol;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_character = GetInputValue("_character", _character);
			_patrol = GetInputValue("_patrol", _patrol);
			if (_character.TryGetComponent(this, out CharacterEntity character) &&
			    _patrol.TryGetComponent(this, out PatrolRoute patrol))
			{
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