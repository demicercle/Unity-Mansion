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
		DebugLog("OnScriptStart " + _script, 1);
	}

	protected virtual void OnScriptDestroy()
	{
		DebugLog("OnScriptDestroy " + _script, 1);
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
		DebugLog("Inits", 1);
	}

	protected void DebugLog(string msg, int minDebugMode = 0)
	{
		if(GameScript.debugMode > minDebugMode) Debug.Log("x_" + this + ": " + msg + "\nin graph=" + graph);
	}
	
	protected void DebugLogWarning(string msg)
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
//			Debug.Log(this + " connect " + node + " to " + inputName);
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
		_go = newObject;
		if(GameScript.debugMode > 1) Debug.Log(this + " SetGameObject _go=" + newObject);
	}

	public GameObject GetGameObject(GameNode node)
	{
		if (!_go)
			SetGameObject(node.Script.gameObject);
		if(GameScript.debugMode > 1) Debug.Log(this + " GetGameObject node=" + node + " _go=" + _go + " node.Script=" + node.Script);
		return _go;
	}

	public T GetComponent<T>(GameNode node) where T : Component
	{
		return GetGameObject(node).GetComponent<T>();
	}

	public bool TryGetComponent<T>(GameNode node, out T component) where T : Component
	{
		return GetGameObject(node).TryGetComponent<T>(out component);
	}

	public SceneReference()
	{
		_go = null;
		//Debug.Log(this + " constructor _go=" + _go);
	}

	private GameObject _go;
}