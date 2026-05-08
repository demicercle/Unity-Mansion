using UnityEngine;

[CreateNodeMenu("Actions/Instantiate")]
public class NodeInstantiate : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _position;

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

			_position = GetInputValue("_position", _position);
			if (!_position.TryGetComponent(this, out Transform transform))
			{
				DebugLogWarning("no valid position setup");
				return;
			}
			
			var instance = Instantiate(prefab);
			instance.transform.position = transform.position;
			if(!ignoreRotation) instance.transform.rotation = transform.rotation;
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}