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
	
	[SerializeField] private GameScriptGraph startGraph;

	private GameScriptGraph graph;
	
	public void SetGraph(GameScriptGraph newGraph)
	{
		if (graph)
		{
			if(debugMode > 0) Debug.Log(this + " Dispose graph");
			
			foreach (var node in graph.nodes.OfType<GameNode>())
			{
				node.Script = null;
			}

			Destroy(graph);
			graph = null;
		}

		if (newGraph)
		{
			graph = Instantiate(newGraph);
			if(debugMode > 0) Debug.Log(this + " SetGraph " + graph);
		
			foreach (var node in graph.nodes.OfType<GameNode>())
			{
				node.Script = this;
			}
		}
	}

	private GameObject _instigator;

	public void SetInstigator(GameObject newInstigator)
	{
		_instigator = newInstigator;
	}

	public GameObject GetInstigator() => _instigator;
	
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;

	private void OnEnable()
	{
		gameTurnManager.onSceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		gameTurnManager.onSceneLoaded -= OnSceneLoaded;
		SetGraph(null);
	}

	private void OnSceneLoaded()
	{
		if(debugMode > 0) Debug.Log(this + " OnSceneLoaded");

		if (startGraph)
			SetGraph(startGraph);
	}
}