using UnityEngine;
using XNode;

public class NodeInteracted : GameNode
{
	[Output] public NodeExec _out;
	
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
	
	public void Interacted()
	{
		_out.callback?.Invoke();
	}
}