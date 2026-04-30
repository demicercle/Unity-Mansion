
using XNode;

[CreateNodeMenu("Variables/GameObject")]
public class NodeGameObject : GameNode
{
	[Output] public SceneReference _out;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
			return _out;
		
		return base.GetValue(port);
	}
}