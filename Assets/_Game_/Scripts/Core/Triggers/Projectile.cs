using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
	[ReadOnly] public ItemData itemData;
	[Min(1f)]public float speed = 1f;

	public UnityEvent onHit;
	
	private Vector3 origin, destination;
	private float time;
	private float calculatedSpeed;
	private Vector3 newPosition;
	private Rigidbody rb;
	private Projectile clone;
	private bool destroysOnHit;

	public void DestroyNow() => Destroy(gameObject);

	public void StopSimulate()
	{
		origin = destination = default;
		
		if(clone)
			Destroy(clone.gameObject);
	}

	public void Simulate(Vector3 from, Vector3 to, float height)
	{
		if (!clone || Vector3.Distance(from, to) > 0.5f)
		{
			origin = from;
			destination = to;
			StopSimulate();
		
			clone = Instantiate(gameObject).GetComponent<Projectile>();
			clone.transform.SetParent(null, true);
			clone.rb.includeLayers = Layers.MaskProjectile;
			clone.rb.excludeLayers = ~Layers.MaskProjectile;
			rb.detectCollisions = true;
			clone.destroysOnHit = true;
			clone.rb.isKinematic = false;
			clone.rb.linearVelocity = (to - from).normalized * clone.speed + Vector3.up * height;
			Debug.Log(this + " Simulates " + rb.linearVelocity + "\n" + height + " " + from + " " + to );
		}
	}

	private void OnCollisionEnter(Collision other)
	{
		if (destroysOnHit)
		{
			Debug.Log(this + " destroysOnHit " + other.collider);
			DestroyNow();
		}
	}

	public void Launch(Vector3 from, Vector3 to)
	{
		origin = from;
		destination = to;
		time = 0;
		calculatedSpeed = speed / Utils.HDistance(from, to);
	}

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		rb.isKinematic = true;
		rb.detectCollisions = false;
	}
}