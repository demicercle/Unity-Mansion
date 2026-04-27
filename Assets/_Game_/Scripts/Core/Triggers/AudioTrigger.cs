using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


[RequireComponent(typeof(AudioSource))]
public class AudioTrigger : MonoBehaviour
{
	public void PlayNext()
	{
		clipIndex = (clipIndex + 1) % clips.Length;
		audioSource.clip = clips[clipIndex];
		audioSource.Play();
	}
	
	public void PlayRandomClip()
	{
		audioSource.clip = clips[clipIndex = Random.Range(0, clips.Length)];
		audioSource.Play();
	}

	[SerializeField] private AudioClip[] clips;

	public UnityEvent onStart;

	private int clipIndex;
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