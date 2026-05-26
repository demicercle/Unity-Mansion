using UnityEngine;
using XNode;

[CreateNodeMenu("Actions/Interact")]
public class NodeInteract : GameNode/*, ISceneLoaded*/
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _prop;
	[Input] public ObjectReference _instigator;
	
	public override object GetValue(NodePort port)
	{
		// return outputs
		return base.GetValue(port);
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_prop = GetInputValue("_prop", new ObjectReference(Script.gameObject));
			if (_prop.TryGetComponent(out PropComponent prop))
			{
				_instigator = GetInputValue("_instigator", new ObjectReference(Script.gameObject));
				
				if (_instigator.TryGetComponent(out CharacterEntity character))
					prop.InteractedBy(character);
				else
					prop.InteractedBy(null);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		// dispose inputs
		base.OnScriptDestroy();
	}
}