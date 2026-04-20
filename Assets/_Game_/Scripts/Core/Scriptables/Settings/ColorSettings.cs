using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorSettings", menuName = "ColorSettings", order = 0)]
public class ColorSettings : ScriptableObject
{
	static private ColorSettings defaultSettingsInstance;
	static public ColorSettings DefaultSettings
	{
		get
		{
			if(!defaultSettingsInstance) defaultSettingsInstance = Resources.LoadAll<ColorSettings>(Consts.ResPath_Settings).FirstOrDefault();
			if (!defaultSettingsInstance)
			{
				Debug.LogWarning("no ColorSettings found, create new one");
				defaultSettingsInstance = CreateInstance<ColorSettings>();
			}
			return defaultSettingsInstance;
		}
	}
	
	[Header("Colors")]
	[SerializeField]private Color[] factionColors = new Color[3]
	{
		Color.blue,
		Color.green,
		Color.red
	};
	[Range(0f, 1f)] [SerializeField]private float disabledToFaction = 0.5f;
	[SerializeField]private Color disabledColor = Color.gray;
	[SerializeField]private Color deadColor = Color.black;
		
	static public Color GetDeadColor() => DefaultSettings.deadColor;
	static public Color GetCharacterColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints) => DefaultSettings.GetCurrentColor(character, disabledIfNoActions, disabledIfNoMovePoints);
	static public Color GetDisabledColor(CharacterFaction faction) => Color.Lerp(DefaultSettings.disabledColor, DefaultSettings.GetFactionColor(faction), DefaultSettings.disabledToFaction);

	
	//#region PRIVATE
	private Color GetCurrentColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints)
	{
		if (character.IsDead()) 
			return GetDeadColor();
		
		if (!character.IsCurrentTurn())
			return GetDisabledColor(character.GetCurrentFaction());
		
		if (!character.HasUsedAction(CharacterAction.RunAction) || !character.HasMovePoints())
		{
			if (disabledIfNoActions && !character.HasActions()) 
				return GetDisabledColor(character.GetCurrentFaction());
			
			if (disabledIfNoMovePoints && !character.HasMovePoints()) 
				return GetDisabledColor(character.GetCurrentFaction());
		}

		return GetFactionColor(character.GetCurrentFaction());
	}
	private Color GetFactionColor(CharacterFaction faction) => factionColors[(int)faction];
	//#endregion
}