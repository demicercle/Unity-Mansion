using XNode;

[CreateNodeMenu("Actions/Interact")]
public class NodeInteract : GameNode/*, ISceneLoaded*/
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _prop;
	[Input] public SceneReference _instigator;
	
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
			_prop = GetInputValue("_prop", _prop);
			if (_prop.TryGetComponent(this, out PropComponent prop))
			{
				_instigator = GetInputValue("_instigator", _instigator);
				
				if (_instigator.TryGetComponent(this, out CharacterEntity character))
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