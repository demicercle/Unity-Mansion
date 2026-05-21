using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Variables/Get Self")]
public class NodeGetSelf : GameNode
{
	[Output] public GameObject _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
		{
			return _out = Script.gameObject;
		}
		
		return base.GetValue(port);
	}
}