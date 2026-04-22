using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HideInFog : MonoBehaviour
{
	[SerializeField] private CharacterController character;
	[SerializeField] private List<Renderer> renderers = new List<Renderer>();
	[SerializeField] private List<Collider> colliders = new List<Collider>();
	[SerializeField] private bool debugLog;
	
	private bool isHidden;
	private int currentLayer;

	private void Awake()
	{
		if (character == null)
		{
			Debug.LogWarning(this + " has no character, disabling");
			enabled = false;
		}
	}

	private void OnEnable()
	{
		character.onSpawned += OnSpawned;
		RoomTrigger.onChanged += UpdateVisibility;
	}

	private void OnDisable()
	{
		if (character) character.onSpawned -= OnSpawned;
		RoomTrigger.onChanged -= UpdateVisibility;
	}

	private void OnSpawned()
	{
		Invoke("UpdateVisibility", 0.1f);
	}

	private void UpdateVisibility()
	{
		CancelInvoke("UpdateVisibility");
		isHidden = !character.IsCharacterRevealed();
		currentLayer = isHidden ? Layers.LayerHidden : Layers.LayerCharacter;
		renderers.ForEach(r => r.gameObject.layer = currentLayer);
		colliders.ForEach(c => c.gameObject.layer = currentLayer);
		if(debugLog) Debug.Log(this + " " + character + " UpdateVisibility " + isHidden);
	}
}