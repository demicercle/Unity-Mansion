using System;
using System.Collections;
using UnityEngine;

public class Startup : MonoBehaviour
{
	public GameView gameView;
	
	private void Awake()
	{
		gameView.startupComponent = this; // TODO (only used for coroutine)
		gameView.Awake();
	}

	private IEnumerator Start()
	{
		DontDestroyOnLoad(gameObject);
		
		StartCoroutine(gameView.Start());

		while (true)
		{
			if (gameView.GetNextCoroutine(out var coroutine))
			{
				yield return StartCoroutine(coroutine);
				gameView.RemoveNextCoroutine();
			}
			else
			{
				yield return null;
			}
		}
	}

	private void Update()
	{
		gameView.Update();
	}

	private void LateUpdate()
	{
		gameView.LateUpdate();
	}

	private void OnDestroy()
	{
		gameView.Dispose();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		gameView.hasFocus = hasFocus;
	}

	private void OnGUI()
	{
		if (gameView.debugGUI > 0f && (Debug.isDebugBuild || Application.isEditor))
		{
			GUILayout.BeginArea(new RectOffset(5,5,5,5).Remove(new Rect(0,0,Screen.width * gameView.debugGUI,Screen.height)));
			GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
			gameView.OnGUIDebug();
			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}