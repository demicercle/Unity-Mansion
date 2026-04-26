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
		Utils.SetDirty(this);
	}

	[ContextMenu("Remove Port")]
	public void RemovePort()
	{
		RemoveDynamicPort("Out" + DynamicOutputs.ToArray().Length);   
		Utils.SetDirty(this);
	}
	
	public override object GetValue(NodePort port)
	{
		CreateOutputs();
		
		if (int.TryParse(port.fieldName.Last().ToString(), out var index))
			return _outs[index];

		DebugWarningThis(this + " invalid port: " + port.fieldName);
		
		return null; // Replace this
	}

	protected override void Init()
	{
		base.Init();
		
		if (!DynamicOutputs.Any())
		{
			AddPort();
		}
		
		CreateOutputs();
	}

	private NodeExec[] _outs;
	private int _index;
	private int _count;

	private void CreateOutputs()
	{
		if (_outs == null || _outs.Length == 0)
		{
			var ports = DynamicOutputs.ToArray();
			_outs = new NodeExec[ports.Length];
			for (int i = 0; i < ports.Length; i++)
			{
				_outs[i] = new NodeExec();
			}
		}
	}
	
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		
		_index = 0;
		_in.StartInput(this, "_in", () =>
		{
			CreateOutputs();
			
			_outs[_index].Invoke();

			do
			{
				_index = (_index + 1) % _outs.Length;
			} while (_outs[_index] == null || !_outs[_index].HasCallbacks());
		});
	}
}