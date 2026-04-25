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
				OnScriptAwake();
		}
	}
	
	virtual protected void OnScriptAwake()
	{
		DebugLogThis("OnScriptAwake");
	}

	virtual protected void OnScriptDestroy()
	{
		DebugLogThis("OnScriptDestroy");
	}
	
	protected override void Init()
	{
		base.Init();
		DebugLogThis("Inits");
	}

	protected void DebugLogThis(string msg) => Debug.Log("x_" + this + ": " + msg);
}


[System.Serializable]
public class NodeExec
{
	private System.Action callback;
	
	public void Invoke() => callback?.Invoke();
	
	public void AwakeInput(Node node, string inputName, System.Action callback)
	{
		if (!Application.isPlaying)
		{
			Debug.Log(this + " app not playing");
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
	private GameObject value;
	
	public string findName = "";

	public bool TryGetComponent<T>(GameNode node, out T component) where T : Component
	{
		value = GetGameObject(node);
		component = null;
		return value && value.TryGetComponent(out component);
	}

	public GameObject GetGameObject(GameNode node)
	{
		if (!string.IsNullOrWhiteSpace(findName))
			return GameObject.Find(findName);
		else
			return node.script.gameObject;
	}

	public T GetComponent<T>(GameNode node) where T : Component
	{
		if (!string.IsNullOrWhiteSpace(findName))
			return GameObject.Find(findName)?.GetComponent<T>();
		else
			return node.script.GetComponent<T>();
	}
}