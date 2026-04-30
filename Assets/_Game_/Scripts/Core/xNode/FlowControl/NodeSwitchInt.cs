using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("FlowControl/Switch")]
public class NodeSwitchInt : GameNode
{
	[Input] public NodeExec _in;
	[Input] public int _index;

	public bool loop = true;

	[ContextMenu("Add Port")]
	public void AddPort()
	{
		AddDynamicOutput(typeof(NodeExec), fieldName: "Out" + DynamicOutputs.ToArray().Length);   
		Utils.SetDirty(this);
	}

	[ContextMenu("Remove Port")]
	public void RemovePort()
	{
		RemoveDynamicPort("Out" + (DynamicOutputs.ToArray().Length-1));   
		Utils.SetDirty(this);
	}
	
	public override object GetValue(NodePort port)
	{
		CreateOutputs();
		
		if (int.TryParse(port.fieldName.Last().ToString(), out var index)
		    && index >= 0 && index < _outs.Length)
			return _outs[index];

		DebugWarningThis("invalid port: " + port.fieldName + " parsed index=" + index + " outs=" + _outs.Length);
		
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

	private void CreateOutputs()
	{
		var ports = DynamicOutputs.ToArray();
		_outs = new NodeExec[ports.Length];
		for (int i = 0; i < ports.Length; i++)
		{
			_outs[i] = new NodeExec();
		}
		
		DebugLogThis("created outs=" + _outs.Length);
	}
	
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		
		_in.StartInput(this, "_in", () =>
		{
			CreateOutputs();

			_index = GetInputValue("_index", _index);
			
			if (_index >= 0 && _index < _outs.Length)
			{
				_outs[_index].Invoke();
				DebugLogThis("fired: " + _index);
			}
			else DebugLogThis("not fired, invalid index: " + _index);

			if (loop)
			{
				_index = (_index + 1) % _outs.Length;
			}
			else
			{
				_index = Mathf.Clamp(_index, _index + 1, _outs.Length);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}