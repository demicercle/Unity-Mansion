using System;
using System.Collections.Generic;
using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "WeaponData", menuName = "WeaponData", order = 0)]
public class WeaponData : ScriptableObject
{
	public static int debugMode => CharacterEntity.debugAim;
	
	[System.Serializable]
	public struct ShootInfo
	{
		public float angle;
		public float delay;
		public Vector2 atkMod;
		public bool raycastAll;
		public bool clampDistance;
		
		public float RollAtkMod() => Random.Range(atkMod.x, atkMod.y);

		public RaycastHit[] Raycast(Vector3 from, Vector3 dir, float distance)
		{
			dir = Quaternion.Euler(0, angle, 0) * dir;
			
			if (debugMode > 0)
			{
				DebugDrawer.Line(from, from + dir * distance, Color.magenta, 1.0f);
			}
			
			if (raycastAll)
			{
				return Physics.RaycastAll(from, dir, distance, Layers.MaskAiming);
			}
			else
			{
				return Physics.Raycast(from, dir, out var hit, distance, Layers.MaskAiming)
					? new RaycastHit[]{hit}
					: Array.Empty<RaycastHit>();
			}
		}
	}
	
	public WeaponData Duplicate()
	{
		var duplicated = Instantiate(this);
		duplicated.name = name;
		return duplicated;
	}

	public static event System.Action<WeaponData> onWeaponChanged; 
	
	[Header("General")]
	[SerializeField] private bool noAttack = false;
	[Range(1, 20)] [SerializeField] private int hitChance = 10;
	public float GetHitChance() => DicePool.DiceToFloat(20, hitChance);
	
	[Header("Damage")]
	[Range(1, 12)] [SerializeField] private int minDamage = 1;
	[Range(1, 12)] [SerializeField] private int maxDamage = 1;
	[SerializeField] private AnimationCurve damageCurve = AnimationCurve.Linear(0, 0, 1, 1);
	
	public int RollDamage(float result)
	{
		return (int)Mathf.Lerp(minDamage, maxDamage, damageCurve.Evaluate(result));
	}
	
	[Header("Range")]
	[SerializeField] private bool isRanged = false;
	public bool IsRanged() => isRanged;
	[Range(0f, 32f)] [SerializeField] private float attackRange = 0f;
	public float GetAttackRange() => attackRange;
	[SerializeField] private AnimationCurve rangeHitCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
	public float EvaluateRange(float range, float radius) => range <= radius ? 1.0f : Mathf.Clamp01(rangeHitCurve.Evaluate(range / attackRange));
	[SerializeField] private AnimationCurve heightHitBonus = AnimationCurve.Linear(-1f, -0.25f, 1f, 0.25f);
	public float EvaluateHeight(float height) => heightHitBonus.Evaluate(height);

	[Header("Ammunition")] 
	[SerializeField] [Min(0)] private int consumeAmmo = 0;
	[SerializeField] private string ammoTags;
	[SerializeField] [Min(0)] private int maxAmmo;
	[SerializeField] private ShootInfo[] shootInfos = Array.Empty<ShootInfo>();

	public bool GetShootInfos(out ShootInfo[] shoots)
	{
		shoots = shootInfos;
		return shoots.Length > 0;
	}
	
	private int currentAmmo;

	public bool IsFullAmmo() => maxAmmo > 0 && currentAmmo >= maxAmmo;

	public bool IsEmptyAmmo() => maxAmmo > 0 && currentAmmo <= 0;
	
	public bool IsReloadable() => maxAmmo > 0;

	public int GetMaxAmmo() => maxAmmo;
	public int GetCurrentAmmo() => currentAmmo;
	
	public void ConsumeAmmo() => AddAmmo(-consumeAmmo);

	public void AddAmmo(int qty)
	{
		currentAmmo = Mathf.Clamp(currentAmmo + qty, 0, maxAmmo);
		onWeaponChanged?.Invoke(this);
	}

	public void SetCurrentAmmo(int current)
	{
		currentAmmo = Mathf.Clamp(current, 0, maxAmmo);
		onWeaponChanged?.Invoke(this);
	}
	
	[Header("Other")]
	[SerializeField] private DecalData[] decals;

	public bool TrySpawnDecal(RaycastHit lastShootHitResult)
	{
		if (!lastShootHitResult.collider)
			return false;
		
		var shootRenderers = lastShootHitResult.collider.GetComponents<Renderer>()
			.Concat(lastShootHitResult.collider.GetComponentsInChildren<Renderer>());

		var instances = 0;
		foreach (var shootRenderer in shootRenderers)
		{
			if (shootRenderer.bounds.Contains(lastShootHitResult.point) && TryGetDecalPrefab(shootRenderer.sharedMaterial, out var decal))
			{
				Instantiate(decal, lastShootHitResult.point, Quaternion.LookRotation(lastShootHitResult.normal));
				instances++;
			}
		}

		return instances > 0;
	}

	private bool TryGetDecalPrefab(Material hitMaterial, out GameObject decal)
	{
		if (hitMaterial && DecalData.TryGetDecal(hitMaterial, decals, out var decalData))
		{
			decal = decalData.decalPrefab;
			return true;
		}

		decal = null;
		return false;
	}
	
	public Weapon rpgWeaponR = Weapon.Relax;
	public Weapon rpgWeaponL = Weapon.Relax;
	public bool noAim;

	public bool CanBeReloadedWithAmmo(ItemData draggedItem)
	{
		return draggedItem.GetCharacterAction() == CharacterAction.Reload
		       && draggedItem.CompareTags(ammoTags)
		       && draggedItem.GetQuantity() >= consumeAmmo;
	}
}