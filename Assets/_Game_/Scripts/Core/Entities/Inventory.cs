using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Inventory : MonoBehaviour
{
	private static List<Inventory> allInventories = new List<Inventory>();

	public static Inventory[] GetSavedInventories() => allInventories.Where(i => i.saveInventory && !i.IsPartyInventory()).ToArray();
	
	public static Inventory[] GetLootBags() => allInventories.Where(i => i.isLootBag).ToArray();

	public static Inventory CreateLootBag(Inventory lootBagPrefab, Vector3 navPosition)
	{
		var bag = Instantiate(lootBagPrefab.gameObject).GetComponent<Inventory>();
		bag.isLootBag = true;
		bag.maxSize = 1;
		bag.destroyIfEmpty = true;
		NavUtils.TryGetNearestNavPosition(navPosition, CreateLootNavDistance, out navPosition);
		bag.transform.position = navPosition;
		lootBagPrefab.gameObject.SetActive(true);
		return bag;
	}
	
	private const float CreateLootNavDistance = 1.0f;
	
	public string GetName() => name;
	public bool IsName(string s) => s == name;
	
	public static int debugMode { get; set; }
	public static event System.Action<Inventory> onAnyInventoryChanged;
	public static event System.Action<Inventory, ItemData> onAnyItemAdded, onAnyItemRemoved;
	public static event System.Action<Inventory, Inventory, ItemData> onAnyItemTransfered;
	public static event System.Action<Inventory, ItemData, object> onAnyItemUsed;
	
	[SerializeField] [Range(1, 32)] private int maxSize = 6;
	[SerializeField] private Inventory lootBagPrefab = null;
	[SerializeField] private bool saveInventory;
	[SerializeField] private List<ItemData> items = new List<ItemData>();
	
	private bool isLootBag;
	
	public string[] GetItemNames()
	{
		return items.ConvertAll(i => !i ? "null" : i.name).ToArray();
	}

	public void SetItemNames(string[] slots)
	{
		if(slots == null) return;
		for (int i = 0; i < slots.Length; i++)
		{
			SetItemName(i, slots[i]);
		}
	}
	
	public void SetItemName(int index, string itemName)
	{
		if (index < 0 || index >= items.Count)
		{
			Debug.LogWarning(this + " invalid SetItemName index: " + index + " items=" + items.Count);
			return;
		}
		
		var newItem = ItemData.GetItemFromName(itemName);
		
		var lastItem = items[index];
		if (lastItem)
		{
			onAnyItemRemoved?.Invoke(this, lastItem);
		}
		
		items[index] = newItem;

		if (items[index])
		{
			onAnyItemAdded?.Invoke(this, items[index]);
		}
		
		onAnyInventoryChanged?.Invoke(this);
	}
	
	public bool TryCreateLootBag(ItemData itemData, out Inventory bag)
	{
		if (!lootBagPrefab)
		{
			Debug.LogWarning(this + " cannot CreateLootBag because no prefab set!");
			bag = null;
			return false;
		}

		if (!NavUtils.TryGetNearestNavPosition(transform.position, CreateLootNavDistance, out var navPosition))
		{
			Debug.LogWarning(this + " cannot CreateLootBag: no nav position found");
			bag = null;
			return false;
		}

		bag = CreateLootBag(lootBagPrefab, navPosition);
		bag.TryAddItem(itemData);
		bag.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
		return true;
	}

	public bool IsEmpty() => items.All(it => !it);

	public bool DropOnGround(out Inventory bag)
	{
		if (!lootBagPrefab)
		{
			Debug.LogWarning(this + " cannot CreateLootBag because no prefab set!");
			bag = null;
			return false;
		}

		if (IsEmpty())
		{
			bag = null;
			return false;
		}

		if (!NavUtils.TryGetNearestNavPosition(transform.position, CreateLootNavDistance, out var navPosition))
		{
			Debug.LogWarning(this + " cannot CreateLootBag: no nav position found");
			bag = null;
			return false;
		}
		
		bag = CreateLootBag(lootBagPrefab, navPosition);
		bag.items.AddRange(items);
		bag.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
		items.Clear();
		foreach(var it in bag.items) items.Add(null);
		onAnyInventoryChanged?.Invoke(this);
		return true;
	}
	
	public void NotifyUsed(ItemData item, object objectTo)
	{
		onAnyItemUsed?.Invoke(this, item, objectTo);
	}

	public Vector3 GetPosition()
	{
		if (TryGetComponent(out CharacterEntity character))
			return character.GetPosition();
		if (TryGetComponent(out Collider coll))
			return coll.bounds.center;
		return transform.position;
	}
	
	public int GetMaxSize() => maxSize;

	public void TransfertTo(Inventory otherInventory)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i])
			{
				if (!otherInventory.TryAddItem(items[i]))
				{
					Debug.LogWarning(this + " inventory full " + otherInventory);
					return;
				}
						
				if(debugMode > 0) Debug.Log(this + " onAnyItemTransfered " + this + " " + otherInventory + " " + items[i]);
				onAnyItemTransfered?.Invoke(this, otherInventory, items[i]);
				items[i] = null; // remove from this inventory
			}
		}
	}
	
	public bool TryGetItem(int index, out ItemData item)
	{
		return (item = index >= 0 && index < items.Count ? items[index] : null);
	}

	public bool TryAddItem(ItemData itemData)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (!items[i])
			{
				items[i] = itemData;
				if(debugMode > 0) Debug.Log(this + " onAnyItemAdded " + items[i]);
				onAnyItemAdded?.Invoke(this, items[i]);
				onAnyInventoryChanged?.Invoke(this);
				return true;
			}
		}

		return false;
	}
	
	public bool TryRemoveItem(ItemData itemToUse)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] == itemToUse)
			{
				if(debugMode > 0) Debug.Log(this + " onAnyItemRemoved " + items[i]);
				onAnyItemRemoved?.Invoke(this, items[i]);
				items[i] = null;
				onAnyInventoryChanged?.Invoke(this);
				return true;
			}
		}

		return false;
	}

	public bool IsPartyInventory() => selfCharacter && selfCharacter.InParty();
	
	#region PRIVATE
	private bool destroyIfEmpty;
	private CharacterEntity selfCharacter;
	
	private void Awake()
	{ 
		allInventories.Add(this);
		
		selfCharacter = GetComponent<CharacterEntity>();
	}

	private void OnDestroy()
	{
		allInventories.Remove(this);
	} 
	
	[ContextMenu("Update Size")]
	private void UpdateSize()
	{
		while (items.Count > maxSize)
		{
			items.RemoveAt(items.Count - 1);
		}

		while (items.Count < maxSize)
		{
			items.Add(null);
		}
		
		Utils.SetDirty(this);
	}

	private void LateUpdate()
	{
		if (destroyIfEmpty)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i])
					return;
			}

			if(debugMode > 0) Debug.Log(this + " DestroysIfEmpty");
			gameObject.SetActive(false);
		}
	}
	#endregion
	
	

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		var label = "";
		
		foreach (var item in items)
		{
			if (item)
				label += item.name + "\n";
		}

		label = label.Trim('\n');
		
		if (!string.IsNullOrEmpty(label))
		{
			var bounds = new Bounds(Vector3.zero, transform.position);
			
			if (TryGetComponent(out Collider coll))
			{
				bounds = coll.bounds;
			}
			
			UnityEditor.Handles.Label(bounds.center, label);
		}
	}
#endif
}