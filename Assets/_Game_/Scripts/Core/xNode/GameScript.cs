using System.Collections;
using System.Linq;
using UnityEngine;


public class GameScript : MonoBehaviour
{
	public static int debugMode { get; set; }

	public void DestroyScript(bool destroyGameObject)
	{
		if(destroyGameObject)
			Destroy(gameObject);
		else
			Destroy(this);
	}
	
	[SerializeField] private GameScriptGraph graph;

	public void SetGraph(GameScriptGraph newGraph)
	{
		graph = newGraph;
	}
	
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;
	
	private void OnDestroy()
	{
		if (!graph) return;
		if (!Application.isPlaying) return;
		
		if(debugMode > 0) Debug.Log(this + " OnDestroy");
		
		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.Script = null;
		}
	}

	private void OnEnable()
	{
		if (!graph)
		{
			Debug.LogWarning(this + " has no graph, destroys");
			enabled = false;
			return;
		}
		
		gameTurnManager.onSceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		if (!graph) return;
		gameTurnManager.onSceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded()
	{
		graph = Instantiate(graph);
		
		if(debugMode > 0) Debug.Log(this + " OnSceneLoaded " + graph);

		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.Script = this;
		}
	}
}