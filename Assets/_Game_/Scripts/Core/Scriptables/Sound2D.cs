using UnityEngine;
using UnityEngine.Audio;


[CreateAssetMenu(fileName = "Sound2D", menuName = "Sound2D", order = 0)]
public class Sound2D : ScriptableObject
{
	public AudioMixerGroup mixerGroup;
	public AudioClip[] clips;
	
	
	public void PlayNext()
	{
		currentIndex = (currentIndex + 1) % clips.Length;
		var clip = clips[currentIndex];
		var source = new GameObject(clip.name).AddComponent<AudioSource>();
		source.transform.SetParent(Camera.main.transform, false);
		source.clip = clip;
		source.outputAudioMixerGroup = mixerGroup;
		source.Play();
		source.gameObject.AddComponent<AutoDestroy>().delay = source.clip.length + 1.5f;
		DontDestroyOnLoad(source.gameObject);
	}

	private int currentIndex;
}