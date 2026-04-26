using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using XNode;

[CreateNodeMenu("Actions/Floating Text")]
public class NodeFloatingText : GameNode
{
	//[Output] public NodeExec _out;
	[Input] public NodeExec _in;
	[Input] public SceneReference _character;
	[Input] public LocalizedText message;

	[ContextMenu("Parse")]
	public void Parse()
	{
		message.Parse(DialogFiles.script);
	}

	protected override void Init()
	{
		base.Init();
		Parse();
	}
	
	// Use this for initialization
	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_in.StartInput(this, "_in", OnCallback);
	}

	private void OnCallback()
	{
		if (_character.TryGetComponent(this, out CharacterController character))
		{
			character.FloatingText(GetInputValue("message", message).GetValue(DialogFiles.script));
			//_out.Invoke();
		}
	}
}