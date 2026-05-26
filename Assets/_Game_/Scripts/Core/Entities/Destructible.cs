using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Destructible : MonoBehaviour, IAttackable
{
	public static List<Destructible> allInstances = new List<Destructible>();

	public bool IsDoor() => isDoor || CompareTag(Tags.TagDoor);
	public bool IsName(string s) => name == s;
	public string GetName() => name;
	public static int debugMode => CharacterEntity.debugStats;

	public static event System.Action<Destructible, DamageInfo, object> onAnyDestructibleHit;

	[SerializeField] private Transform pivot;
	[SerializeField] [Range(0.1f, 5.0f)] private float radius = 1.0f;
	[SerializeField] [Range(1.0f, 99.0f)] private float maxHealth = 1.0f;
	[SerializeField] private bool isDoor;
	[SerializeField] private Rigidbody[] rigidbodies;
	[SerializeField] private GameObject[] deactivateObjects;

	public Vector3 GetPosition()
	{
		return pivot ? pivot.position : GetCenterPosition();
	}

	public Vector3 GetCenterPosition() => collider ? collider.bounds.center : transform.position;
	
	public float GetRadius() => collider ? Mathf.Max(collider.bounds.size.x, collider.bounds.size.z) / 2f : radius;
	
	public float GetHeight() => collider ? collider.bounds.size.y : radius;

	public bool ignoreRangedWeapons;
	public UnityEvent takeDamage;
	public UnityEvent breaks;

	public bool IsBroken() => isBroken;
	

	public void AimedBy(CharacterEntity otherCharacter)
	{
		foreach (var outline in GetComponentsInChildren<OutlineRenderer>())
		{
			outline.CursorOver();
		}
	}

	public void TakeDamage(DamageInfo dmgInfo, object instigator)
	{
		if (ignoreRangedWeapons && dmgInfo.isRanged)
		{
			dmgInfo.amount = 0;
		}
		
		damageAmount += dmgInfo.amount;
		if(debugMode > 0) Debug.Log(this + " take damage " + dmgInfo + " total=" + damageAmount + "/" + maxHealth);

		onAnyDestructibleHit?.Invoke(this, dmgInfo, instigator);
		
		takeDamage?.Invoke();
		
		if (damageAmount >= maxHealth)
		{
			if (instigator is Component)
				BreakFrom((instigator as Component).transform.position);
			else
				Break();
		}
	}

	public void Break() => BreakFrom(collider.bounds.center);
	
	public void BreakFrom(Vector3 fromPoint)
	{
		if (isBroken) return;
		isBroken = true;
		if(debugMode > 0)Debug.Log(this + " Breaks");


		foreach (var obj in deactivateObjects)
		{
			if (obj) obj.SetActive(false);
		}

		gameObject.layer = Layers.LayerIgnore;
		
		NavmeshRebuilder.SetNavigationDirty();
		
		foreach (var rb in rigidbodies)
		{
			if (!rb) continue;
			rb.isKinematic = false;
			rb.useGravity = true;
			PhysicsUtils.ApplyForceAtPoint(rb, fromPoint);
			//rb.angularVelocity = new Vector3(0, Mathf.PI, 0);
		}
			
		breaks?.Invoke();
		
	}
	
	private bool isBroken;
	private float damageAmount;
	private new Collider collider;

	[ContextMenu("Collect Rigidbodies")]
	private void CollectRigidbodies()
	{
		rigidbodies = GetComponentsInChildren<Rigidbody>();
		Utils.SetDirty(this);
	}

	private void Awake()
	{
		allInstances.Add(this);
		
		collider = GetComponent<Collider>();
			
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

	private void OnDrawGizmos()
	{
		if (Application.isPlaying && !IsBroken())
		{
			Gizmos.color = Utils.ColorAlpha(Color.lightYellow, .25f);
			Gizmos.DrawWireCube(GetCenterPosition(), new Vector3(GetRadius()*2f, GetHeight(), GetRadius()*2f));
		}
	}
}