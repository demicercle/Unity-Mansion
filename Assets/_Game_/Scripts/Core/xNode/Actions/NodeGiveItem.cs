using UnityEngine;
using XNode;

[CreateNodeMenu("Actions/Give Item")]
public class NodeGiveItem : GameNode
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _character;
	
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
		//_in = GetInputValue("_in", _in);
		_in.InitInput(this, "_in", () =>
		{
			_character = GetInputValue("_character", new ObjectReference(Script.gameObject));
			if (_character.TryGetComponent(out Inventory inv))
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

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}