using XNode;

[Node.CreateNodeMenu("Conditions/Is In Combat")]
public class NodeIsInCombat : GameNode
{
	[Input] public SceneReference _character;

	[Output] public bool _inCombat;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_inCombat")
		{
			_character = GetInputValue("_character", _character);
			if (_character.TryGetComponent(this, out CharacterEntity character))
			{
				_inCombat = character.InCombat();
			}
			else
			{
				DebugWarningThis("cannot find character: " + _character);
			}
			
			return _inCombat;
		}
		
		return base.GetValue(port);
	}
}