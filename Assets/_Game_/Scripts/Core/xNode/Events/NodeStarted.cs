using XNode;

[CreateNodeMenu("Events/Started")]
public class NodeStarted : GameNode
{
	[Output] public NodeExec _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out") return _out;
		return base.GetValue(port);
	}

	protected override void OnScriptAwake()
	{
		base.OnScriptAwake();
		
		_out.Invoke();
	}
}