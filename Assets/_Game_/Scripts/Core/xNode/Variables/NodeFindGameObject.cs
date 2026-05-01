
using UnityEngine;
using XNode;

[CreateNodeMenu("Variables/Find GameObject")]
public class NodeFindGameObject : GameNode
{
	[Input] public string findName;
	[Output] public SceneReference _out;

	private GameObject _gameObject;
	
	#if UNITY_EDITOR
	[ContextMenu("Find in editor")]
	private void FindInEditor()
	{
		var obj = GameObject.Find(findName);
		if (obj)
		{
			UnityEditor.Selection.activeGameObject = obj;
			UnityEditor.EditorGUIUtility.PingObject(obj);
		}
		else
		{
			UnityEditor.EditorUtility.DisplayDialog("Find in editor", "Object not found!", "Sh*t");
		}
	}
	#endif
	
	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "_out")
		{
			if (!_gameObject)
			{
				findName = GetInputValue("findName", findName);
				_gameObject = GameObject.Find(findName);
			}

			if (!_gameObject)
			{
				Debug.LogError(this + " cannot find GameObject " + findName);
			}
			else
			{
				_out.SetGameObject(_gameObject);
			}
			
			return _out;
		}
		
		return base.GetValue(port);
	}
}