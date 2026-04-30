using UnityEngine;
using XNode;

[CreateNodeMenu("Events/Interacted")]
public class NodeInteracted : GameNode
{
	[Input] public SceneReference _prop;
	[Output] public NodeExec _out;

	// Return the correct value of an output port when requested
	public override object GetValue(NodePort port) {
		if (port.fieldName == "_out")
		{
			return _out;
		}
		return null; // Replace this
	}

	private PropComponent prop;
	

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_prop = GetInputValue<SceneReference>("_prop", _prop);
		if (_prop.TryGetComponent(this, out prop))
		{
			DebugLogThis("init prop: "  + prop);
			prop.onInteracted += Interacted;
		}
		else
		{
			DebugWarningThis("no prop found");
		}
	}
	
	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		if (prop)
		{
			prop.onInteracted -= Interacted;
		}
	}
	
	private void Interacted()
	{
		_out.Invoke();
	}
}