using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public interface IDebugGUI
{
	void OnDebugGUI();
}

public class DebugGUI : MonoBehaviour
{
	static public bool IsEnabled
	{
		get => UserSettings.GetCurrentSettings().showDebugGUI;
		set
		{
			UserSettings.GetCurrentSettings().showDebugGUI = value;
			GameView.Focus(!value);
		}
	}

	public static float FADE_DURATION = 3.0f;
	public static float FADE_PROGRESS = 0.0f;

	static public void Label(string label) => GUILayout.Label(GetLabel(label));
	static public void FlexibleSpace() => GUILayout.FlexibleSpace();
	static public void Space(float s = 5f) => GUILayout.Space(s);
	static public void BeginGroup(string label) => GUILayout.BeginVertical(new GUIContent(GetLabel(label)), new GUIStyle("window"));
	static public void EndGroup() => GUILayout.EndVertical();
	static public float Field(float value, string label) => RapidGUI.RGUI.Field(value, GetLabel(label));
	static public int Field(int value, string label) => RapidGUI.RGUI.Field(value, GetLabel(label));
	static public Vector2 Field(Vector2 value, string label) => RapidGUI.RGUI.Field(value, GetLabel(label));
	static public bool Toggle(bool value, string label) => GUILayout.Toggle(value, GetLabel(label));
	static public int Range(int value, int min, int max, string label) => RapidGUI.RGUI.Slider(value, min, max, GetLabel(label));
	static public float Range(float value, float min, float max, string label) => RapidGUI.RGUI.Slider(value, min, max, GetLabel(label));
	static public bool ToolButton( string label) => GUILayout.Button(GetLabel(label), GUILayout.ExpandWidth(false));

	private static float fadeAlpha;
	private static float fadeTime;
	static public IEnumerator FadeIn(float d = -1)
	{
		fadeTime = d < 0 ? FADE_DURATION : d;
		fadeAlpha = 1.0f;
		yield return new WaitForSeconds(FADE_DURATION);
	}
	
	static public IEnumerator FadeOut(float d = -1)
	{
		fadeTime = d < 0 ? FADE_DURATION : d;
		fadeAlpha = 0.0f;
		yield return new WaitForSeconds(FADE_DURATION);
	}
	
	static string GetLabel(string label) => Utils.SplitCamelCase(label);
	/*{
		#if UNITY_EDITOR
		return UnityEditor.ObjectNames.NicifyVariableName(label);
		#else
		return label;
		#endif
	}*/

	public KeyCode toggleKey = KeyCode.F3;
	[Space]
	
	[Header("Appearance")]
	[Range(0f, 1f)] public float viewX = 0;
	[Range(0f, 1f)] public float viewY = 0;
	[Range(0f, 1f)] public float viewWidth = 0.3f;
	[Range(0f, 1f)] public float viewHeight = 0.5f;
	public GUISkin guiSkin;
	
	[Header("Debug Objects")]
	public List<Object> debugObjects = new List<Object>();

	[Space] [Space] 
	[ReadOnly] [SerializeField] private Vector2 scrollValue;
	[ReadOnly] [SerializeField] private Rect screenRect;
	[ReadOnly] [SerializeField] private Vector2 screenSize;

	private GUIStyle fadeStyle;
	private Texture2D blackPixel;

	private void Awake()
	{
		if(!Application.isEditor && !Debug.isDebugBuild)
			Destroy(this);

		else
		{
			fadeStyle = new GUIStyle();
			blackPixel = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			blackPixel.SetPixel(0, 0, Color.black);
		}
	}

	void OnDestroy() => Destroy(blackPixel); // \o/

	private void Update()
	{
		if (!Debug.isDebugBuild) return;
		
		if (fadeTime > 0 || fadeAlpha > 0)
		{
			IsEnabled = true;
			blackPixel.SetPixel(0, 0, Utils.ColorAlpha(Color.black, Mathf.Abs(fadeAlpha - fadeTime / FADE_DURATION))); 
			fadeStyle.normal.background = blackPixel;
			fadeStyle.normal.textColor = Color.white;
			return;
		}
		
		if (DebugLogManager.Instance.IsLogWindowVisible) IsEnabled = false;
		if (!IsEnabled) return;
		
		screenSize.x = Screen.width;
		screenSize.y = Screen.height;
		
		screenRect.x = viewX * screenSize.x;
		screenRect.y = viewY * screenSize.y;
		screenRect.width = viewWidth * screenSize.y;
		screenRect.height = viewHeight * screenSize.y;
	}

	private void OnGUI()
	{
		if (!Debug.isDebugBuild) return;
		
		if (fadeTime > 0 || fadeAlpha > 0)
		{
			GUI.Label(new Rect(0,0,Screen.width, Screen.height), 
				new GUIContent("Loading..." + (FADE_PROGRESS > 0 ? " (" + (int)(FADE_PROGRESS * 100) + "%)" : "")),
				fadeStyle);
			return;
		}
			
		if (Event.current.type == EventType.KeyUp && Event.current.keyCode == toggleKey) // toggle debug GUI with F3
		{
			IsEnabled = !IsEnabled;
		}
		
		if (!IsEnabled) return;
		GUI.skin = guiSkin;
		
		GUILayout.BeginArea(screenRect, GUIContent.none, GUIStyle.none);
		//GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); if(GUILayout.Button("CLOSE", GUILayout.ExpandWidth(false))) IsEnabled = false; GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal(new GUIStyle("box"), GUILayout.ExpandWidth(true)); DrawToolbar(); GUILayout.EndHorizontal();
		scrollValue = GUILayout.BeginScrollView(scrollValue, GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));
		foreach (var obj in debugObjects)
		{
			if(obj is IDebugGUI gui) gui.OnDebugGUI();
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	void DrawToolbar()
	{
		FlexibleSpace();
		if (ToolButton("RESET")) ConsoleWrapper.ResetGame();
		if (ToolButton("QUIT")) ConsoleWrapper.QuitGame();
	}
}