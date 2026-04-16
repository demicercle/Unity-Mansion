using UnityEngine;

public class Startup : MonoBehaviour
{
	[SerializeField]
	private GameManager _gameManager;
	
	void Start()
	{
		DontDestroyOnLoad(gameObject);
		_gameManager.Start();
	}

	void Update()
	{
		_gameManager.Update();
	}
}