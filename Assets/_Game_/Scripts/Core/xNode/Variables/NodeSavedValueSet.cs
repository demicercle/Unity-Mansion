using UnityEngine;

[CreateNodeMenu("Variables/Saved Value/Set")]
public class NodeSavedValueSet : GameNode
{
	public string valueName;
	
	[Input] public NodeExec _set;
	[Input] public NodeExec _add;
	[Input] public int _newValue;

	protected override void OnScriptStart()
	{
		base.OnScriptStart();
		_set.InitInput(this, "_set", () =>
		{
			_newValue = GetInputValue("_newValue", _newValue);
			DebugLog("_set " + valueName + " to " + _newValue);
			GameSave.Current.SetSavedValue(valueName, _newValue);
		});
		_add.InitInput(this, "_add", () =>
		{
			_newValue = GetInputValue("_newValue", _newValue);
			DebugLog("_add " + _newValue + " to " + valueName);
			if(GameSave.Current.TryGetSavedValue(valueName, out var oldValue))
				GameSave.Current.SetSavedValue(valueName, oldValue + _newValue);
			else 
				GameSave.Current.SetSavedValue(valueName, _newValue);
		});
	}

	protected override void OnScriptDestroy()
	{
		base.OnScriptDestroy();
		_set.Dispose();
		_add.Dispose();
	}
}