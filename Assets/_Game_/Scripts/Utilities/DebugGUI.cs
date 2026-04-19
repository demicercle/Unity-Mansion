using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public interface IDebugGUI
{
	void OnDebugGUI();
}

public class DebugGUI : MonoBehaviour
{
	static public bool IsEnabled => UserSettings.GetCurrentSettings().showDebugGUI;

	static public void Space(float s = 5f) => GUILayout.Space(s);
	static public void BeginGroup(string label) => GUILayout.BeginVertical(new GUIContent(label), new GUIStyle("window"));
	static public void EndGroup() => GUILayout.EndVertical();
	static public float Field(float value, string label) => RapidGUI.RGUI.Field(value, label);
	static public int Field(int value, string label) => RapidGUI.RGUI.Field(value, label);
	static public Vector2 Field(Vector2 value, string label) => RapidGUI.RGUI.Field(value, label);
	static public bool Toggle(bool value, string label) => GUILayout.Toggle(value, label);

	[Header("Appearance")]
	[Range(0f, 1f)] public float viewX = 0;
	[Range(0f, 1f)] public float viewY = 0;
	[Range(0f, 1f)] public float viewWidth = 0.3f;
	[Range(0f, 1f)] public float viewHeight = 0.5f;
	public GUISkin guiSkin;
	public string guiTitle;
	public string guiStyleName;
	
	[Header("Debug Objects")]
	public List<Object> debugObjects = new List<Object>();

	[Space] [Space] 
	[ReadOnly] [SerializeField] private Vector2 scrollValue;
	[ReadOnly] [SerializeField] private Rect screenRect;
	[ReadOnly] [SerializeField] private Vector2 screenSize;
	
	private GUIStyle guiStyle;
	
	private void OnDrawGizmosSelected()
	{
		Update();
	}

	private void Awake()
	{
		if(!Application.isEditor && !Debug.isDebugBuild)
			Destroy(this);

		guiStyle = GUIStyle.none;
	}

	private void Update()
	{
		if (!IsEnabled) return;
		if (!string.IsNullOrEmpty(guiStyleName)) guiStyle = new GUIStyle(guiStyleName);
		
		screenSize.x = Screen.width;
		screenSize.y = Screen.height;
		
		screenRect.x = viewX * screenSize.x;
		screenRect.y = viewY * screenSize.y;
		screenRect.width = viewWidth * screenSize.y;
		screenRect.height = viewHeight * screenSize.y;
	}

	private void OnGUI()
	{
		if (!IsEnabled) return;
		GUI.skin = guiSkin;
		GUILayout.BeginArea(screenRect, new GUIContent(guiTitle), new GUIStyle(guiStyle));
		//GUILayout.BeginHorizontal(); GUILayout.FlexibleSpace(); if(GUILayout.Button("CLOSE", GUILayout.ExpandWidth(false))) IsEnabled = false; GUILayout.EndHorizontal();
		scrollValue = GUILayout.BeginScrollView(scrollValue, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
		foreach (var obj in debugObjects)
		{
			if(obj is IDebugGUI gui) gui.OnDebugGUI();
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}
}