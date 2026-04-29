using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Destructible : MonoBehaviour, IAttackable
{
	public static List<Destructible> allInstances = new List<Destructible>();
	
	public bool IsName(string s) => name == s;
	public string GetName() => name;
	public static int debugMode => CharacterEntity.debugStats;

	public static event System.Action<Destructible, DamageInfo, object> onAnyDestructibleHit;
	
	[SerializeField] [Range(0.1f, 5.0f)] private float radius = 1.0f;
	[SerializeField] [Range(1.0f, 99.0f)] private float maxHealth = 1.0f;
	[SerializeField] private Rigidbody[] rigidbodies;
	[SerializeField] private GameObject[] deactivateObjects;

	public Vector3 GetPosition() => transform.position;
	public float GetRadius() => radius;
	public float GetHeight() => radius;

	public bool damagedWithRangeWeapons;
	public UnityEvent takeDamage;
	public UnityEvent breaks;

	public bool IsBroken() => isBroken;

	public void TakeDamage(DamageInfo dmgInfo, object instigator)
	{
		if (!damagedWithRangeWeapons && dmgInfo.isRanged)
		{
			dmgInfo.amount = 0;
		}
		
		damageAmount += dmgInfo.amount;
		if(debugMode > 0) Debug.Log(this + " take damage " + dmgInfo + " total=" + damageAmount + "/" + maxHealth);

		onAnyDestructibleHit?.Invoke(this, dmgInfo, instigator);
		takeDamage?.Invoke();
		
		if (damageAmount >= maxHealth)
		{
			Break();
		}
	}

	public void Break()
	{
		if (isBroken) return;
		isBroken = true;
		if(debugMode > 0)Debug.Log(this + " Breaks");


		foreach (var obj in deactivateObjects)
		{
			if (obj) obj.SetActive(false);
		}

		gameObject.layer = Layers.LayerIgnore;
		
		GameTurnManager.Instance.SetNavigationDirty();
		
		foreach (var rb in rigidbodies)
		{
			if (!rb) continue;
			rb.isKinematic = false;
			rb.useGravity = true;
		}
			
		breaks?.Invoke();
		
	}
	
	private bool isBroken;
	private float damageAmount;

	[ContextMenu("Collect Rigidbodies")]
	private void CollectRigidbodies()
	{
		rigidbodies = GetComponentsInChildren<Rigidbody>();
		Utils.SetDirty(this);
	}

	private void Awake()
	{
		allInstances.Add(this);
			
		foreach (var obj in deactivateObjects)
		{
			if (obj) obj.SetActive(true);
		}
		
		foreach (var rb in rigidbodies)
		{
			if (!rb) continue;
			rb.isKinematic = true;
		}
	}

	private void OnDestroy()
	{
		allInstances.Remove(this);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Utils.DrawGizmosCircle(GetPosition(), transform.up, radius);
		foreach (var rb in rigidbodies)
		{
			if (!rb) continue;
			var coll = rb.GetComponent<Collider>();
			Gizmos.DrawWireCube(coll.bounds.center, coll.bounds.size);
		}
	}
}