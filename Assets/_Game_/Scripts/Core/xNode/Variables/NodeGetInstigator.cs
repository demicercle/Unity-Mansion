using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Variables/Get Instigator")]
public class NodeGetInstigator : GameNode
{
	[Output] public SceneReference _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
		{
			_out.SetGameObject(Script.GetInstigator());
			return _out;
		}
		
		return base.GetValue(port);
	}
}