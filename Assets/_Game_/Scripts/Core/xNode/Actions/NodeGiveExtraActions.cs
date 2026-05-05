[CreateNodeMenu("Actions/Give Extra Actions")]
public class NodeGiveExtraActions : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public int _amount = 2;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_character = GetInputValue("_character", _character);
			_amount = GetInputValue("_amount", _amount);
			if (_character.TryGetComponent(this, out CharacterEntity character) && _amount > 0)
			{
				character.GiveExtraActions(_amount);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}