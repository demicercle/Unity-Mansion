using System;
using UnityEngine;

public class AttackTrigger : MonoBehaviour
{
	[SerializeField] private CharacterController selfCharacter;
	[SerializeField] [Range(0.1f, 1.0f)] private float triggerRangeOffset = 0.3f;
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
		
		if (!selfCharacter) selfCharacter = GetComponentInParent<CharacterController>();
		if (!selfCharacter)
		{
			Debug.LogWarning(this + " has no character set, disabling");
			enabled = false;
			return;
		}
		
		sphereCollider.isTrigger = true;
	}

	private void OnEnable()
	{
		sphereCollider.enabled = true;
		selfCharacter.onStatsChanged += OnCharacterStatsChanged;
	}

	private void OnDisable()
	{
		sphereCollider.enabled = false;

		if (!selfCharacter) return;
		selfCharacter.onStatsChanged -= OnCharacterStatsChanged;
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
		if (maxTriggerCount >= 0 && triggerCount >= maxTriggerCount) return;
		if (meleeWeaponOnly && selfCharacter.IsUsingRangeWeapon()) return;
		
		if (other.TryGetComponent(out CharacterController otherCharacter))
		{
			if (!otherCharacter.IsDead() && GameUtils.TagCheck(filters, otherCharacter))
			{
				if (movingCharacterOnly && !otherCharacter.IsMoving()) 
					return;
				
				if (enemyCharacterOnly && !selfCharacter.IsEnemyTo(otherCharacter))
					return;
				
				if (requiresWeaponReady && !selfCharacter.WeaponsReady())
				{
					Debug.Log(this + " weapons not ready");
					return;
				}
				
				// trigger opportunity attack
				
				if (stopAgent) 
					otherCharacter.StopAgent();
				
				if (isInstantAttack) 
					selfCharacter.Routines.AssignAttackInstant(otherCharacter, useAttackAction);
				else 
					selfCharacter.Routines.AssignAttack(otherCharacter, false, false);
				
				triggerCount++;
				
				Debug.Log(this + " in " + selfCharacter + " triggers opportunity attack on " + otherCharacter);
			}
		}
	}

	private void OnCharacterStatsChanged()
	{
		sphereCollider.radius = selfCharacter.GetRadius() + triggerRangeOffset;
	}
	
	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		sphereCollider = Utils.GetOrAddComponent<SphereCollider>(gameObject);
		if (!Mathf.Approximately(sphereCollider.radius, triggerRangeOffset))
		{
			sphereCollider.isTrigger = true;
			gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_TRIGGER);
			OnCharacterStatsChanged();
			Utils.SetDirty(this);
		}
	}
#endif
}