using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

public class NodeFloatingText : GameNode
{
	[Output] public NodeExec _out;
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public string message;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}
	
	// Use this for initialization
	protected override void Init()
	{
		base.Init();
		_in.InitInput(this, "_in", OnCallback);
	}

	private void OnCallback()
	{
		if (_character.TryGetComponent(this, out CharacterController character))
		{
			character.FloatingText(GetInputValue("message", message));
		}
	}
}