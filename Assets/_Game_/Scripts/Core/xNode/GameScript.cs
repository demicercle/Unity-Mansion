using UnityEngine;

public interface ISceneLoaded
{
	void OnSceneLoaded();
}

public class GameScript : MonoBehaviour
{
	[SerializeField] private GameScriptGraph graph;

	private GameTurnManager gameTurnManager => GameTurnManager.Instance;
	
	private void OnEnable()
	{
		gameTurnManager.onSceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		gameTurnManager.onSceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded()
	{
		foreach (var node in graph.nodes)
		{
			(node as ISceneLoaded)?.OnSceneLoaded();
		}
	}
}