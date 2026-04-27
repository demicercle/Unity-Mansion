using System;
using UnityEngine;


[CreateAssetMenu(fileName = "CharacterData", menuName = "CharacterData", order = 0)]
public class CharacterData : ScriptableObject
{
	[Header("Profile")]
	public string displayName;
	public Texture2D portraitTexture;
	public CharacterFaction faction;
	
	[Header("Actions")]
	public int maxActions = 1;
	public float maxMovePoints = 3.0f;
	public float runActionExtraMovePoints = 3.0f;

	[Header("Health")] 
	[Range(1, 99)] public int maxHealth;
}