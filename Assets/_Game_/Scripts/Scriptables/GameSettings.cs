using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "GameSettings", order = 0)]
public class GameSettings : ScriptableObject
{
	static private GameSettings instance;
	static public GameSettings Instance
	{
		get
		{
			if(!instance) instance = Resources.Load<GameSettings>("ColorSettings");
			if(!instance) instance = CreateInstance<GameSettings>();
			return instance;
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
	[Space]
	[Header("Delays")]
	[Min(0f)][SerializeField]private float whenCameraStarts = 0.25f;
	[Min(0f)][SerializeField]private float whenTurnChanged = 4.0f;
	[Min(0f)][SerializeField]private float whenFocusNextCharacter = 3.0f;
	[Min(0f)][SerializeField]private float whenAIThinks = 1.5f;
	[Min(0f)][SerializeField]private float defaultActionSeconds = 3.0f;
	
	#region Delays
	static public float GetTurnChangedSeconds() => Instance.whenTurnChanged;
	static public float GetFocusCharacterSeconds() => Instance.whenFocusNextCharacter;
	static public float GetDefaultActionSeconds() => Instance.defaultActionSeconds;
	static public float GetCameraStartsSeconds() => Instance.whenCameraStarts;
	static public float GetAIThinksSeconds() => Instance.whenAIThinks;
	#endregion
		
	#region COLORS
	static public Color GetDeadColor() => Instance.deadColor;
	static public Color GetCharacterColor(CharacterController character, bool disabledIfNoActions, bool disabledIfNoMovePoints) => Instance.GetCurrentColor(character, disabledIfNoActions, disabledIfNoMovePoints);
	static public Color GetDisabledColor(CharacterFaction faction) => Color.Lerp(Instance.disabledColor, Instance.GetFactionColor(faction), Instance.disabledToFaction);
	#endregion

	
	#region PRIVATE
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
	#endregion
}