using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Conditions/Is In Combat")]
public class NodeIsInCombat : GameNode
{
	[Input] public GameObject _character;

	[Output] public bool _inCombat;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_inCombat")
		{
			_character = GetInputValue("_character", Script.gameObject);
			if (_character.TryGetComponent(out CharacterEntity character))
			{
				_inCombat = character.InBattle();
			}
			else
			{
				DebugLogWarning("cannot find character: " + _character);
			}
			
			return _inCombat;
		}
		
		return base.GetValue(port);
	}
}