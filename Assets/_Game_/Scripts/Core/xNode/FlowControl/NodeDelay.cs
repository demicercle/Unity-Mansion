using System.Collections;
using UnityEngine;
using XNode;

[CreateNodeMenu("FlowControl/Delay")]
public class NodeDelay : GameNode
{
	[Input] public NodeExec _in;
	[Input] public float _delay = 0.1f;
	[Output] public NodeExec _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
			return _out;
		return null;
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", () =>
		{
			if (_scheduler) _scheduler.StopAndDestroy();
			_scheduler = CoroutineScheduler.Start(script.gameObject, WaitAction());
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}

	private IEnumerator WaitAction()
	{
		_delay = GetInputValue("_delay", _delay);
		yield return new WaitForSeconds(_delay);
		_out.Invoke();
	}
	
	private CoroutineScheduler _scheduler;
}