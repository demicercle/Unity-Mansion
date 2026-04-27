using RPGCharacterAnims.Lookups;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "WeaponData", order = 0)]
public class WeaponData : ScriptableObject
{
	[Range(1, 12)] public int minDamage = 1;
	[Range(1, 12)] public int maxDamage = 1;
	public bool isRanged = false;
	[Range(0f, 32f)] public float attackRange = 0f;
	public bool noAttack = false;
	public Weapon rpgWeaponR = Weapon.Relax;
	public Weapon rpgWeaponL = Weapon.Relax;
	
	public int RollDamage()
	{
		if (minDamage > maxDamage)
		{
			//Debug.LogWarning(this + " has invalid min max damages " + minDamage + " > " + maxDamage);
			return minDamage;
		}
		
		return Random.Range(minDamage, maxDamage);
	}
}