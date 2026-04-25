using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

[CreateAssetMenu]
public class GameScriptGraph : NodeGraph
{ 
}

[System.Serializable]
public class NodeExec
{
	public System.Action callback;
	
	public void Connect(Node node, string inputName, System.Action callback)
	{
		otherExec =node.GetInputPort(inputName).GetInputValue<NodeExec>();
		if (otherExec != null)
		{
			Debug.Log(this + " Connects " + node + " " + inputName);
			otherExec.callback += callback;
		}
	}

	private static NodeExec otherExec;
}