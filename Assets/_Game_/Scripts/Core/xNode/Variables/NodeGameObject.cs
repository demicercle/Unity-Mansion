
using XNode;

[CreateNodeMenu("Variables/GameObject")]
public class NodeGameObject : GameNode
{
	[Input] public string findName;
	
	[Output] public SceneReference _out;

	public override object GetValue(NodePort port)
	{
		findName = GetInputValue("findName", findName);
		
		_out.findName = findName;
		
		if (port.fieldName == "_out")
			return _out;
		
		return base.GetValue(port);
	}
}