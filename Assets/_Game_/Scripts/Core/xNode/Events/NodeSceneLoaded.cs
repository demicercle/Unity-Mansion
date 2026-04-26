using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using RPGCharacterAnims;
using UnityEngine;
using XNode;

[CreateNodeMenu("Events/SceneLoaded")]
public class NodeSceneLoaded : GameNode
{
	[Output] public NodeExec _out;

	public void OnSceneLoaded()
	{
		_out.Invoke();
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