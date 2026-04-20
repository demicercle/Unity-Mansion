using UnityEngine;
using UnityEngine.SceneManagement;


public class Startup : MonoBehaviour
{
	[SerializeField] private string gameScene;
	
	void Start()
	{
		FadeScreen.FadeIn(true);
		SceneManager.LoadScene(gameScene);
	}
}