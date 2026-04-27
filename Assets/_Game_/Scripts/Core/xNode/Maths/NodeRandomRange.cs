using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Maths/Random Range")]
public class NodeRandomRange : GameNode
{
	[Input] public float _min;
	[Input] public float _max;
	[Output] public float _result;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_result")
		{
			_min = GetInputValue<float>("_min", _min);
			_max = GetInputValue<float>("_max", _max);
			_result = Random.Range(_min, _max);
			return _result;
		}
		return base.GetValue(port);
	}
}