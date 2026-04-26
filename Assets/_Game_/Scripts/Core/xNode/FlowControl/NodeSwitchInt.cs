using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("FlowControl/Switch")]
public class NodeSwitchInt : GameNode
{
	[Input] public NodeExec _in;

	[ContextMenu("Add Port")]
	public void AddPort()
	{
		AddDynamicOutput(typeof(NodeExec), fieldName: "Out" + DynamicOutputs.ToArray().Length);   
	}

	[ContextMenu("Remove Port")]
	public void RemovePort()
	{
		RemoveDynamicPort("Out" + DynamicOutputs.ToArray().Length);   
	}
	
	public override object GetValue(NodePort port)
	{
		if (int.TryParse(port.fieldName.Last().ToString(), out var index) && index < _outs.Length)
			return _outs[index];
		return null; // Replace this
	}

	protected override void Init()
	{
		base.Init();
		if (!DynamicOutputs.Any())
		{
			AddPort();
		}
	}

	private NodeExec[] _outs;
	private int _index;
	
	protected override void OnScriptStart()
	{
		base.OnScriptStart();

		_outs = DynamicOutputs.ToList().ConvertAll(x => (NodeExec)x.GetOutputValue()).ToArray();
		_index = 0;
		_in.StartInput(this, "_in", () =>
		{
			_outs[_index].Invoke();

			do
			{
				_index = (_index + 1) % _outs.Length;
			} while (_outs[_index] == null || !_outs[_index].HasCallbacks());
		});
	}
}