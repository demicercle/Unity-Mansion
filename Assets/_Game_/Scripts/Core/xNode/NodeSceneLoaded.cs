using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RPGCharacterAnims;
using UnityEngine;
using XNode;

public class NodeSceneLoaded : GameNode
{
	[Output] public NodeExec _out;

	public void OnSceneLoaded()
	{
		_out.callback?.Invoke();
	}
	
	// Use this for initialization
	protected override void Init()
	{
		base.Init();
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}
}