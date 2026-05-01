using System;
using UnityEngine;
using XNode;

public abstract class GameNode : Node
{
	private GameScript _script;

	public GameScript Script
	{
		get => _script;
		set
		{
			if(_script) 
				OnScriptDestroy();
			
			_script = value;
			
			if(_script) 
				OnScriptStart();
		}
	}
	
	protected virtual void OnScriptStart()
	{
		DebugLogThis("OnScriptStart", 1);
	}

	protected virtual void OnScriptDestroy()
	{
		DebugLogThis("OnScriptDestroy", 1);
	}

	private void Reset()
	{
		#if UNITY_EDITOR
		this.name = UnityEditor.ObjectNames.NicifyVariableName(GetType().ToString().Replace("Node", string.Empty)
			.Trim(' '));
#endif
	}

	protected override void Init()
	{
		base.Init();
		_script = null;
		DebugLogThis("Inits", 1);
	}

	protected void DebugLogThis(string msg, int minDebugMode = 0)
	{
		if(GameScript.debugMode > minDebugMode) Debug.Log("x_" + this + ": " + msg + "\nin graph=" + graph);
	}
	
	protected void DebugWarningThis(string msg)
	{
		Debug.LogWarning("x_" + this + ": " + msg);
	}
}


[System.Serializable]
public class NodeExec
{
	private System.Action callback;
	
	public void Invoke() => callback?.Invoke();

	public void Dispose()
	{
		callback = null;
	}
	
	public void InitInput(Node node, string inputName, System.Action callback)
	{
		if (!Application.isPlaying)
		{
			return;
		}
		
		otherExec = node.GetInputPort(inputName).GetInputValue<NodeExec>();
		if (otherExec != null)
		{
			otherExec.callback += callback;
			return;
		}
		
		Debug.LogWarning(this + " can't connect from " + node + " " + inputName + " in " + node.graph);
	}

	public bool HasCallbacks()
	{
		return callback?.Target != null;
	}

	private static NodeExec otherExec;
}

[System.Serializable]
public class SceneReference
{
	public override string ToString()
	{
		return "x_SceneReference";
	}

	public void SetGameObject(GameObject newObject)
	{
		_gameObject = newObject;
	}

	public GameObject GetGameObject(GameNode node)
	{
		if (!_gameObject)
			_gameObject = node.Script.gameObject;
		return _gameObject;
	}

	public T GetComponent<T>(GameNode node) where T : Component
	{
		return GetGameObject(node).GetComponent<T>();
	}

	public bool TryGetComponent<T>(GameNode node, out T component) where T : Component
	{
		return GetGameObject(node).TryGetComponent<T>(out component);
	}

	private GameObject _gameObject;
}