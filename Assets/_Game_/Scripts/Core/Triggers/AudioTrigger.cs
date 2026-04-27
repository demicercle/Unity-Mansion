using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;


[RequireComponent(typeof(AudioSource))]
public class AudioTrigger : MonoBehaviour
{
	public void SetActive(bool value) => gameObject.SetActive(value);
	public void SetDisabled(bool value) => gameObject.SetActive(!value);
	
	
	public void PlayNext()
	{
		if (!audioSource) return;
		
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
			Debug.Log(Utils.GetTransformPath(transform) + " AudioTrigger Plays " + clipIndex + " = " + audioSource.clip);
#if UNITY_EDITOR
			UnityEditor.EditorGUIUtility.PingObject(gameObject);
#endif
		}
	}

	[SerializeField] private bool debugMode;
	[Space]
	[SerializeField] private AudioClip[] clips;
	[SerializeField] private bool nextIsRandom;
	[Space]
	[SerializeField] private bool playAtStart;
	[SerializeField] private bool playRepeat;
	[SerializeField] private Vector2 repeatInterval;

	private int clipIndex;
	private AudioSource audioSource;

	private void Awake()
	{
		audioSource =  GetComponent<AudioSource>();
	}

	private IEnumerator Start()
	{
		if(playAtStart)
			PlayNext();
		
		while (playRepeat)
		{
			do
			{
				yield return new WaitForSeconds(1f);
			} while (audioSource.isPlaying);
			
			yield return new WaitForSeconds(Random.Range(repeatInterval.x, repeatInterval.y));
			
			PlayNext();
		}
	}
}