using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
	private static List<SceneLoader> activeInstances = new List<SceneLoader>();
	public static bool AnyInstanceActive => activeInstances.Count > 0;

	public void Reload() // TODO Reload scene??
	{
		enabled = true;
		Awake();
		StartCoroutine("Start");
	}

	public void LoadNow() => autoLoad = true;

	[SerializeField] private List<string> scenesToLoad = new List<string>();
	[SerializeField] private LoadSceneMode loadSceneMode = LoadSceneMode.Additive;
	[SerializeField] private bool autoLoad = true;

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
		while (!autoLoad)
			yield return null;
		
		while (activeInstances.First() != this)
		{
			yield return null; // wait other scene loaders
		}

		foreach (var sceneToLoad in scenesToLoad)
		{
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
		}
		
		enabled = false;
	}
}