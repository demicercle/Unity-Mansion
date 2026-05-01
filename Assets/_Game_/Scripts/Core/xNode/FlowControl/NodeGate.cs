using XNode;

[Node.CreateNodeMenu("FlowControl/Gate")]
public class NodeGate : GameNode
{
	[Input] public NodeExec _in;
	[Input] public NodeExec _close;
	[Input] public NodeExec _open;
	
	[Output] public NodeExec _out;

	public bool startsClosed;
	public bool autoClose;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out") return _out;
		return base.GetValue(port);
	}
	
	private bool isClosed;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		isClosed = startsClosed;
		_in.InitInput(this, "_in", () =>
		{
			DebugLogThis("isClosed: " + isClosed);
			if (!isClosed)
			{
				if (autoClose)
					isClosed = true;
				
				_out.Invoke();
			}
		});
		_open.InitInput(this, "_open", () =>
		{
			isClosed = false;
			DebugLogThis("isClosed: " + isClosed);
		});
		_close.InitInput(this, "_close", () =>
		{
			isClosed = true;
			DebugLogThis("isClosed: " + isClosed);
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
		_close.Dispose();
		_open.Dispose();
	}
}