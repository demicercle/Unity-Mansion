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
	private void Parse()
	{
		lines = dialogText.text.Split('\n');
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
	[SerializeField] private string line;
	[SerializeField] [ReadOnly] private string text;

	public void Init()
	{
		text = int.TryParse(line, out var lineIndex)
			? LanguageSettings.CurrentSettings.GetLine(lineIndex, line)
			: string.Empty;
	}

	public string GetValue()
	{
		if (int.TryParse(line.ToString(), out var lineIndex))
			return LanguageSettings.CurrentSettings.GetLine(lineIndex, "GetLine(" + lineIndex + ")");
		else
			return line;
	}
}

/*#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(LocalizedText))]
public class LocalizedTextDrawer : UnityEditor.PropertyDrawer
{
	public override void OnGUI(Rect rect, UnityEditor.SerializedProperty property, GUIContent label)
	{
		var lineProperty = property.FindPropertyRelative("line");
		var textProperty = property.FindPropertyRelative("text");
		textProperty.stringValue = int.TryParse(lineProperty.stringValue, out var lineIndex)
			? LanguageSettings.CurrentSettings.GetLine(lineIndex, lineProperty.stringValue)
			: lineProperty.stringValue;
	}
}
#endif*/