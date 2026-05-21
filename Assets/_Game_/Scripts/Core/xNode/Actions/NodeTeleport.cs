using UnityEngine;
using XNode;

[Node.CreateNodeMenu("Actions/Teleport")]
public class NodeTeleport : GameNode
{
	[Input] public NodeExec _in;
	[Input] public GameObject _character;
	[Input] public GameObject _waypoint;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.InitInput(this, "_in", () =>
		{
			_character = GetInputValue("_character", Script.gameObject);
			_waypoint = GetInputValue("_waypoint", Script.gameObject);
			if (_character.TryGetComponent(out CharacterEntity character) &&
			    _waypoint.TryGetComponent(out Waypoint waypoint))
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