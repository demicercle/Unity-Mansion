using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Inventory : MonoBehaviour
{
	public const int InventorySlot_Weapon = 6;
	
	private static List<Inventory> allInventories = new List<Inventory>();

	public static Inventory[] GetSavedInventories() => allInventories.Where(i => i.saveInventory && !i.IsPartyInventory()).ToArray();
	
	public static Inventory[] GetLootBags() => allInventories.Where(i => i.isLootBag).ToArray();

	public static Inventory CreateLootBag(Inventory lootBagPrefab, Vector3 navPosition)
	{
		var bag = Instantiate(lootBagPrefab.gameObject).GetComponent<Inventory>();
		bag.isLootBag = true;
		bag.maxSize = 1;
		bag.saveInventory = false;
		bag.destroyIfEmpty = true;
		NavUtils.TryGetNearestNavPosition(navPosition, CreateLootNavDistance, out navPosition);
		bag.transform.position = navPosition;
		lootBagPrefab.gameObject.SetActive(true);
		return bag;
	}
	
	private const float CreateLootNavDistance = 1.5f;
	
	public string GetName() => name;
	public bool IsName(string s) => s == name;
	
	public bool Locked { get; set; }
	
	public static int debugMode { get; set; }
	public static event System.Action<Inventory> onAnyInventoryChanged;
	public static event System.Action<Inventory, ItemData> onAnyItemAdded, onAnyItemRemoved;
	public static event System.Action<Inventory, Inventory, ItemData> onAnyItemTransfered;
	public static event System.Action<Inventory, ItemData, object> onAnyItemUsed;

	public static void NotifyTransfered(Inventory from, Inventory to, ItemData item) =>
		onAnyItemTransfered?.Invoke(from, to, item);
	
	[SerializeField] [Range(1, 32)] private int maxSize = 1;
	[SerializeField] private Inventory lootBagPrefab = null;
	[SerializeField] private bool saveInventory;
	[SerializeField] private bool destroyIfEmpty;
	[SerializeField] private List<ItemData> items = new List<ItemData>();
	
	private bool isLootBag;
	
	public string[] SerializeToSlots()
	{
		return items.ConvertAll(item => ItemData.Serialize(item)).ToArray();
	}

	public void DeserializeFromSlots(string[] slots)
	{
		if(slots == null) return;
		
		ClearInventory(false);
		
		for (int i = 0; i < slots.Length; i++)
		{
			var serializedItem = slots[i];
			if (ItemData.TryDeserialize(serializedItem, out var newItem))
				items[i] = newItem;
		}

		NotifyChanged();
	}

	public void SwapIndex(int index1, int index2, bool notifyChanged)
	{
		var item1 = items[index1];
		var item2 = items[index2];
		items[index1] = item2;
		items[index2] = item1;
		
		if(notifyChanged) NotifyChanged();
	}

	public void ClearInventory(bool notifyChanged)
	{
		for (int i = 0; i < items.Count; i++)
		{
			ClearItem(i, false);
		}
		
		if(notifyChanged) NotifyChanged();
	}

	private void ClearItem(int index, bool notifyChanged)
	{
		if (index < 0 || index >= items.Count)
			return;

		if (!items[index])
			return;
		
		
		if(debugMode > 0) Debug.Log(this + " onAnyItemRemoved " + items[index]);
		onAnyItemRemoved?.Invoke(this, items[index]);

		Destroy(items[index]);
		items[index] = null;
		
		
		if (notifyChanged)
		{
			NotifyChanged();
		}
	}

	public bool TryCreateLootBag(ItemData itemData, out Inventory bag) =>
		TryCreateLootBag(itemData, GetPosition(), out bag);
	
	public bool TryCreateLootBag(ItemData itemData, Vector3 position, out Inventory bag)
	{
		int itemIndex = items.IndexOf(itemData);
		if (itemIndex < 0)
		{
			Debug.LogWarning(this +  " cannot create loot bag: item not in inventory ");
			bag = null;
			return false;
		}
		
		if(!itemData.TryGetLootBagPrefab(out var lootPrefab))
			lootPrefab = lootBagPrefab;
		
		if (!lootPrefab)
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

		bag = CreateLootBag(lootPrefab, navPosition);
		bag.ClearInventory(false);
		if (!bag.TryAddItem(itemData))
		{
			Debug.LogWarning(this + " error while adding item to loot bag: " + bag);
			return false;
		}
		bag.transform.position = position;
		bag.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
		items[itemIndex] = null;
		NotifyChanged();
		return true;
	}

	private bool IsEmpty() => items.All(it => !it);

	public bool DropAllOnGround()
	{
		var bag = default(Inventory);

		var changed = false;
		for (int i = 0; i < items.Count; i++)
		{
			var itemData = items[i];

			if (itemData && TryCreateLootBag(itemData, out bag))
			{
				changed = true;
				ClearItem(i, false);
			}
		}

		if (changed)
		{
			NotifyChanged();
		}

		if (IsEmpty())
		{
			return true;
		}
		
		if (!lootBagPrefab)
		{
			Debug.LogWarning(this + " cannot CreateLootBag because no prefab set!");
			return false;
		}

		if (IsEmpty())
		{
			return false;
		}

		if (!NavUtils.TryGetNearestNavPosition(transform.position, CreateLootNavDistance, out var navPosition))
		{
			Debug.LogWarning(this + " cannot CreateLootBag: no nav position found");
			return false;
		}
		
		bag = CreateLootBag(lootBagPrefab, navPosition);
		bag.items.AddRange(items);
		bag.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
		ClearInventory(false);
		NotifyChanged();
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

	public bool TryTransfertItemTo(ItemData itemToTransfer, Inventory otherInventory)
	{
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] == itemToTransfer)
			{
				if (otherInventory.TryAddItem(itemToTransfer))
				{
					items[i] = null;
					NotifyChanged();
					return true;
				}
				
				break;
			}
		}

		return false;
	}
	
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
				NotifyChanged();
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
				ClearItem(i, false);
				NotifyChanged();
				
				return true;
			}
		}

		return false;
	}

	public bool IsPartyInventory() => selfCharacter && selfCharacter.InParty();
	
	public void NotifyChanged()
	{
		onAnyInventoryChanged?.Invoke(this);
	}
	
	#region PRIVATE
	
	private CharacterEntity selfCharacter;
	
	private void Awake()
	{ 
		allInventories.Add(this);
		
		selfCharacter = GetComponent<CharacterEntity>();

		for (int i = 0; i < items.Count; i++)
		{
			if(items[i])
			{
				items[i] = items[i].Duplicate();
				items[i].SetMaxQuantity();
			}
		}
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
		Locked = selfCharacter && selfCharacter.IsActing();
		
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