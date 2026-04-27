using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


[RequireComponent(typeof(AudioSource))]
public class AudioTrigger : MonoBehaviour
{
	
	public void PlayNext()
	{
		if (nextIsRandom)
		{
			if (clips.Length > 1)
			{
				var oldIndex = clipIndex;
				do
				{
					clipIndex = Random.Range(0, clips.Length);
				} while (clipIndex == oldIndex);
			}
		}
		else
		{
			clipIndex = (clipIndex + 1) % clips.Length;
		}
		
		audioSource.clip = clips[clipIndex];
		if(audioSource.clip)
			audioSource.Play();

		if (debugMode)
		{
			Debug.Log(this + " Plays " + clipIndex + " = " + audioSource.clip);
#if UNITY_EDITOR
			UnityEditor.EditorGUIUtility.PingObject(gameObject);
#endif
		}
	}

	[SerializeField] private bool debugMode;
	[Space]
	[SerializeField] private AudioClip[] clips;
	[SerializeField] private bool randomAtStart;
	[SerializeField] private bool nextIsRandom;
	
	public UnityEvent onStart;

	private int clipIndex;
	private AudioSource audioSource;

	private void Awake()
	{
		audioSource =  GetComponent<AudioSource>();
	}

	private void Start()
	{
		if(randomAtStart)
			clipIndex = Random.Range(0, clips.Length);
		
		onStart?.Invoke();
	}
}