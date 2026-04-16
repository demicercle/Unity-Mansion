using System;
using System.Collections;
using UnityEngine;

public class Startup : MonoBehaviour
{
	[SerializeField]
	private GameManager _gameManager;
	
	private IEnumerator Start()
	{
		DontDestroyOnLoad(gameObject);
		_gameManager.Start();

		while (true)
		{
			if (_gameManager.GetNextCoroutine(out var coroutine))
			{
				yield return StartCoroutine(coroutine);
				_gameManager.RemoveNextCoroutine();
			}
			else
			{
				yield return null;
			}
		}
	}

	private void Update()
	{
		_gameManager.Update();
	}

	private void OnDestroy()
	{
		_gameManager.Dispose();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		_gameManager.hasFocus = hasFocus;
	}

	private void OnGUI()
	{
		if (_gameManager.debugGUI && (Debug.isDebugBuild || Application.isEditor))
		{
			GUILayout.BeginArea(new Rect(0,0,Screen.width,Screen.height));
			GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
			_gameManager.OnGUI();
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}