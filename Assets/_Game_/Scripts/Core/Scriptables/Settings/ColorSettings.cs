using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorSettings", menuName = "ColorSettings", order = 0)]
public class ColorSettings : ScriptableObject
{
	private static ColorSettings defaultSettingsInstance;
	public static ColorSettings DefaultSettings
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
	[SerializeField] private float defaultOutlineSize = 2.0f;
	[SerializeField]private Color defaultOutlineColor = Color.yellow;
		
	public static Color GetDeadColor() => DefaultSettings.deadColor;
	public static Color GetCharacterColor(CharacterEntity character, bool disabledIfNoActions = false, bool disabledIfNoMovePoints = false) => DefaultSettings.GetCurrentColor(character, disabledIfNoActions, disabledIfNoMovePoints);
	public static Color GetDisabledColor(CharacterFaction faction) => Color.Lerp(DefaultSettings.disabledColor, DefaultSettings.GetFactionColor(faction), DefaultSettings.disabledToFaction);
	public static Color GetDisabledColor() => DefaultSettings.disabledColor;
	
	public static float GetCharacterOutlineSize(CharacterEntity parentCharacter) => DefaultSettings.defaultOutlineSize;
	public static float GetDefaultOutlineSize() => DefaultSettings.defaultOutlineSize;
	public static Color GetDefaultOutlineColor() => DefaultSettings.defaultOutlineColor;
	
	//#region PRIVATE
	private Color GetCurrentColor(CharacterEntity character, bool disabledIfNoActions, bool disabledIfNoMovePoints)
	{
		if (character.IsDead()) 
			return GetDeadColor();
		
		if (!character.IsCurrentTurn())
			return GetDisabledColor(character.GetCurrentFaction());
		
		if (disabledIfNoActions && !character.HasActions()) 
			return GetDisabledColor(character.GetCurrentFaction());
			
		if (disabledIfNoMovePoints && !character.HasMovePoints()) 
			return GetDisabledColor(character.GetCurrentFaction());
		
		return GetFactionColor(character.GetCurrentFaction());
	}
	private Color GetFactionColor(CharacterFaction faction) => factionColors[(int)faction];
	//#endregion
}