using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "WeaponData", order = 0)]
public class WeaponData : ScriptableObject
{
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
	[SerializeField] private string ammoTags;
	[SerializeField] [Min(0)] private int maxAmmo;
	
	private int currentAmmo;

	public bool IsFullAmmo() => maxAmmo > 0 && currentAmmo >= maxAmmo;

	public bool IsEmptyAmmo() => maxAmmo > 0 && currentAmmo <= 0;
	
	public bool IsReloadable() => maxAmmo > 0;

	public int GetMaxAmmo() => maxAmmo;
	public int GetCurrentAmmo() => currentAmmo;

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
	
	public string GetAmmoTags() => ammoTags;
	
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
}