using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Variables/Get Instigator")]
public class NodeGetInstigator : GameNode
{
	[Output] public ObjectReference _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
		{
			return _out = new ObjectReference(Script.GetInstigator());
		}
		
		return base.GetValue(port);
	}
}