using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

public class NodeFloatingText : GameNode
{
	[Input] public NodeExec _in;
	[Output] public NodeExec _out;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}
	
	[Input] public string message;
	
	// Use this for initialization
	protected override void Init()
	{
		base.Init();
		_in.InitInput(this, "_in", OnCallback);
	}

	private void OnCallback()
	{
		
	}
}