using System;
using UnityEngine;
using Object = UnityEngine.Object;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleHelper : MonoBehaviour
{
	private ParticleSystem ps;

	[ContextMenu("Set Looping Recursive")]
	private void SetLoopingRecursive()
	{
		if(!ps) ps = GetComponent<ParticleSystem>();
		ParticleSystem[] others = GetComponentsInChildren<ParticleSystem>();
		Utils.UndoRecord("SetLoopingRecursive", (Object[])others);
		foreach (var other in others)
		{
			other.loop = ps.loop;
			Utils.SetDirty(other);
		}
	}

	public void PlayWithSpeed(float newSpeed)
	{
		ps.playbackSpeed = newSpeed;
	}

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
	}
}