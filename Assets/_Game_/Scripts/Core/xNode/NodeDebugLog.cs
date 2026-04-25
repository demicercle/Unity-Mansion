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
		_in.Connect(this, "_in", OnCallback);
		
		base.Init();
	}

	private void OnCallback()
	{
		Debug.Log(this + " " + message);
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}