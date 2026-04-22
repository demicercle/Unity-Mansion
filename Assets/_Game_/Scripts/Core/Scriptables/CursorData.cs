using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CursorType
{
	DefaultArrow,
	
	Waiting,
	Loading,
	
	Navigate,
	Select,
	NoActions,
	
	OpenDoor,
	Attack,
	Talk,
	
	Blocked
}

[CreateAssetMenu(fileName = "CursorData", menuName = "CursorData", order = 0)]
public class CursorData : ScriptableObject
{
	static public CursorData GetCursorData(CursorType cursorType)
	{
		LoadCursors();
		if(cursorDataLookup.ContainsKey(cursorType))
			return cursorDataLookup[cursorType];
		Debug.LogWarning("cursor type not found: " + cursorType);
		if(!defaultCursor) defaultCursor = ScriptableObject.CreateInstance<CursorData>();
		return defaultCursor;
	}

	public static bool HasCursor(CursorType cursorType)
	{
		LoadCursors();
		return cursorDataLookup.ContainsKey(cursorType);
	}

	private static CursorData defaultCursor;
	private static CursorData[] cursorDatas;
	private static Dictionary<CursorType, CursorData> cursorDataLookup;
	private static void LoadCursors()
	{
		if (cursorDatas == null)
			cursorDatas = Resources.LoadAll<CursorData>(Consts.ResPath_Cursors);
		
		if (cursorDataLookup == null)
		{
			cursorDataLookup = new Dictionary<CursorType, CursorData>();
			foreach (var data in cursorDatas)
			{
				if(!cursorDataLookup.ContainsKey(data.cursorType))
					cursorDataLookup.Add(data.cursorType, data);
				else Debug.LogWarning("duplicated cursor data: " + data.cursorType + " in " + data);
			}
		}
	}
	
	public int frame { get; private set; }

	public CursorType cursorType = default;
	public Texture2D[] textures = new Texture2D[1];
	public Vector2 hotspot = Vector2.zero;
	public float animationSpeed = 1;
	public bool loopAnimation = false;

	public void Apply(bool animate, RawImage rawImage = null, RectTransform cursorTransform = null)
	{
		var tex = textures[animate ? loopAnimation ? (int)(frame/30 * animationSpeed) % textures.Length : (int)Mathf.Clamp(frame, 0, textures.Length - 1) : 0];
		if (rawImage != null && cursorTransform != null)
		{
			rawImage.texture = tex;
			cursorTransform.anchoredPosition = Input.mousePosition;
			cursorTransform.pivot = hotspot;
		}
		else
		{
			Cursor.SetCursor(tex, hotspot, CursorMode.Auto);
		}

		if (animate)
		{
			if (frame == 0) 
				frame = textures.Length - 1;
			else 
				frame += 1;
		}
		else
		{
			frame = 0;
		}
	}
}