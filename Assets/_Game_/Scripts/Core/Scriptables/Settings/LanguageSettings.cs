using System;
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
			if (!currentSettings)
			{
				currentSettings = Resources.LoadAll<LanguageSettings>(Consts.ResPath_Settings).FirstOrDefault();
			}
			
			return currentSettings;
		}
	}

	public static DialogLines GetDialogLines(DialogFiles dialogFile)
	{
		return CurrentSettings.dialogLines[(int)dialogFile];
	}
	
	public class Main
	{
		public static string GetLine(int index, string fallback) => GetDialogLines(DialogFiles.main).GetLine(index, fallback);
		public static string NoActions => GetLine(9, "no actions!");
		public static string Miss => GetLine(10, "miss!");
		public static string WillMiss => GetLine(10, "miss!");
		public static string Dodge => GetLine(11, "dodge!");
		public static string Hit => GetLine(12, "hit!");
		public static string WillHit => GetLine(12, "hit!");
		public static string Cant => GetLine(13, "can't!");
		public static string HitBlocked => GetLine(13, "can't!");
		public static string TakeThis => GetLine(14, "take this!");
		public static string Run => GetLine(15, "run!");
		public static string Heal => GetLine(16, "heal!");

		public static string AttackResult(CharacterEntity.AttackResult ar) => ar == CharacterEntity.AttackResult.Miss ? Miss : ar == CharacterEntity.AttackResult.Dodge ? Dodge : Hit;
	}

	[SerializeField][ReadOnly] private DialogLines[] dialogLines;

	[ContextMenu("Parse")]
	public void Parse()
	{
		var enumNames = System.Enum.GetNames(typeof(DialogFiles));
		Debug.Log(this + " Parse " + Utils.ArrayStr(enumNames));
		dialogLines = new DialogLines[enumNames.Length];
		for (int i = 0; i < enumNames.Length; i++)
		{
			dialogLines[i] = new DialogLines();
			dialogLines[i].Parse((DialogFiles)i);
		}
		Utils.SetDirty(this);
	}

	private void OnEnable()
	{
		Parse();
	}
}

[System.Serializable]
public class DialogLines
{
	private static TextAsset[] dialogFiles;

	public string GetLine(int index, string defaultValue)
	{
		index -= 1;
		return index >= 0 && index < lines.Length && !string.IsNullOrEmpty(lines[index]) ? lines[index] : defaultValue;
	}

	public void Parse(DialogFiles dialogFile)
	{
		if (dialogFiles == null)
		{
			dialogFiles = Resources.LoadAll<TextAsset>(Consts.ResPath_Dialogs);
		}

		dialogText = dialogFiles[(int)dialogFile];
		
		if(dialogText)
			lines = dialogText.text.Split('\n');
	}

	[SerializeField][ReadOnly] private TextAsset dialogText;
	[SerializeField][ReadOnly] private string[] lines =  Array.Empty<string>();
}

public enum DialogFiles
{
	main,
	gamelog,
	items,
	script
}

[System.Serializable]
public class LocalizedText
{
	[SerializeField] private int lineIndex;
	[SerializeField] private string text;
	
	public void Parse(DialogFiles fileType)
	{
		text = LanguageSettings.GetDialogLines(fileType).GetLine(lineIndex, text);
	}

	public string GetValue(DialogFiles fileType)
	{
		return lineIndex <= 0 ? text : LanguageSettings.GetDialogLines(fileType).GetLine(lineIndex, text);
	}
}