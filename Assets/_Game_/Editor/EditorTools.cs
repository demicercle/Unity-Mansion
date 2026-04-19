using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EditorTools
{
	#region OpenScenes

	private const string SCENE_PATH = "Assets/_Game_/Scenes/";
	
	[MenuItem("Tools/OpenScenes/GameScenes/Level_0")]
	static void OpenScenes_GameScene() => OpenScenes("GameScene", "Level_0", "Level_0_Env");
	
	[MenuItem("Tools/OpenScenes/StartScene")]
	static void OpenScenes_StartScene() => OpenScenes("StartScene");

	static void OpenScenes(params string[] scenes)
	{
		for (int i = 0; i < scenes.Length; i++)
		{
			EditorSceneManager.OpenScene(SCENE_PATH + scenes[i] + ".unity", i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
		}
	}
	#endregion
	
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

	static void MakePrefabsFromSelection(string prefix, System.Action<GameObject> process)
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
	static void MapCheck_InvalidColliders()
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
}