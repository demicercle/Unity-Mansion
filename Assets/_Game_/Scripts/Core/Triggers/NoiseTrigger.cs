using System;
using UnityEngine;


public class NoiseTrigger : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	[SerializeField] private float drawDuration;
	[Space]
	[SerializeField] private int rayCount;
	[SerializeField] private float rayLength;
	[SerializeField] private float radius;
	[SerializeField] private bool emitOnEnable;
	[SerializeField] private bool lookUpParentCharacter;
	[SerializeField] private CharacterEntity selfCharacter;

	private void Awake()
	{
		if(lookUpParentCharacter)
			selfCharacter = GetComponentInParent<CharacterEntity>();
	}

	private void OnEnable()
	{
		if (emitOnEnable)
			Emit();
	}

	[ContextMenu("Emit")]
	private void Emit()
	{
		if (rayCount <= 0)
			return;
		
		for (int i = 0; i < rayCount; i++)
		{
			var origin = transform.position;
			var angle = i * 360f / (float)rayCount;
			var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
			var length = rayLength;
			while(length > 0f)
			{
				if (Physics.SphereCast(new Ray(origin, dir), radius, out var wallHit, length, Layers.MaskNoise))
				{
					if (debugMode > 0) DebugDrawer.Line(origin, wallHit.point, Color.red, drawDuration);
					length -= wallHit.distance;
					origin = wallHit.point;
					dir = Vector3.Reflect(dir, wallHit.normal);
					OnHit(wallHit);
				}
				else
				{
					if (debugMode > 0) DebugDrawer.Line(origin, origin + dir * length, Color.red, drawDuration);
					length -= length;
				}
				//Debug.Log(i + " " + length);
			}
		}
	}

	private void OnHit(RaycastHit wallHit)
	{
		if (!selfCharacter)
			return;
		
		if(debugMode >0) Debug.Log(this + " wallHit: " + wallHit.collider);
		if (wallHit.collider.TryGetComponent(out CharacterEntity other) && other != selfCharacter)
		{
			if (!other.IsDead() && selfCharacter.IsEnemyTo(other) && other.IsCharacterRevealed())
			{
				if (other.TryGetComponent(out CharacterDetection otherDetection))
				{
					otherDetection.SetCharacterDetected(selfCharacter, true);
				}
			}
		}
	}
}