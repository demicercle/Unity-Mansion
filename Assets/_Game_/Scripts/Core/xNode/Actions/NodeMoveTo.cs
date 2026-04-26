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

	protected override void OnScriptAwake()
	{
		base.OnScriptAwake();
		_start.AwakeInput(this, "_start", OnStart);
	}

	private void OnStart()
	{
		DebugLogThis("OnStart");
		if (_character.TryGetComponent(this, out CharacterController character))
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