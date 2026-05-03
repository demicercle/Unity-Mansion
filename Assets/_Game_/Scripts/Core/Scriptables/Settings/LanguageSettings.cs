using System;
using System.Linq;
using UnityEngine;

public enum DialogFiles
{
	main,
	gamelog,
	items,
	script
}

[CreateAssetMenu(fileName = "LanguageSettings", menuName = "LanguageSettings", order = 0)]
public class LanguageSettings : ScriptableObject
{
	private static LanguageSettings[] allSettings;
	private static LanguageSettings fallbackSettings;

	public class Main
	{
		public static string Miss => GetSettings(DialogFiles.main).GetLine(10, "miss!");
		public static string WillMiss => GetSettings(DialogFiles.main).GetLine(10, "miss!");
		public static string Dodge => GetSettings(DialogFiles.main).GetLine(11, "dodge!");
		public static string Hit => GetSettings(DialogFiles.main).GetLine(12, "hit!");
		public static string WillHit => GetSettings(DialogFiles.main).GetLine(12, "hit!");
		public static string Cant => GetSettings(DialogFiles.main).GetLine(13, "can't!");
		public static string HitBlocked => GetSettings(DialogFiles.main).GetLine(13, "can't!");

		public static string AttackResult(CharacterEntity.AttackResult ar) => ar == CharacterEntity.AttackResult.Miss ? Miss : ar == CharacterEntity.AttackResult.Dodge ? Dodge : Hit; 
	}

	public static LanguageSettings GetSettings(DialogFiles fileType)
	{
		if (allSettings == null)
		{
			allSettings = Resources.LoadAll<LanguageSettings>(Consts.ResPath_Settings)
				.OrderBy(s => s.name)
				.ToArray();

			foreach (var s in allSettings)
			{
				s.Parse();
			}
		}

		if ((int)fileType >= allSettings.Length)
		{
			Debug.LogWarning("invalid language dialog file: " + fileType);
			
			if (!fallbackSettings)
				fallbackSettings = ScriptableObject.CreateInstance<LanguageSettings>();
			
			return fallbackSettings;
		}
		
		return allSettings[(int)fileType];
	}
	
	
	public TextAsset dialogText;

	[SerializeField] [ReadOnly] private string[] lines;

	[ContextMenu("Parse")]
	public void Parse()
	{
		if (!dialogText)
		{
			Debug.LogWarning(this + " has no dialogText");
			return;
		}
		
		lines = dialogText.text.Split('\n');
		lines = new string[]{string.Empty}.Concat(lines).ToArray();
#if UNITY_EDITOR
		if (Application.isPlaying) return;
		/*var names = System.Enum.GetNames(typeof(DialogFiles));
		for (int i = 0; i < names.Length; i++)
		{
			var sett = GetSettings((DialogFiles)i);
			sett.name = typeof(LanguageSettings) + " (" + i + ")";
			Debug.Log("rename " + sett + " " + i + " " + names[i]);
			UnityEditor.AssetDatabase.RenameAsset(UnityEditor.AssetDatabase.GetAssetPath(sett), name);
		}*/
		UnityEditor.EditorUtility.SetDirty(this);
		//UnityEditor.AssetDatabase.SaveAssets();
#endif
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
			//Debug.Log(this + " GetLine " + index + " fallbacks " + fallback);
			return fallback;
		}
		
		//Debug.Log(this + " GetLine " + index + " => " + lines[index]);
		return lines[index].Trim('\n');
	}

	private void OnEnable()
	{
		Parse();
	}
}

[System.Serializable]
public class LocalizedText
{
	[SerializeField] private int lineIndex;
	[SerializeField] private string text;
	
	public void Parse(DialogFiles fileType)
	{
		text = LanguageSettings.GetSettings(fileType).GetLine(lineIndex, text);
	}

	public string GetValue(DialogFiles fileType)
	{
		return lineIndex <= 0 ? text : LanguageSettings.GetSettings(fileType).GetLine(lineIndex, text);
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