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
	static public bool IsEnabled { get; set; }
	static public void Open() => IsEnabled = true;
	static public void Close() => IsEnabled = false;
	
	static public void Header(string label) => GUILayout.Label(label);
	static public float Field(float value, string label) => RapidGUI.RGUI.Field(value, label);
	static public int Field(int value, string label) => RapidGUI.RGUI.Field(value, label);
	static public Vector2 Field(Vector2 value, string label) => RapidGUI.RGUI.Field(value, label);

	[Header("Viewport Rect")]
	[Range(0f, 1f)] public float viewX = 0;
	[Range(0f, 1f)] public float viewY = 0;
	[Range(0f, 1f)] public float viewWidth = 0.3f;
	[Range(0f, 1f)] public float viewHeight = 0.5f;
	
	[Header("Debug Objects")]
	public List<Object> debugObjects = new List<Object>();

	[Space] [Space] 
	[ReadOnly] [SerializeField] private Vector2 scrollValue;
	[ReadOnly] [SerializeField] private Rect screenRect;
	[ReadOnly] [SerializeField] private Vector2 screenSize;

	private void OnDrawGizmosSelected()
	{
		Update();
	}

	private void Awake()
	{
		if(!Application.isEditor && !Debug.isDebugBuild)
			Destroy(this);
	}

	private void Update()
	{
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
		if (!IsEnabled) return;
		GUILayout.BeginArea(screenRect, new GUIStyle("window"));
		scrollValue = GUILayout.BeginScrollView(scrollValue, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
		foreach (var obj in debugObjects)
		{
			if(obj is IDebugGUI gui) gui.OnDebugGUI();
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}
}