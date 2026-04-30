using XNode;
using UnityEngine;

[CreateNodeMenu("Actions/Move To")]
public class NodeMoveTo : GameNode
{
	[Input] public NodeExec _start;
	[Input] public SceneReference _character;
	[Input] public SceneReference _to;
	[Input] public bool canUseRunAction;
	[Space]	
	[Output] public NodeExec _finished;
	

	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_finished")
			return _finished;
		return base.GetValue(port);
	}

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_start.StartInput(this, "_start", OnStart);
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_start.Dispose();
	}

	private void OnStart()
	{
		DebugLogThis("OnStart");
		if (_character.TryGetComponent(this, out CharacterEntity character))
		{
			if (_to.TryGetComponent(this, out Waypoint waypoint))
			{
				character.Routines.AssignMoveToWaypoint(waypoint, canUseRunAction, OnComplete);
			}
		}
	}

	private void OnComplete()
	{
		_finished.Invoke();
	}
}