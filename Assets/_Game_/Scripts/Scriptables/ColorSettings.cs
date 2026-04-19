using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ColorSettings", menuName = "ColorSettings", order = 0)]
public class ColorSettings : ScriptableObject
{
	static private ColorSettings instance;
	static public ColorSettings Instance
	{
		get
		{
			if(!instance) instance = Resources.Load<ColorSettings>("ColorSettings");
			if(!instance) instance = CreateInstance<ColorSettings>();
			return instance;
		}
	}

	static public Color GetDeadColor() => Instance.deadColor;
	static public Color GetCharacterColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints) => Instance.GetCurrentColor(character, disabledIfNoActions, disabledIfNoMovePoints);
	static public Color GetDisabledColor(CharacterFaction faction) => Color.Lerp(Instance.disabledColor, Instance.GetFactionColor(faction), Instance.disabledToFaction);
	public Color GetCurrentColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints)
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
	
	public Color GetFactionColor(CharacterFaction faction) => factionColors[(int)faction];

	public Color[] factionColors = new Color[3]
	{
		Color.blue,
		Color.green,
		Color.red
	};
	[Range(0f, 1f)] public float disabledToFaction = 0.5f;
	public Color disabledColor = Color.gray;
	public Color deadColor = Color.black;
}