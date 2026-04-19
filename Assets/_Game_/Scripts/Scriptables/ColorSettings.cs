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
	static public Color GetDisabledColor() => Instance.disabledColor;
	static public Color GetCharacterColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints) => Instance.GetCurrentColor(character, disabledIfNoActions, disabledIfNoMovePoints);
	public Color GetCurrentColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints)
	{
		if (character.IsDead()) 
			return GetDeadColor();
		
		if (character.InParty())
		{
			if (!character.HasUsedAction(CharacterAction.RunAction))
			{
				if (disabledIfNoActions && !character.HasActions()) 
					return GetDisabledColor();
			
				if (disabledIfNoMovePoints && !character.HasMovePoints()) 
					return GetDisabledColor();
			}
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

	public Color disabledColor = Color.gray;
	public Color deadColor = Color.black;
}