using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO [RequireComponent(typeof(Animator))]
public class FadeScreen : MonoBehaviour
{
	public static float FadeDuration = 1f;
	
	[IngameDebugConsole.ConsoleMethod("fade_in","")]
	public static Coroutine FadeIn(bool instant = false) => GetInstance().FadeCoroutine(true,instant);

	[IngameDebugConsole.ConsoleMethod("fade_out", "")]
	public static Coroutine FadeOut(bool instant = false) => GetInstance().FadeCoroutine(false,instant);
	public static void SetLoadingProgress(float progress)
	{
		loadingProgress = progress;
	}
	public static void SetText(string text) => GetInstance().tmpTxt.text = text; //TODO: not in use
	public static bool IsFadingOrVisible => GetInstance().image.enabled; //TODO: not in use
	
	#region PRIVATE
	private static FadeScreen instance;
	private static float loadingProgress;

	private static FadeScreen GetInstance()
	{
		if (!instance) instance = Utils.FindFirstObject<FadeScreen>();
		if (!instance) Debug.LogError("No Fade Screen!");
		return instance;
	}
	
	[SerializeField] private Color color = Color.black;
	[SerializeField] private Image image;
	[SerializeField] private TMPro.TMP_Text tmpTxt;
	[SerializeField] private Slider progressSlider;
	
	private float time;
	private float fromAlpha, toAlpha;

	private Coroutine FadeCoroutine(bool fadeIn, bool instant)
	{
		if (instant)
		{
			transform.SetAsLastSibling();
			image.enabled = fadeIn;
			image.color = Utils.ColorScale(color, fadeIn ? 1f : 0f);
			return null;
		}
		
		StopAllCoroutines();
		return StartCoroutine(DoFade(fadeIn));
	}

	private IEnumerator DoFade(bool fadeIn)
	{
		toAlpha = fadeIn ? 1f : 0f;
		fromAlpha = 1f - toAlpha;
		time = 0f;
		while (time < FadeDuration)
		{
			transform.SetAsLastSibling();
			image.enabled = true;
			image.color = Utils.ColorScale(color, Mathf.Lerp(fromAlpha, toAlpha, time / FadeDuration));
			tmpTxt.enabled = true;
			tmpTxt.color = Utils.ColorScale(tmpTxt.color, image.color.a);
			progressSlider.value = loadingProgress;
			time += Time.unscaledDeltaTime;
			yield return null;
		}
		image.enabled = fadeIn;
		tmpTxt.enabled = fadeIn;
	}

	private void Awake()
	{
		instance = this;
	}
	#endregion
}