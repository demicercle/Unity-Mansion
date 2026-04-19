using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeScreen : MonoBehaviour
{
	static void Create()
	{
		if (instance) return;
		Instantiate(Resources.Load<GameObject>("FadeScreen"));
	}
	
	static private FadeScreen instance;

	static public FadeScreen Instance
	{
		get
		{
			if (!instance) instance = Utils.FindFirstObject<FadeScreen>();
			if (!instance) Create();
			if (!instance) Debug.LogError("No Fade Screen!");
			return instance;
		}
	}
	
	static public void SetText(string text) => Instance.tmpTxt.text = text;

	[IngameDebugConsole.ConsoleMethod("fade_in","")]
	static public Coroutine FadeIn(bool instant = false) => Instance.FadeCoroutine(true,instant);

	[IngameDebugConsole.ConsoleMethod("fade_out", "")]
	static public Coroutine FadeOut(bool instant = false) => Instance.FadeCoroutine(false,instant);
	
	[Min(0f)][SerializeField] private float duration = 1f;
	[SerializeField] private Color color = Color.black;
	[SerializeField] private Image image;
	[SerializeField] private TMPro.TMP_Text tmpTxt;
	
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
		while (time < duration)
		{
			transform.SetAsLastSibling();
			image.enabled = true;
			image.color = Utils.ColorScale(color, Mathf.Lerp(fromAlpha, toAlpha, time / duration));
			tmpTxt.enabled = true;
			tmpTxt.color = Utils.ColorScale(tmpTxt.color, image.color.a);
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
}