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

	private void OnGUI()
	{
		_gameManager.OnGUI();
	}
}