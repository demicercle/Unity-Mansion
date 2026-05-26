using UnityEngine;

[CreateNodeMenu("Actions/Heal")]
public class NodeHeal : GameNode
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _character;
	[Input] public float _amount;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		//_in = GetInputValue("_in", _in);
		_in.InitInput(this, "_in", () =>
		{
			_amount = GetInputValue("_amount", _amount);
			_character = GetInputValue("_character", new ObjectReference(Script.gameObject));
			
			if (_character.TryGetComponent(out CharacterEntity character))
			{
				DebugLog("healed: " + character + " by " + _amount);
				character.Heal(_amount);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}