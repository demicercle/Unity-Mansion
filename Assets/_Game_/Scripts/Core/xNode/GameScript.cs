using System;
using UnityEngine;


public class GameScript : MonoBehaviour
{
	[SerializeField] private GameScriptGraph graph;
	
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;
	
	[SerializeField] [ReadOnly] private PropComponent prop;

	private void OnDrawGizmosSelected()
	{
		TryGetComponent(out prop);
	}

	private void OnEnable()
	{
		gameTurnManager.onSceneLoaded += OnSceneLoaded;
		
		prop = GetComponent<PropComponent>();
		if (prop)
		{
			prop.onInteracted += OnInteracted;
		}
	}

	private void OnDisable()
	{
		gameTurnManager.onSceneLoaded -= OnSceneLoaded;
		
		if (prop)
		{
			prop.onInteracted -= OnInteracted;
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
}