using XNode;

[CreateNodeMenu("Actions/Wait")]
public class NodeWait : GameNode
{
	[Input] public NodeExec _in;
	[Input] public float _duration;
	
	public override object GetValue(NodePort port)
	{
		// return outputs
		return base.GetValue(port);
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		// init inputs
		_in.InitInput(this, "_in", () =>
		{
			GameTurnManager.Instance.SetWaitTime(GetInputValue("_duration", _duration));
		});
	}

	protected override void OnScriptDestroy()
	{
		// dispose inputs
		base.OnScriptDestroy();
	}
}