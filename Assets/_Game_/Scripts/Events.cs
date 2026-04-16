using UnityEngine;

public class GameEvents
{
	static public System.Action<Character, int> characterAddedToParty;
	static public System.Action<Character, bool> characterSelected;
	static public System.Action<bool> routinesStarted;
	static public System.Action<int> newTurnStarted;
}

public class UIEvents
{
	static public System.Action<Character> clickSelectCharacter;
	static public System.Action clickEndTurn;
}