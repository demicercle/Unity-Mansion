using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Talked")]
public class NodeTalked : GameNode
{
	[Input] public SceneReference _character;
	[Output] public NodeExec _out;
	
	protected override void OnScriptAwake()
	{
		base.OnScriptAwake();
		
		if (_character.TryGetComponent(this, out CharacterController character))
		{
			character.onTalkedBy += OnTalkedBy;
		}
		else
		{
			DebugLogWarningThis("no character found");
		}
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		if (_character.TryGetComponent(this, out CharacterController character))
		{
			character.onTalkedBy -= OnTalkedBy;
		}
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}
	
	private void OnTalkedBy(CharacterController other)
	{
		_out.Invoke();
	}
}