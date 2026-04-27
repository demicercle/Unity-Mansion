using UnityEngine;
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
	
	private AudioSource audioSource;

	private void Awake()
	{
		audioSource =  GetComponent<AudioSource>();
	}
}