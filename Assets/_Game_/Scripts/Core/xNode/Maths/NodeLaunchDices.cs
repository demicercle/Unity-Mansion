
using UnityEngine;
using XNode;

[CreateNodeMenu("Maths/Launch Dices")]
public class NodeLaunchDices : GameNode
{
	[Input] public int _count = 2;
	[Input] public int _faces = 6;

	[Output] public float _result;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_result")
		{
			RollNow(false);
			return _result;
		}
		return base.GetValue(port);
	}

	[ContextMenu("Roll!")]
	private void RollNow() => RollNow(true);
	private void RollNow(bool debug)
	{
		_count = GetInputValue("_count", _count);
		_faces = GetInputValue("_faces", _faces);

		_result = 0;
		for (int i = 0; i < _count; i++)
			_result += Random.Range(0, _faces) + 1f;
		
		if(debug) Debug.Log(_result);
	}
}