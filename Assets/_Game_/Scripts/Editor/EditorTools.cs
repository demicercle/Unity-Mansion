using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorTools
{
	#region RevealInFinder
	[MenuItem("Tools/RevealInFinder/persistentDataPath")]
	private static void RevealInFinder_persistentDataPath()
	{
		EditorUtility.RevealInFinder(Application.persistentDataPath);
	}
	[MenuItem("Tools/RevealInFinder/consoleLogPath")]
	private static void RevealInFinder_consoleLogPath()
	{
		EditorUtility.RevealInFinder(Application.consoleLogPath);
	}
	[MenuItem("Tools/RevealInFinder/dataPath")]
	private static void RevealInFinder_dataPath()
	{
		EditorUtility.RevealInFinder(Application.dataPath);
	}
	[MenuItem("Tools/RevealInFinder/streamingAssetsPath")]
	private static void RevealInFinder_streamingAssetsPath()
	{
		EditorUtility.RevealInFinder(Application.streamingAssetsPath);
	}
	[MenuItem("Tools/RevealInFinder/temporaryCachePath")]
	private static void RevealInFinder_temporaryCachePath()
	{
		EditorUtility.RevealInFinder(Application.temporaryCachePath);
	}
	#endregion

	#region MakePrefabsFromSelection

	[MenuItem("Tools/MakePrefabsFromSelection/StaticProp")]
	static public void MakePrefabsFromSelection_StaticProp()
	{
		MakePrefabsFromSelection("LD_", (go) =>
		{
			Utils.GetOrAddComponent<BoxCollider>(go);
			go.isStatic = true;
			go.gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_WALL);
		});
	}

	private static void MakePrefabsFromSelection(string prefix, System.Action<GameObject> process)
	{
		var selection = Selection.gameObjects;
		if (selection.Length == 0) return;
		var folder = EditorUtility.OpenFolderPanel("MakePrefabsFromSelection", "Assets/_Game_/Prefabs", "");
		var excludeNames = new List<string>();
		var newSelection = new List<Object>();
		foreach (var go in selection)
		{
			var goName = go.name;
			if (excludeNames.Contains(goName)) continue;
			
			if (process != null)
			{
				process.Invoke(go);
				EditorUtility.SetDirty(go);
			}
			
			var path = System.IO.Path.Combine(folder, prefix + goName + ".prefab");
			excludeNames.Add(goName);
			var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
			newSelection.Add(prefab);
			EditorGUIUtility.PingObject(prefab);
		}
		AssetDatabase.SaveAssets();
		Selection.objects = newSelection.ToArray();
	}
	#endregion
	
	#region MapCheck
	[MenuItem("Tools/MapCheck/InvalidColliders")]
	private static void MapCheck_InvalidColliders()
	{
		var invalidColliders = new List<Object>();
		var colliders = Object.FindObjectsByType<Collider>(default);
		foreach (var collider in colliders)
		{
			var localScale = collider.transform.localScale;
			if (localScale.x < 0 || localScale.y < 0 || localScale.z < 0)
			{
				invalidColliders.Add(collider.gameObject);
				EditorGUIUtility.PingObject(invalidColliders.Last());
			}
		}

		if (invalidColliders.Count > 0) Selection.objects = invalidColliders.ToArray();
		Debug.Log("Found invalid colliders: " + invalidColliders.Count);
	}
	#endregion

	#region Selection

	[MenuItem("Tools/Selection/SetSelectionDirty")]
	private static void SetSelectionDirty()
	{
		foreach (var o in Selection.objects)
		{
			Debug.Log("SetSelectionDirty: ");
			EditorUtility.SetDirty(o);
		}
	}
	
	[MenuItem("Tools/Selection/SelectParent")]
	private static void SelectParent()
	{
		if (Selection.activeTransform && Selection.activeTransform.parent)
		{
			EditorGUIUtility.PingObject(Selection.activeGameObject = Selection.activeTransform.parent.gameObject);
		}
	}
	
	[MenuItem("Tools/Selection/UnfoldAllGameObjects")]
	private static void UnfoldAllGameObjects()
	{
		// Récupère la fenêtre Hierarchy
		EditorApplication.RepaintHierarchyWindow();
		var hierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
		var hierarchyWindow = EditorWindow.GetWindow(hierarchyWindowType);

		// Récupère la liste des GameObjects dans la scène
		GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();

		// Pour chaque GameObject, déploie son parent dans la Hierarchy
		foreach (GameObject go in allGameObjects)
		{
			if (go.transform.parent != null)
			{
				SetExpandedRecursive(go.transform.parent, true);
			}
			else
			{
				SetExpandedRecursive(go.transform, true);
			}
		}
		
		hierarchyWindow.Repaint();
	}
	
	private static void SetExpandedRecursive(Transform transform, bool expanded)
	{
		// Utilise la réflexion pour accéder à la propriété "expanded" de la Hierarchy
		var sceneHierarchyWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
		var sceneHierarchyWindow = EditorWindow.GetWindow(sceneHierarchyWindowType);
		var sceneHierarchyWindowClass = sceneHierarchyWindow.GetType();

		// Récupère la méthode interne pour définir l'état "expanded"
		MethodInfo setExpandedMethod = sceneHierarchyWindowClass.GetMethod(
			"SetExpandedRecursive",
			BindingFlags.NonPublic | BindingFlags.Instance
		);

		if (setExpandedMethod != null)
		{
			setExpandedMethod.Invoke(sceneHierarchyWindow, new object[] { transform.GetInstanceID(), expanded });
		}
	}
	

	#endregion
	
	#region Transforms

	[MenuItem("Tools/Transform/SetParentOriginToFirstChild")]
	private static void SetParentOriginToFirstChild()
	{
		var parent =  Selection.activeTransform;
		if (parent)
		{
			Undo.RecordObject(parent, "SetParentOriginToFirstChild");
			var newPosition = parent.position;
			var newRotation = parent.rotation;
			var children = new List<Transform>();
			for (int i = 0; i < parent.childCount; i++)
			{
				children.Insert(0, parent.GetChild(i));
				if (i == 0)
				{
					newPosition = children[0].position;
					newRotation = children[0].rotation;
				}
				children[0].SetParent(null, true);
			}

			parent.position = newPosition;
			parent.rotation = newRotation;

			foreach (var child in children)
				child.SetParent(parent, true);

			EditorUtility.SetDirty(parent);
		}
	}
	#endregion
}