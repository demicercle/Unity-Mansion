using RPGCharacterAnims.Lookups;
using UnityEngine;

public enum TargetType
{
	None,
	AnyCharacter,
	GroundPosition,
	OtherItem,
	SelfCharacter
}

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData", order = 0)]
public class ItemData : ScriptableObject
{

	public static ItemData GetItemFromName(string itemName)
	{
		return Resources.Load<ItemData>(Consts.ResPath_Items + itemName);
	}

	public static string Serialize(ItemData item) => !item ? "null" : item.name + ":" + item.GetQuantity();

	public static bool TryDeserialize(string str, out ItemData newItem)
	{
		var split = str.Split(':');
		if (split.Length > 1)
		{
			newItem = GetItemFromName(split[0]);
			if (newItem)
			{
				newItem = newItem.Duplicate();
				
				if (int.TryParse(split[1].Trim('_'), out var qty))
					newItem.SetQuantity(qty);
				
				return true;
			}
		}

		newItem = null;
		return false;
	}

	public ItemData Duplicate()
	{
		var duplicated = Instantiate(this);
		duplicated.name = name;
		if (duplicated.weaponData) duplicated.weaponData = duplicated.weaponData.Duplicate();
		return duplicated;
	}
	
	[Header("General")]
	[SerializeField] private LocalizedText displayName;
	[SerializeField] private LocalizedText description;
	[SerializeField] private Texture2D icon;
	[SerializeField] private string itemTags;

	public string GetDisplayName() => displayName.GetValue(DialogFiles.items);
	
	public Texture2D GetIcon() => icon;
	
	[ContextMenu("Parse")]
	public void Parse()
	{
		displayName.Parse(DialogFiles.items);
		description.Parse(DialogFiles.items);
		Utils.SetDirty(this);
	}

	public string GetTooltipDescription()
	{
		return $"<b><size=22>{GetDisplayName()}</size></b>\n{GetUsabilityDescription()}\n{GetUserDescription()}";
	}

	public string GetUsabilityDescription() => targetType == TargetType.None ? "" :
		"<color=grey>" + LanguageSettings.GetDialogLines(DialogFiles.items).GetLine((int)targetType, Utils.SplitCamelCase(targetType.ToString())) + "</color>";

	public string GetUserDescription() => description.GetValue(DialogFiles.items);

	[Header("Equipable")] 
	[SerializeField] private bool equipOnHand;
	[SerializeField] private WeaponData weaponData;

	public bool IsWeapon(out WeaponData outWeapon)
	{
		outWeapon = weaponData;
		return outWeapon;
	}
	
	[Header("Usable")]
	[SerializeField] private TargetType targetType = TargetType.None;
	[SerializeField] private CharacterAction actionType = CharacterAction.UseItem;
	[SerializeField] private bool usableOnDead = false;
	[SerializeField] private bool turnBasedOnly = false;
	[SerializeField] [Range(0f, 12f)] private float useRange = 0.0f;
	[SerializeField] [Min(0f)] private float useGroundRadius = 0.2f;
	[SerializeField] private bool isProjectile = false;
	[SerializeField] private GameScriptGraph onUseScript = null;
	
	[Header("Consommable")]
	[SerializeField] private bool isAmmo;
	[SerializeField] [Min(1)] private int maxQuantity = 1;

	private int currentQuantity;

	public CharacterAction GetCharacterAction() => actionType;

	public bool TurnBasedOnly() => turnBasedOnly;
	
	public bool IsUsable() => targetType != TargetType.None;

	public bool IsUsableOnSelf() => targetType == TargetType.SelfCharacter;

	public void SetQuantity(int newValue)
	{
		if (weaponData) 
			weaponData.SetCurrentAmmo(newValue);
		else 
			currentQuantity = newValue;
	}

	public int GetQuantity() => weaponData ? weaponData.GetCurrentAmmo() : currentQuantity;
	
	public int GetMaxQuantity() => weaponData ? weaponData.GetMaxAmmo() : maxQuantity;
	
	public bool ShowQuantity() => GetMaxQuantity() > 1;

	public void SetMaxQuantity()
	{
		if(weaponData)
			weaponData.SetCurrentAmmo(weaponData.GetMaxAmmo());
		else 
			currentQuantity = maxQuantity;
	}

	public void AddQuantity(int qty)
	{
		if (weaponData)
			weaponData.AddAmmo(qty);
		else
			currentQuantity = Mathf.Clamp(currentQuantity + qty, 0, maxQuantity);
	}

	public bool IsFull() => currentQuantity >= maxQuantity;

	public bool IsEmpty() => currentQuantity == 0;
	
	public bool EquipOnHand() => equipOnHand;

	public Weapon GetWeaponInHand() => equipOnHand ? Weapon.RightItem : Weapon.Relax;

	public bool RaycastAnyCharacter() => targetType == TargetType.AnyCharacter;
	
	public bool RaycastNavigation() => targetType == TargetType.GroundPosition;

	public bool CanBeUsedOnDead()
	{
		return targetType == TargetType.AnyCharacter && usableOnDead;
	}
	
	public bool IsProjectile() => isProjectile;
	
	public bool CompareTags(string tags)
	{
		foreach (var other in tags.Split(','))
		{
			foreach (var self in itemTags.Split(','))
			{
				if (other == self)
					return true;
			}
		}

		return string.IsNullOrEmpty(tags) && string.IsNullOrEmpty(itemTags);
	}
	
	public float GetUseRange() => useRange;

	public bool CanBeUsedAt(Vector3 position)
	{
		return targetType == TargetType.GroundPosition
			&& NavUtils.TryGetNearestNavPosition(position, useGroundRadius, out var _);
	}
	
	public bool CanBeUsedOn(object target)
	{
		if (target != null)
		{
			if (targetType == TargetType.AnyCharacter)
			{
				return target is CharacterEntity;
			}
		}
		
		return false;
	}

	public void LaunchProjectile(GameObject target, Vector3 destination)
	{
		foreach (var weapon in target.GetComponentsInChildren<WeaponRenderer>())
		{
			if (weapon.rpgWeapon == Weapon.RightItem && CompareTags(weapon.itemTags))
			{
				if (weapon.TryGetComponent(out Projectile projectile))
				{
					var startPosition = projectile.transform.position;
					
					if (target.TryGetComponent(out CharacterEntity characterEntity))
						startPosition = characterEntity.GetCenterPosition();

					if (projectile.Launch(startPosition, destination, out var clone))
					{
						UseOn(clone.gameObject, target.gameObject);
						return;
					}
				}
			}
		}
	}

	public void UseOn(GameObject target, GameObject instigator)
	{
		Debug.Log(this + " UseOn " + target + " by " + instigator);
		var script = target.AddComponent<GameScript>();
		script.SetInstigator(instigator);
		script.SetGraph(onUseScript);
	}
}