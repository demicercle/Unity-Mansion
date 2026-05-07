using System;
using UnityEngine;


[CreateAssetMenu(fileName = "CharacterData", menuName = "CharacterData", order = 0)]
public class CharacterData : ScriptableObject
{
	[Header("Profile")]
	[SerializeField] private string displayName;
	public string GetDisplayName() => displayName;
	[SerializeField] private Texture2D portraitTexture;
	public Texture2D GetPortrait() => portraitTexture;
	[SerializeField] private CharacterFaction faction;
	public CharacterFaction GetFaction() => faction;
	
	[Header("Actions")]
	[SerializeField] private int maxActions = 1;
	public int GetMaxActions() => maxActions;
	[SerializeField] private float maxMovePoints = 3.0f;
	public float GetMaxMovePoints() => maxMovePoints;
	[SerializeField] private float runActionExtraMovePoints = 3.0f;
	public float GetRunActionExtraMovePoints() => runActionExtraMovePoints;
	[Range(1, 20)] [SerializeField] private int dodgeChance = 10;
	public int GetDodgeChance() => dodgeChance;
	
	[Range(0f, 1f)] [SerializeField] private float stealthValue = 0.33f;
	public float GetStealthValue() => stealthValue;


	[Header("Health")] 
	[Range(1, 99)] [SerializeField] private int maxHealth;
	public int GetMaxHealth() => maxHealth;
}