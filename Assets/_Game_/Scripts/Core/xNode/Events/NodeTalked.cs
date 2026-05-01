using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Talked")]
public class NodeTalked : GameNode
{
	[Input] public SceneReference _character;
	[Output] public NodeExec _out;

	private CharacterEntity character;
	
	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}

	protected override void OnScriptStart()
	{
		_character = GetInputValue<SceneReference>("_character", _character);
		if (_character.TryGetComponent(this, out character))
		{
			DebugLog("init character: "  + character);
			character.onTalkedBy += OnTalkedBy;
		}
		else
		{
			DebugLogWarning("no character found");
		}
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		if (character)
		{
			character.onTalkedBy -= OnTalkedBy;
		}
	}

	
	private void OnTalkedBy(CharacterEntity other)
	{
		_out.Invoke();
	}
}