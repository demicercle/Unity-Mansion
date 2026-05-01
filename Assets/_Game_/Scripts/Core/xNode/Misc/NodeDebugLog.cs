using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

[CreateNodeMenu("Misc/Debug Log")]
public class NodeDebugLog : GameNode
{
	[Input] public NodeExec _in;
	[Input] public string message;

	public bool debugBreak;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		return null; // Replace this
	}
	
	// Use this for initialization
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			Debug.Log("<b>x_DebugLog</b> " + GetInputValue("message", message) + "\nin graph=" + graph);
			if (debugBreak)
			{
				Debug.Break();
				Debug.DebugBreak();
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}