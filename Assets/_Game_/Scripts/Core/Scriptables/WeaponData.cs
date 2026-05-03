using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "WeaponData", order = 0)]
public class WeaponData : ScriptableObject
{
	[Range(1, 12)] [SerializeField] private int minDamage = 1;
	[Range(1, 12)] [SerializeField] private int maxDamage = 1;
	[Range(1, 20)] [SerializeField] private int hitChance = 10;
	public float GetHitChance() => DicePool.DiceToFloat(20, hitChance);
	
	[SerializeField] private bool isRanged = false;
	public bool IsRanged() => isRanged;
	[Range(0f, 32f)] [SerializeField] private float attackRange = 0f;
	public float GetAttackRange() => attackRange;
	[SerializeField] private AnimationCurve rangeHitCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
	public float EvaluateRange(float range, float radius) => range <= radius ? 1.0f : Mathf.Clamp01(rangeHitCurve.Evaluate(range / attackRange));
	[SerializeField] private AnimationCurve heightHitBonus = AnimationCurve.Linear(-1f, -0.25f, 1f, 0.25f);
	public float EvaluateHeight(float height) => heightHitBonus.Evaluate(height);
	[SerializeField] private bool noAttack = false;

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
	
	public int RollDamage(float result)
	{
		return (int)Mathf.Lerp(minDamage, maxDamage, result);
	}
}