using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Animation))]
public class ItemAnimation : MonoBehaviour
{
	private static List<ItemAnimation> activeInstances  = new List<ItemAnimation>();
	
	public enum TriggerType
	{
		None = -1,
		Loot = 0,
		Use = 1
	}

	[SerializeField] private float startDelay = 0.5f;
	[SerializeField] private RawImage iconContainer;
	[SerializeField] private TriggerType triggerType;

	public UnityEvent onPlayed;
	
	private bool displayOnLoot => triggerType == TriggerType.Loot;
	private bool displayOnUse => triggerType == TriggerType.Use;

	private ScreenFollowsWorld screenFollowsWorld;
	private Inventory inventoryFrom, inventoryTo;
	private RectTransform rectTransform;
	private bool isClone;
	private new Animation animation;

	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (Application.isPlaying) return;
		var newName = typeof(ItemAnimation) + "_" + triggerType;
		if (newName != name)
		{
			name = newName;
			Utils.SetDirty(this);
		}
	}
	#endif

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		animation = GetComponent<Animation>();
		animation.playAutomatically = false;
		screenFollowsWorld = Utils.GetOrAddComponent<ScreenFollowsWorld>(gameObject);
	}

	private void OnDestroy()
	{
		if (!isClone)
		{
			if(displayOnLoot) Inventory.onAnyItemTransfered -= OnAnyItemTransfered;
			if(displayOnUse) Inventory.onAnyItemUsed -= OnAnyItemUsed;
		}
	}

	private void OnAnyItemTransfered(Inventory fromInventory, Inventory toInventory, ItemData itemData)
	{
		var instance = Instantiate(this, transform.parent).GetComponent<ItemAnimation>();
		instance.gameObject.SetActive(true);
		instance.isClone = true;
		instance.iconContainer.texture = itemData.GetIcon();
		instance.inventoryFrom = fromInventory;
		instance.inventoryTo = toInventory;
	}

	private void OnAnyItemUsed(Inventory fromInventory, ItemData itemData, object objectTo)
	{
		var instance = Instantiate(this, transform.parent).GetComponent<ItemAnimation>();
		instance.gameObject.SetActive(true);
		instance.isClone = true;
		instance.iconContainer.texture = itemData.GetIcon();
		instance.inventoryFrom  = fromInventory;
		if(objectTo is CharacterEntity) 
			instance.inventoryFrom = (objectTo as CharacterEntity).GetComponent<Inventory>();
	}

	private IEnumerator Start()
	{
		if (!isClone)
		{
			if(displayOnLoot) Inventory.onAnyItemTransfered += OnAnyItemTransfered;
			if(displayOnUse) Inventory.onAnyItemUsed += OnAnyItemUsed;
			
			gameObject.SetActive(false);
			yield break;
		}

		activeInstances.Add(this);
		
		iconContainer.enabled = false;
		yield return new WaitForSeconds(activeInstances.Count * startDelay);
		iconContainer.enabled = true;
		
		animation.clip = animation.GetClip(typeof(ItemAnimation) + "_" + triggerType.ToString());
		animation.Play(animation.clip.name);
		onPlayed?.Invoke();
		
		do
		{
			yield return null;
		} while (animation.isPlaying);
		
		Destroy(gameObject);
	}

	private void LateUpdate()
	{
		if (inventoryFrom)
		{
			if (!inventoryTo)
			{
				screenFollowsWorld.SetWorldOffset(inventoryFrom.GetPosition());
			}
			else
			{
				screenFollowsWorld.SetWorldOffset(Vector3.Lerp(inventoryFrom.GetPosition(), inventoryTo.GetPosition(),
					animation[animation.clip.name].normalizedTime));
			}
		}
	}
}