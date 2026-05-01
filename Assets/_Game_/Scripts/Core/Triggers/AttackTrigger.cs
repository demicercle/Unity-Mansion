using System;
using System.Linq;
using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
	public static int debugMode => CharacterTrigger.debugMode;
	
	[SerializeField] private CharacterEntity selfCharacter;
	[SerializeField] private float triggerRangeOffset = 0.1f;
	[SerializeField] private bool triggerOnExit = true;
	[SerializeField] private bool triggerOnEnter = false;
	[SerializeField] private bool movingCharacterOnly = true;
	[SerializeField] private bool enemyCharacterOnly = true;
	[SerializeField] private bool meleeWeaponOnly = true;
	[SerializeField] private bool isInstantAttack = true;
	[SerializeField] private bool stopAgent = true;
	[SerializeField] private bool useAttackAction = false;
	[SerializeField] private bool requiresWeaponReady = true;
	[SerializeField] [Min(-1)] private int maxTriggerCount = -1;
	[SerializeField] private string filters = default;
	
	private SphereCollider sphereCollider;
	private int triggerCount;
	
	private void Awake()
	{
		sphereCollider = GetComponent<SphereCollider>();
		if (!sphereCollider) sphereCollider = gameObject.AddComponent<SphereCollider>();
		
		if (!selfCharacter) selfCharacter = GetComponentInParent<CharacterEntity>();
		if (!selfCharacter)
		{
			Debug.LogWarning(this + " has no character set, disabling");
			enabled = false;
			return;
		}

		sphereCollider.radius = selfCharacter.GetAttackRange() + triggerRangeOffset;
		sphereCollider.isTrigger = true;
	}

	private void OnEnable()
	{
		sphereCollider.enabled = true;
		selfCharacter.onStatsChanged += OnCharacterStatsChanged;
		if(debugMode > 0) Debug.Log(this + " OnEnable");
	}

	private void OnDisable()
	{
		sphereCollider.enabled = false;

		if (!selfCharacter) return;
		selfCharacter.onStatsChanged -= OnCharacterStatsChanged;
		if(debugMode > 0) Debug.Log(this + " OnDisable");
	}

	private void OnTriggerEnter(Collider other)
	{
		if (triggerOnEnter) OnTrigger(other);
	}

	private void OnTriggerExit(Collider other)
	{
		if (triggerOnExit) OnTrigger(other);
	}

	private void OnTrigger(Collider other)
	{
		if (selfCharacter.IsDead()) return;
		if (maxTriggerCount >= 0 && triggerCount >= maxTriggerCount) return;
		if (meleeWeaponOnly && selfCharacter.IsUsingRangeWeapon()) return;
		
		if (other.TryGetComponent(out CharacterEntity otherCharacter))
		{
			if (!otherCharacter.IsDead() && TagCheck(filters, otherCharacter))
			{
				if (movingCharacterOnly && !otherCharacter.IsMoving()) 
					return;
				
				if (enemyCharacterOnly && !selfCharacter.IsEnemyTo(otherCharacter))
					return;
				
				if (requiresWeaponReady && !selfCharacter.WeaponsReady())
				{
					if(debugMode > 0) Debug.Log(this + " weapons not ready");
					return;
				}

				if (selfCharacter.IsUsingRangeWeapon())
				{
					if (!selfCharacter.LOSCheck(otherCharacter))
					{
						if(debugMode > 0) Debug.Log(this + " !LOSCheck on " + otherCharacter);
						return;
					}
				}
				
				// trigger opportunity attack
				
				if (stopAgent) 
					otherCharacter.StopAgent();

				if (debugMode > 0) Debug.Log(this + " triggers instant:" + isInstantAttack + " on " + otherCharacter);
				
				if (isInstantAttack) 
					selfCharacter.Routines.AssignPerformAttack(otherCharacter, useAttackAction);
				else 
					selfCharacter.Routines.AssignMoveAndAttack(otherCharacter, otherCharacter.IsUsingRangeWeapon());
				
				triggerCount++;
				
				Debug.Log(this + " in " + selfCharacter + " triggers opportunity attack on " + otherCharacter);
			}
		}
	}

	private void OnCharacterStatsChanged()
	{
		sphereCollider.radius = selfCharacter.GetRadius() + triggerRangeOffset;
	}
	
	private bool TagCheck(string filters, object obj)
	{
		return obj == null || string.IsNullOrWhiteSpace(filters) ||
		       filters.Split(',').ToList().Any(tag =>
		       {
			       if (obj is Component)
				       return (obj as Component).CompareTag(tag);

			       if (obj is GameObject)
				       return (obj as GameObject).CompareTag(tag);

			       return false;
		       });
	}
	
	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (selfCharacter)
		{
			Gizmos.color = Color.red;
			Utils.DrawGizmosCircle(selfCharacter.GetPosition(), Vector3.up, selfCharacter.GetAttackRange() + triggerRangeOffset);
			//Gizmos.DrawWireSphere(selfCharacter.GetPosition(), selfCharacter.GetRadius() + triggerRangeOffset);
		}
	}
#endif
}