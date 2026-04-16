using System;
using UnityEngine;

public class Startup : MonoBehaviour
{
	[SerializeField]
	private GameManager _gameManager;
	
	private void Start()
	{
		DontDestroyOnLoad(gameObject);
		_gameManager.Start();
	}

	private void Update()
	{
		_gameManager.Update();
	}

	private void OnDestroy()
	{
		_gameManager.Dispose();
	}
}