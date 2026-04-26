using System;
using System.Linq;
using UnityEngine;


public class GameScript : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	[SerializeField] private GameScriptGraph graph;

	public void SetGraph(GameScriptGraph newGraph)
	{
		graph = newGraph;
	}
	
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;

	private void Start()
	{
		if (!graph)
		{
			Destroy(this);
			return;
		}
		
		graph = Instantiate(graph);

		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.script = this;
		}
	}

	private void OnDestroy()
	{
		if (!graph) return;
		
		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.script = null;
		}
			
		Destroy(graph);
	}

	private void OnEnable()
	{
		gameTurnManager.onSceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		if (!graph) return;
		gameTurnManager.onSceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded()
	{
		foreach (var node in graph.nodes)
		{
			(node as NodeSceneLoaded)?.OnSceneLoaded();
		}
	}
}