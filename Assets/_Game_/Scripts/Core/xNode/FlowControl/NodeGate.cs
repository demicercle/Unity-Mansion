using XNode;

[Node.CreateNodeMenu("FlowControl/Gate")]
public class NodeGate : GameNode
{
	[Input] public NodeExec _in;
	[Input] public NodeExec _close;
	[Input] public NodeExec _open;
	
	[Output] public NodeExec _out;

	public bool startsClosed;
	
	private bool isClosed;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		isClosed = startsClosed;
		_in.InitInput(this, "_in", () =>
		{
			if(!isClosed)
				_out.Invoke();
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