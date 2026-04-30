using System;
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

	private void Start()
	{
		if (!graph)
		{
			Debug.LogWarning(this + " has no graph, destroys");
			DestroyScript(false);
			return;
		}
		
		graph = Instantiate(graph);
		
		if(debugMode > 0) Debug.Log(this + " Start " + graph);
		#if UNITY_EDITOR
		if (debugMode > 1) UnityEditor.Selection.activeObject = graph;
		#endif

		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.script = this;
		}
	}

	private void OnDestroy()
	{
		if (!graph) return;
		if (!Application.isPlaying) return;
		
		if(debugMode > 0) Debug.Log(this + " OnDestroy");
		
		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.script = null;
		}
			
		//Destroy(graph);
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
		foreach (var node in graph.nodes.OfType<ISceneLoaded>())
		{
			(node as ISceneLoaded)?.OnSceneLoaded();
		}
	}
}