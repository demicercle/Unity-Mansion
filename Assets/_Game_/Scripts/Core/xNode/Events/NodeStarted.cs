using System.Collections;
using XNode;

[CreateNodeMenu("Events/Started")]
public class NodeStarted : GameNode, ISceneLoaded
{
	[Output] public NodeExec _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out") return _out;
		return base.GetValue(port);
	}
	
	public void OnSceneLoaded()
	{
		script.StartCoroutine(WaitAndInvoke());
	}

	private IEnumerator WaitAndInvoke()
	{
		yield return null;
		_out.Invoke();
	}
}