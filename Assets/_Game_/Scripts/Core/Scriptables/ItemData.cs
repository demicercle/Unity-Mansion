using RPGCharacterAnims.Lookups;
using UnityEngine;

public enum TargetType
{
	None,
	AnyCharacter,
	GroundPosition
}

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData", order = 0)]
public class ItemData : ScriptableObject
{

	public static ItemData GetItemFromName(string itemName)
	{
		return Resources.Load<ItemData>(Consts.ResPath_Items + itemName);
	}
	
	[SerializeField] private LocalizedText displayName;
	[SerializeField] private Texture2D icon;
	[SerializeField] private TargetType targetType;
	[SerializeField] [Range(0f, 12f)] private float useRange;
	[SerializeField] private GameScriptGraph onUseScript;
	[SerializeField] private string itemTags;
	[SerializeField] [Min(0f)] private float useGroundRadius = 0.2f;
	
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

		return string.IsNullOrEmpty(tags+itemTags);
	}
	
	public float GetUseRange() => useRange;

	public bool CanBeUsedAt(Vector3 position)
	{
		return targetType == TargetType.GroundPosition
			&& NavUtils.TryGetNearestNavPosition(position, useGroundRadius, out var _);
	}
	
	public bool CanBeUsedOn(GameObject target, out object objectTo)
	{
		if (target)
		{
			if (targetType == TargetType.AnyCharacter && target.TryGetComponent<CharacterEntity>(out var character))
			{
				objectTo = character;
				return true;
			}
		}

		objectTo = null;
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
						UseOn(clone.gameObject);
						return;
					}
				}
			}
		}
	}

	public void UseOn(GameObject target)
	{
		Debug.Log(this + " UseOn " + target);
		target.AddComponent<GameScript>().SetGraph(onUseScript);
	}

	public string GetDisplayName() => displayName.GetValue(DialogFiles.items);
	
	public Texture2D GetIcon() => icon;
	
	[ContextMenu("Parse")]
	public void Parse()
	{
		displayName.Parse(DialogFiles.items);
		Utils.SetDirty(this);
	}
}