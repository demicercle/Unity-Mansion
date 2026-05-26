using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Variables/Get Self")]
public class NodeGetSelf : GameNode
{
	[Output] public ObjectReference _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
		{
			return _out = new ObjectReference(Script.gameObject);
		}
		
		return base.GetValue(port);
	}
}