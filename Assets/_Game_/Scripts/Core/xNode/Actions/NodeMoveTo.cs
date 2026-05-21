using XNode;
using UnityEngine;

[CreateNodeMenu("Actions/Move To")]
public class NodeMoveTo : GameNode
{
	[Input] public NodeExec _start;
	[Input] public GameObject _character;
	[Input] public GameObject _destination;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		//_start = GetInputValue("_start", _start);
		_start.InitInput(this, "_start", () =>
		{
			_character = GetInputValue("_character", Script.gameObject);
			if (!_character.TryGetComponent(out CharacterEntity character))
			{
				DebugLogWarning("no character found");
				return;
			}

			_destination = GetInputValue("_destination", Script.gameObject);
			if (_destination.TryGetComponent(out Waypoint waypoint))
			{
				DebugLog("Assign move: " + character + " to waypoint: " + waypoint);
				character.Routines.AssignMoveToWaypoint(waypoint);
			}
			else
			{
				DebugLog("Assign move: " + character + " to destination: " + _destination.transform.position);
				character.Routines.AssignMoveToDestination(_destination.transform.position, character.GetAgent().GetMinReachDistance(), !character.IsControllableByPlayer());
			}
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_start.Dispose();
	}
}