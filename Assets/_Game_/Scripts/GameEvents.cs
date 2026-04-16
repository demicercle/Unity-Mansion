using UnityEngine;

public class GameEvents
{
	static public System.Action<Character, int> characterAddedToParty;
	static public System.Action<Character, bool> characterSelected;
	static public System.Action<Character, Vector3> characterMoved;
	static public System.Action<bool> routinesStarted;
	static public System.Action<int> newTurnStarted;
	static public System.Action<PropComponent, GameObject> propInteracted;
	static public System.Action<Character> runAction;
	static public System.Action<CharacterStats> actionUsed;
}
