using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		Debug.Log(this + " Awake");
		activeInstances.Add(this);
	}

	private void OnDisable()
	{
		Debug.Log(this + " OnDisable");
		activeInstances.Remove(this);
	}

	private IEnumerator Start()
	{
		while (activeInstances.First() != this)
		{
			yield return null; // wait other scene loaders
		}

		if (!string.IsNullOrWhiteSpace(sceneToLoad))
		{
			if (SceneManager.GetSceneByName(sceneToLoad).isLoaded)
			{
				Debug.Log(this + " skips load scene: " + sceneToLoad + " because already loaded");
			}
			else
			{
				Debug.Log(this + " loads scene: " + sceneToLoad + " " + loadSceneMode);
				var asyncOp = SceneManager.LoadSceneAsync(sceneToLoad, loadSceneMode);
				while (!asyncOp.isDone)
				{
					currentProgress = asyncOp.progress;
					FadeScreen.SetLoadingProgress(currentProgress);
					yield return null;
				}
			}
		}
		else Debug.LogWarning(this + " cannot loads empty scene name");

		enabled = false;
	}
}