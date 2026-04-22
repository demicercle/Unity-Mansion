using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "WeaponData", order = 0)]
public class WeaponData : ScriptableObject
{
	[Range(1f, 6f)] public float minDamage = 1f;
	[Range(1f, 6f)] public float maxDamage = 1f;
	public bool isRanged = false;
	[Range(0f, 10f)] public float attackRange = 0f;
	public bool noAttack = false;
	
	public float RollDamage()
	{
		if (minDamage > maxDamage)
		{
			//Debug.LogWarning(this + " has invalid min max damages " + minDamage + " > " + maxDamage);
			return minDamage;
		}
		
		return Random.Range(minDamage, maxDamage);
	}
}