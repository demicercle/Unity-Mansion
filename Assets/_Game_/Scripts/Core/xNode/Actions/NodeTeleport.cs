using XNode;

[Node.CreateNodeMenu("Actions/Teleport")]
public class NodeTeleport : GameNode
{
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public SceneReference _waypoint;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_character = GetInputValue("_character", _character);
			_waypoint = GetInputValue("_waypoint", _waypoint);
			if (_character.TryGetComponent(this, out CharacterEntity character) &&
			    _waypoint.TryGetComponent(this, out Waypoint waypoint))
			{
				character.StopAgent();
				character.Teleport(waypoint.GetPosition(), waypoint.GetRotation());
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}
}