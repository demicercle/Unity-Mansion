using System;
using UnityEngine;


public class NoiseTrigger : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	public float drawDuration = 1.5f;
	[Space]
	public int rayCount = 16;
	public float rayLength = 6.0f;
	public float radius = 0.1f;
	public bool emitOnEnable = false;
	public bool lookUpParentCharacter = false;
	public CharacterEntity characterInstigator = null;

	private void Awake()
	{
		if(lookUpParentCharacter)
			characterInstigator = GetComponentInParent<CharacterEntity>();
	}

	private void OnEnable()
	{
		if (emitOnEnable)
			Emit();
	}

	[ContextMenu("Emit")]
	public void Emit()
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
		if (!characterInstigator)
			return;
		
		if(debugMode >0) Debug.Log(this + " wallHit: " + wallHit.collider);
		if (wallHit.collider.TryGetComponent(out CharacterEntity other) && other != characterInstigator)
		{
			if (!other.IsDead() && characterInstigator.IsEnemyTo(other) && other.IsCharacterRevealed())
			{
				if (other.TryGetComponent(out CharacterDetection otherDetection))
				{
					otherDetection.SetCharacterDetected(characterInstigator, true);
				}
			}
		}
	}
}