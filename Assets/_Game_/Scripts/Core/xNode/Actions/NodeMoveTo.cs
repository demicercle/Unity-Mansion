using XNode;
using UnityEngine;

[CreateNodeMenu("Actions/Move To")]
public class NodeMoveTo : GameNode
{
	[Input] public NodeExec _start;
	[Input] public SceneReference _character;
	[Input] public SceneReference _to;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		//_start = GetInputValue("_start", _start);
		_start.InitInput(this, "_start", () =>
		{
			_character = GetInputValue("_character", _character);
			_to = GetInputValue("_to", _to);
			if (_character.TryGetComponent(this, out CharacterEntity character)
			    && _to.TryGetComponent(this, out Waypoint waypoint))
			{
				DebugLog("Assign move: " + character + " to waypoint: " + waypoint);
				character.Routines.AssignMoveToWaypoint(waypoint);
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_start.Dispose();
	}
}