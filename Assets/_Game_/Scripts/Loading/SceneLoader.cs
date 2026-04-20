using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	private static List<SceneLoader> activeInstances = new List<SceneLoader>();
	public static bool IsLoadingScene => activeInstances.Count > 0;

	public void SetSceneToLoad(string sceneName, LoadSceneMode mode)
	{
		sceneToLoad = sceneName;
		loadSceneMode = mode;
	}

	public void Reload() // TODO Reload scene??
	{
		enabled = true;
		Awake();
		StartCoroutine("Start");
	}

	[SerializeField] private string sceneToLoad;
	[SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;

	private float currentProgress;

	void Awake()
	{
		activeInstances.Add(this);
	}
	
	private IEnumerator Start()
	{
		while (activeInstances[0] != this)
			yield return null; // wait other scene loaders

		if (!string.IsNullOrWhiteSpace(sceneToLoad))
		{
			var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, loadSceneMode);
			while (!asyncOp.isDone)
			{
				currentProgress = asyncOp.progress;
				FadeScreen.SetLoadingProgress(currentProgress);
				yield return null;
			}
		}
		else Debug.LogWarning(this + " cannot load empty scene name");

		activeInstances.Remove(this);
		enabled = false;
	}
}