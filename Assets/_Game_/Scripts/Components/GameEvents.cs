using UnityEngine;

public class GameEvents
{
	static public System.Action<Character, int> characterAddedToParty;
	static public System.Action<Character, bool> characterSelected;
}