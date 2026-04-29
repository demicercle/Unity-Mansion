using UnityEngine;

[CreateNodeMenu("Actions/Instantiate")]
public class NodeInstantiate : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _position;

	public GameObject prefab;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", () =>
		{
			if (prefab == null)
			{
				DebugWarningThis("no prefab setup");
				return;
			}

			_position = GetInputValue("_position", _position);
			if (!_position.TryGetComponent(this, out Transform transform))
			{
				DebugWarningThis("no valid position setup");
				return;
			}
			
			var instance = Instantiate(prefab);
			instance.transform.position = transform.position;
			instance.transform.rotation = transform.rotation;
		});
	}
}