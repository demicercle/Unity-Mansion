using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;


[RequireComponent(typeof(Slider))]
public class AudioMixerControl : MonoBehaviour
{
	public string parameterName = "Volume";
	public AudioMixer audioMixer;
	public AnimationCurve valueCurve;

	private Slider slider;

	private void Awake()
	{
		slider = GetComponent<Slider>();
		slider.onValueChanged.AddListener(OnValueChanged);
		slider.value = PlayerPrefs.GetFloat(audioMixer.name + "." + parameterName, 0);
	}

	private void OnDestroy()
	{
		slider.onValueChanged.RemoveListener(OnValueChanged);
	}

	private void OnValueChanged(float value)
	{
		PlayerPrefs.SetFloat(audioMixer.name + "." + parameterName, value);
		audioMixer.SetFloat(parameterName, valueCurve.Evaluate(value));
	}
}