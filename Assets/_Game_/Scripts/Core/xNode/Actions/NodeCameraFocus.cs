using UnityEngine;

[CreateNodeMenu("Actions/Camera Focus")]
public class NodeCameraFocus : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _target;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_target = GetInputValue("_target", _target);
			if (_target.TryGetComponent(this, out Waypoint waypoint))
			{
				CameraPivot.Instance.Focus(waypoint.transform);
				CameraPivot.Instance.OrbitTowards(waypoint.GetRotation());
			}
			else if (_target.TryGetComponent(this, out CharacterEntity character))
			{
				CameraPivot.Instance.Focus(character);
			}
			else if (_target.TryGetComponent(this, out Transform transform))
			{
				CameraPivot.Instance.Focus(transform);
			}
		});
	}
}