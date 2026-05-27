using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class HideInFog : MonoBehaviour
{
	public static int debugMode => RoomTrigger.debugMode;

	[SerializeField] private bool collectEquipment;
	[SerializeField] private List<Renderer> equipments = new List<Renderer>();
	[SerializeField] private CharacterEntity character;
	[SerializeField] private List<Renderer> transparentFX = new List<Renderer>();
	[SerializeField] private List<Renderer> renderers = new List<Renderer>();
	[SerializeField] private List<Collider> colliders = new List<Collider>();
	
	private bool isHidden;

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
		if (!character) return;
		if(collectEquipment) CollectEquipment();
		character.onSpawned += RoomChanged;
		GameTurnManager.Instance.onSceneLoaded += RoomChanged;
		RoomTrigger.onChanged += RoomChanged;
		RoomChanged();
	}

	private void OnDisable()
	{
		if (!character) return;
		character.onSpawned -= RoomChanged;
		GameTurnManager.Instance.onSceneLoaded -= RoomChanged;
		RoomTrigger.onChanged -= RoomChanged;
	}

	private void RoomChanged()
	{
		CancelInvoke("UpdateVisibility");
		Invoke("UpdateVisibility", .1f);
	}

	private void UpdateVisibility()
	{
		isHidden = !character.IsCharacterRevealed();
		transparentFX.ForEach(r => r.gameObject.layer = isHidden ? Layers.LayerHidden : Layers.LayerTransparentFX);
		renderers.ForEach(r => r.gameObject.layer = isHidden ? Layers.LayerHidden : Layers.LayerCharacter);
		equipments.ForEach(r => r.gameObject.layer = isHidden ? Layers.LayerHidden : Layers.LayerEquipment);
		colliders.ForEach(c => c.gameObject.layer = isHidden ? Layers.LayerHidden : Layers.LayerCharacter);
		if (character.TryGetAnimation(out var anim))
		{
			foreach (var wp in anim.GetWeaponRenderers())
			{
				wp.gameObject.layer = isHidden ? Layers.LayerHidden : Layers.LayerEquipment;
			}
		}
		if(debugMode > 0) Debug.Log(this + " " + character + " room changed or spawn UpdateVisibility " + isHidden);
	}
	
	[ContextMenu("Collect Equipment")]
	private void CollectEquipment()
	{
		if (!character) return;
		foreach (Renderer equipment in character.GetComponentsInChildren<Renderer>())
		{
			if (equipment.gameObject.layer == Layers.LayerEquipment)
			{
				if (!equipments.Contains(equipment))
				{
					equipments.Add(equipment);
					Utils.SetDirty(this);
				}
			}
		}
	}
}