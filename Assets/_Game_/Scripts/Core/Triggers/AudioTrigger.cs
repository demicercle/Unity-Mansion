using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


[RequireComponent(typeof(AudioSource))]
public class AudioTrigger : MonoBehaviour
{
	public void PlayRandomClip()
	{
		audioSource.clip = clips[Random.Range(0, clips.Length)];
		audioSource.Play();
	}

	[SerializeField] private AudioClip[] clips;

	public UnityEvent onStart;
	
	private AudioSource audioSource;

	private void Awake()
	{
		audioSource =  GetComponent<AudioSource>();
	}

	private void Start()
	{
		onStart?.Invoke();
	}
}