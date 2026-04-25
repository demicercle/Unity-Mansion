using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

public class NodeFloatingText : GameNode
{
	//[Output] public NodeExec _out;
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public LocalizedText message;

	protected override void Init()
	{
		base.Init();
		message.Init();
	}

	// Return the correct value of an output port when requested
	/*public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}*/
	
	// Use this for initialization
	protected override void OnScriptAwake()
	{
		base.OnScriptAwake();
		_in.AwakeInput(this, "_in", OnCallback);
	}

	private void OnCallback()
	{
		if (_character.TryGetComponent(this, out CharacterController character))
		{
			character.FloatingText(GetInputValue("message", message).GetValue());
			//_out.Invoke();
		}
	}
}