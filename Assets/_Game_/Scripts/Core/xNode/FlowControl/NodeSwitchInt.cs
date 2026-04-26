using System.Linq;
using UnityEngine;
using XNode;

[CreateNodeMenu("FlowControl/Switch")]
public class NodeSwitchInt : GameNode
{
	[Input] public NodeExec _in;
	[Output] public NodeExec _out0;
	[Output] public NodeExec _out1;
	[Output] public NodeExec _out2;
	[Output] public NodeExec _out3;
	[Output] public NodeExec _out4;
	[Output] public NodeExec _out5;
	[Output] public NodeExec _out6;
	[Output] public NodeExec _out7;

	public override object GetValue(NodePort port)
	{
		if (int.TryParse(port.fieldName.Last().ToString(), out var index) && index < Outs.Length)
			return Outs[index];
		return null; // Replace this
	}

	private NodeExec[] _outs;
	private int _index;
	
	protected override void OnScriptAwake()
	{
		base.OnScriptAwake();

		_index = 0;
		_in.AwakeInput(this, "_in", () =>
		{
			Outs[_index].Invoke();

			do
			{
				_index = (_index + 1) % Outs.Length;
			} while (Outs[_index] == null || !Outs[_index].HasCallbacks());
		});
	}

	private NodeExec[] Outs
	{
		get
		{
			if(_outs == null) _outs = new NodeExec[8]
			{
				_out0, _out1, _out2, _out3, _out4, _out5, _out6, _out7
			};

			return _outs;
		}
	}
}