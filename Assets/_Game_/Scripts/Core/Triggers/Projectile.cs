using System;
using UnityEngine;
using UnityEngine.Events;


public class Projectile : MonoBehaviour
{
	[ReadOnly] public ItemData itemData;
	[Min(1f)]public float speed = 1f;
	public AnimationCurve lobCurve = AnimationCurve.Linear(0,0,1,0);

	public UnityEvent onHit;
	
	private Vector3 origin, destination;
	private float time;
	private float calculatedSpeed;
	private Vector3 newPosition;

	public void DestroyNow() => Destroy(gameObject);
	
	public void Launch(Vector3 from, Vector3 to)
	{
		origin = from;
		destination = to;
		time = 0;
		calculatedSpeed = speed / Utils.HDistance(from, to);
	}

	private void LateUpdate()
	{
		newPosition = Vector3.Lerp(origin, destination, time)
		              + Vector3.up * lobCurve.Evaluate(time);
		transform.rotation = Quaternion.LookRotation((newPosition - transform.position).normalized);
		transform.position = newPosition;
		time += Time.deltaTime * calculatedSpeed;
		if (time >= 1.0f)
		{
			enabled = false;
			if (itemData)
			{
				itemData.UseOn(gameObject);
			}
			onHit?.Invoke();
		}
	}
}