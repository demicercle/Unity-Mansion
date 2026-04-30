using System;
using UnityEngine;


[RequireComponent(typeof(Animator))]
public class BlinkAnimatorLayer : MonoBehaviour
{
	[SerializeField] private int blinkLayer = 1;
	[SerializeField] private float lerpSpeed = 3.0f;
	
		private Animator animator;

		private void Awake()
		{
			animator = GetComponent<Animator>();
		}

		private void OnDisable()
		{
			animator.SetLayerWeight(blinkLayer, 0);
		}

		private void LateUpdate()
		{
			animator.SetLayerWeight(blinkLayer,Mathf.Lerp(1f, 0f, Mathf.Abs(Mathf.Sin(Time.unscaledDeltaTime * lerpSpeed))));
		}
}