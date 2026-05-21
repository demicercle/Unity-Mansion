using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Conditions/Is DEAD")]
public class NodeIsDead : GameNode
{
	[Input] public GameObject _character;

	[Output] public bool _isDead;

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_inCombat")
		{
			_character = GetInputValue("_character", Script.gameObject);
			if (_character.TryGetComponent(out CharacterEntity character))
			{
				_isDead = character.IsDead();
			}
			else
			{
				DebugLogWarning("cannot find character: " + _character);
			}
			
			return _isDead;
		}
		
		return base.GetValue(port);
	}
}