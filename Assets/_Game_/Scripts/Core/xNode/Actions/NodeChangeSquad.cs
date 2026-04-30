using XNode;

[Node.CreateNodeMenu("Actions/Change Squad")]
public class NodeChangeSquad : GameNode
{
	[Input] public NodeExec _add;
	[Input] public NodeExec _remove;
	[Input] public SceneReference _character;
	[Input] public SceneReference _squad;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_add.InitInput(this, "_add", () =>
		{
			_character = GetInputValue("_character", _character);
			_squad = GetInputValue("_squad", _squad);
			if (_squad.TryGetComponent(this, out Squad squad) &&
			    _character.TryGetComponent(this, out CharacterEntity character))
			{
				squad.AddCharacter(character);
			}
		});
		_remove.InitInput(this, "_remove", () =>
		{
			_character = GetInputValue("_character", _character);
			_squad = GetInputValue("_squad", _squad);
			if (_squad.TryGetComponent(this, out Squad squad) &&
			    _character.TryGetComponent(this, out CharacterEntity character))
			{
				squad.RemoveCharacter(character);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_add.Dispose();
		_remove.Dispose();
	}
}