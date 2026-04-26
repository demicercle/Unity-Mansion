using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Interacted")]
public class NodeInteracted : GameNode
{
	[Input] public SceneReference _prop;
	[Output] public NodeExec _out;
	
	protected override void OnScriptAwake()
	{
		base.OnScriptAwake();
		
		if (_prop.TryGetComponent(this, out PropComponent prop))
		{
			prop.onInteracted += Interacted;
		}
		else
		{
			DebugLogWarningThis("no prop found");
		}
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		if (_prop.TryGetComponent(this, out PropComponent prop))
		{
			prop.onInteracted -= Interacted;
		}
	}

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}
	
	private void Interacted()
	{
		_out.Invoke();
	}
}