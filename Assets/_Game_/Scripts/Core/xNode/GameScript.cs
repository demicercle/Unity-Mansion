using System;
using System.Linq;
using UnityEngine;


public class GameScript : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	[SerializeField] private GameScriptGraph graph;
	
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;
	
	[SerializeField] private PropComponent selfProp;
	[SerializeField] private CharacterController selfCharacter;

	private void OnDrawGizmosSelected()
	{
		if(!Application.isPlaying)
			TryGetComponents();
	}
	
	private void TryGetComponents()
	{
		if(!selfProp) TryGetComponent(out selfProp);
		if(!selfCharacter) TryGetComponent(out selfCharacter);
	}

	private void Awake()
	{
		if (!graph)
		{
			Debug.LogWarning(this + " has no graph!");
			enabled = false;
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

		TryGetComponents();
		
		if (selfProp)
		{
			selfProp.onInteracted += OnInteracted;
		}

		if (selfCharacter)
		{
			selfCharacter.onTalkedBy += OnTalkedBy;
		}
	}

	private void OnDisable()
	{
		if (!graph) return;
		
		gameTurnManager.onSceneLoaded -= OnSceneLoaded;
		
		if (selfProp)
		{
			selfProp.onInteracted -= OnInteracted;
		}

		if (selfCharacter)
		{
			selfCharacter.onTalkedBy -= OnTalkedBy;
		}
	}

	private void OnSceneLoaded()
	{
		foreach (var node in graph.nodes)
		{
			(node as NodeSceneLoaded)?.OnSceneLoaded();
		}
	}

	private void OnInteracted()
	{
		foreach (var node in graph.nodes)
		{
			(node as NodeInteracted)?.Interacted();
		}
	}

	private void OnTalkedBy(CharacterController other)
	{
		if(debugMode > 0) Debug.Log(this + " OnTalkedBy " + other);
		foreach (var node in graph.nodes)
		{
			(node as NodeInteracted)?.Interacted();
		}
	}
}