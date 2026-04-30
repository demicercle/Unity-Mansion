using System.Collections;
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
	
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		Script.StartCoroutine(WaitAndInvoke());
	}

	private IEnumerator WaitAndInvoke()
	{
		yield return null;
		_out.Invoke();
	}
}