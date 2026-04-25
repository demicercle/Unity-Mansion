using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

public class NodeDebugLog : GameNode
{
	[Input] public NodeExec _in;
	[Input] public string message;
	
	// Use this for initialization
	protected override void Init()
	{
		base.Init();
		_in.Init(this, "_in", OnCallback);
	}

	private void OnCallback()
	{
		DebugLogThis(message);
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}