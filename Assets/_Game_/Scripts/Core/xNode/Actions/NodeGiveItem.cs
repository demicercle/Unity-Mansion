using UnityEngine;
using XNode;

[CreateNodeMenu("Actions/Give Item")]
public class NodeGiveItem : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	
	public ItemData itemData;

	[Output] public NodeExec _success;
	[Output] public NodeExec _failure;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_success") return _success;
		if (port.fieldName == "_failure") return _failure;
		return base.GetValue(port);
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", () =>
		{
			if (_character.TryGetComponent(this, out Inventory inv))
			{
				if (inv.TryAddItem(itemData))
				{
					_success.Invoke();
				}
				else
				{
					_failure.Invoke();
				}
			}
		});
	}
}