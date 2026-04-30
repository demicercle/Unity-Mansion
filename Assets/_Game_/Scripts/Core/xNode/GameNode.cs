using System;
using UnityEngine;
using XNode;

public abstract class GameNode : Node
{
	private GameScript _script;

	public GameScript script
	{
		get => _script;
		set
		{
			if(_script != null) 
				OnScriptDestroy();
			
			_script = value;
			
			if(_script != null) 
				OnScriptStart();
		}
	}
	
	virtual protected void OnScriptStart()
	{
		DebugLogThis("OnScriptStart");
	}

	virtual protected void OnScriptDestroy()
	{
		DebugLogThis("OnScriptDestroy");
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
		DebugLogThis("Inits");
	}

	protected void DebugLogThis(string msg)
	{
		if(GameScript.debugMode > 0) Debug.Log("x_" + this + ": " + msg);
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
	
	public void StartInput(Node node, string inputName, System.Action callback)
	{
		if (!Application.isPlaying)
		{
			Debug.Log(this + " app not playing");
			return;
		}
		
		//Debug.Log(this + " AwakeInput " + node + " " + inputName + " " + callback);
		
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
	public string findName = "";

	public GameObject GetGameObject(GameNode node)
	{
		if (!_gameObject)
		{
			if (!string.IsNullOrWhiteSpace(findName))
				_gameObject = GameObject.Find(findName);
			else
				_gameObject = node.script.gameObject;
		}
		
		return _gameObject;
	}

	public T GetComponent<T>(GameNode node) where T : Component
	{
		return GetGameObject(node).GetComponent<T>();
	}

	public bool TryGetComponent<T>(GameNode node, out T component) where T : Component
	{
		return (component = GetComponent<T>(node));
	}

	private GameObject _gameObject;
}