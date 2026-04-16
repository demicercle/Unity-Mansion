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

	private void LateUpdate()
	{
		_gameManager.LateUpdate();
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
		if (_gameManager.debugGUI > 0f && (Debug.isDebugBuild || Application.isEditor))
		{
			GUILayout.BeginArea(new RectOffset(5,5,5,5).Remove(new Rect(0,0,Screen.width * _gameManager.debugGUI,Screen.height)));
			GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
			_gameManager.OnGUIDebug();
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}