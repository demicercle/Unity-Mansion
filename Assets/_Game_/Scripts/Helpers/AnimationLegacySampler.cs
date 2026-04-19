using System;
using UnityEngine;

public class AnimationLegacySampler : MonoBehaviour
{
	public new Animation animation;
	[Range(0f,1f)] public float normalizedTime;
	public bool sample;

	private void OnDrawGizmosSelected()
	{
		if (sample) Sample();
	}

	public void Sample() 
	{
		foreach(AnimationState state in animation)
			state.normalizedTime = normalizedTime;
			
		animation.Sample();
	}
}