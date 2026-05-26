using UnityEngine;

[CreateNodeMenu("Actions/Instantiate")]
public class NodeInstantiate : GameNode
{
	[Input] public NodeExec _in;
	[Input] public ObjectReference _target;

	public GameObject prefab;
	public bool ignoreRotation;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		//_in = GetInputValue("_in", _in);
		_in.InitInput(this, "_in", () =>
		{
			if (prefab == null)
			{
				DebugLogWarning("no prefab setup");
				return;
			}

			_target = GetInputValue("_target", new ObjectReference(Script.gameObject));
			if (!_target.gameObject)
			{
				DebugLogWarning("no valid position setup");
				return;
			}
			
			var instance = Instantiate(prefab);
			instance.transform.position = _target.transform.position;
			if(!ignoreRotation) instance.transform.rotation = _target.transform.rotation;
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}