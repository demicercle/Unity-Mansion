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
	[Input] public GameObject _character;
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
		//_in = GetInputValue("_in", _in);
		_in.InitInput(this, "_in", OnCallback);
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_in.Dispose();
	}

	private void OnCallback()
	{
		_character = GetInputValue("_character", Script.gameObject);
		if (_character.TryGetComponent(out CharacterEntity character))
		{
			character.FloatingText(GetInputValue("message", message).GetValue(DialogFiles.script));
			//_out.Invoke();
		}
		else
		{
			DebugLogWarning("no character found");
		}
	}
}