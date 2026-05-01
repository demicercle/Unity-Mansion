using XNode;

[CreateNodeMenu("Conditions/Compare Int")]
public class NodeCompareInt : GameNode
{
	[Input] public int _A;
	[Input] public int _B;
	[Output] public bool _Equals;
	[Output] public bool _Greater;
	[Output] public bool _Lower;

	public override object GetValue(NodePort port)
	{
		_A = GetInputValue("_A", _A);
		_B = GetInputValue("_B", _B);
		
		if (port.fieldName == "_Equals")
		{
			_Equals = _A == _B;
			DebugLogThis("_Equals: " + _Equals);
			return _Equals;
		}

		if (port.fieldName == "_Greater")
		{
			_Greater = _A > _B;
			DebugLogThis("_Greater: " + _Greater);
			return _Greater;
		}

		if (port.fieldName == "_Lower")
		{
			_Lower = _A < _B;
			DebugLogThis("_Lower: " + _Lower);
			return _Lower;
		}
		
		return base.GetValue(port);
	}
}