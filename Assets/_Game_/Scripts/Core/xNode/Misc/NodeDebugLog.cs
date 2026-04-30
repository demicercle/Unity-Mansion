using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

[CreateNodeMenu("Misc/DebugLog")]
public class NodeDebugLog : GameNode
{
	[Input] public NodeExec _in;
	[Input] public string message;
	
	// Use this for initialization
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", OnCallback);
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}

	private void OnCallback()
	{
		Debug.Log("<color=yellow>x_DebugLog</color> " + GetInputValue("message", message) + "\nin graph=" + graph);
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
}