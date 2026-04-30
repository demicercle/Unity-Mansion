using XNode;

[CreateNodeMenu("FlowControl/Branch")]
public class NodeBranch : GameNode
{
	[Input] public NodeExec _in;
	[Input] public bool _condition;
	[Output] public NodeExec _true;
	[Output] public NodeExec _false;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_true")
			return _true;
		if (port.fieldName == "_false")
			return _false;
		return base.GetValue(port);
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", () =>
		{
			_condition = GetInputValue("_condition", _condition);
			if(_condition)
				_true.Invoke();
			else 
				_false.Invoke();
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}