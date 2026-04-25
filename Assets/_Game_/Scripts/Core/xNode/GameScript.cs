using System;
using System.Linq;
using UnityEngine;


public class GameScript : MonoBehaviour
{
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
		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.script = this;
		}
	}

	private void OnDestroy()
	{
		foreach (var node in graph.nodes.OfType<GameNode>())
		{
			node.script = null;
		}
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
		foreach (var node in graph.nodes)
		{
			(node as NodeInteracted)?.Interacted();
		}
	}
}