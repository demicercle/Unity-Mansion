/*using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LevelView : MonoBehaviour
{
	private LevelData levelData = null;

	public IEnumerator LoadLevel(LevelData newData = null)
	{
		if (newData == null)
		{
			Debug.LogWarning(this + " Init no data, create one");
			newData = ScriptableObject.CreateInstance<LevelData>();
		}
		
		var allScenes = newData.GetScenes();
		if (allScenes.Length <= 0)
		{
			Debug.LogWarning(newData + " has no scenes!");
		}
		
		foreach (var scene in allScenes)
		{
			Debug.Log(this + " load scene: " + scene);
			var async = SceneManager.LoadSceneAsync(scene);
			do
				yield return null;
			while (!async.isDone);
		}
	}
}*/