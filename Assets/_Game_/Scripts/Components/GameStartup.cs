using UnityEngine;
using UnityEngine.SceneManagement;


public class GameStartup : MonoBehaviour
{
	[SerializeField] private string gameScene;
	
	void Start()
	{
		SceneManager.LoadScene(gameScene);
	}
}