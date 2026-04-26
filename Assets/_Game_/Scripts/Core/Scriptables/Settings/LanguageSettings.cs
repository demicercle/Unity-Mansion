using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LanguageSettings", menuName = "LanguageSettings", order = 0)]
public class LanguageSettings : ScriptableObject
{
	private static LanguageSettings currentSettings;
	public static LanguageSettings CurrentSettings
	{
		get
		{
			if(!currentSettings) currentSettings = Resources.LoadAll<LanguageSettings>(Consts.ResPath_Settings).FirstOrDefault();
			if (!currentSettings)
			{
				Debug.LogWarning("no LanguageSettings found, create new one");
				currentSettings = CreateInstance<LanguageSettings>();
			}
			return currentSettings;
		}
	}
	
	public TextAsset dialogText;

	[SerializeField] [ReadOnly] private string[] lines;

	[ContextMenu("Parse")]
	public void Parse()
	{
		lines = dialogText.text.Split('\n');
		lines = new string[]{string.Empty}.Concat(lines).ToArray();
		Utils.SetDirty(this);
	}

	private void TryParse()
	{
		if (lines == null || lines.Length == 0)
			Parse();
	}
	
	public string GetLine(int index, string fallback)
	{
		if (!dialogText)
		{
			Debug.LogWarning(this + " has no dialogText");
			return fallback;
		}
		
		TryParse();
		
		if (index < 0 || index >= lines.Length)
		{
			return fallback;
		}
		
		return lines[index];
	}
}

[System.Serializable]
public class LocalizedText
{
	[SerializeField] private int lineIndex;
	[SerializeField] private string text;

	public void Parse()
	{
		LanguageSettings.CurrentSettings.Parse();
		text = LanguageSettings.CurrentSettings.GetLine(lineIndex, text);
	}

	public string GetValue()
	{
		return lineIndex <= 0 ? text : LanguageSettings.CurrentSettings.GetLine(lineIndex, text);
	}
}

/*#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(LocalizedText))]
public class LocalizedTextDrawer : UnityEditor.PropertyDrawer
{
	private const float Width = 28f;
	
	public override void OnGUI(Rect rect, UnityEditor.SerializedProperty property, GUIContent label)
	{
		var lineProp = property.FindPropertyRelative("lineIndex");
		var lineRect = new Rect(rect.position, new Vector2(Width, rect.height));
		lineProp.intValue = UnityEditor.EditorGUI.IntField(lineRect, lineProp.intValue);
		var textProp = property.FindPropertyRelative("text");
		var textRect = new Rect(rect.position + new Vector2(Width, 0), new Vector2(rect.width - Width, rect.height));
		textProp.stringValue = UnityEditor.EditorGUI.TextField(textRect, textProp.stringValue);
		if (GUI.changed)
		{
			textProp.stringValue = lineProp.intValue <= 0 ? textProp.stringValue : LanguageSettings.CurrentSettings.GetLine(lineProp.intValue, textProp.stringValue);
			property.serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif*/