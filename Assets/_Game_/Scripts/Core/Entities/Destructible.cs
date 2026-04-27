using System;
using UnityEngine;
using UnityEngine.Events;

public class Destructible : MonoBehaviour, IAttackable
{
	[SerializeField] [Range(0.1f, 5.0f)] private float radius = 1.0f;
	[SerializeField] [Range(1.0f, 99.0f)] private float maxHealth = 1.0f;
	[SerializeField] private Rigidbody[] rigidbodies;
	[SerializeField] private GameObject[] deactivateObjects;

	public Vector3 GetPosition() => transform.position;
	public float GetRadius() => radius;

	public UnityEvent takeDamage;
	public UnityEvent breaks;

	public void TakeDamage(float amount, object instigator)
	{
		damageAmount += amount;
		GameCanvas.Instance.FloatingText(this, Mathf.CeilToInt(amount).ToString());
		Debug.Log(this + " take damage " + amount + " total=" + damageAmount + "/" + maxHealth);
		
		takeDamage?.Invoke();
		
		if (damageAmount >= maxHealth)
		{
			Break();
		}
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

	private void Break()
	{
		if (isBroken) return;
		isBroken = true;
		Debug.Log(this + " Breaks");
		
		foreach (var rb in rigidbodies)
		{
			if (!rb) continue;
			rb.isKinematic = false;
			rb.useGravity = true;
		}

		foreach (var obj in deactivateObjects)
		{
			if (obj) obj.SetActive(false);
		}

		gameObject.layer = Layers.LayerIgnore;
		
		NavmeshRebuilder.RebuildNavigationOn(gameObject);
		
		breaks?.Invoke();
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