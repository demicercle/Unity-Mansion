using System;
using UnityEngine;


[CreateAssetMenu(fileName = "CharacterData", menuName = "CharacterData", order = 0)]
public class CharacterData : ScriptableObject
{
	
	/*public System.Action<int> partyIndexChanged;
	public System.Action<Vector3> positionChanged;
	public System.Action<int> rotationChanged;
	public System.Action<int> actionsChanged;
	public System.Action<float> movesPointsChanged;
	public System.Action onKill;
	public System.Action<float> onDamaged;
	public System.Action onHeal;*/
	
	[Header("Profile")] // todo use private and [SerializeField]
	public string displayName;
	public Texture2D portraitTexture;
	public CharacterFaction faction;
	
	[Header("Actions")]
	public int maxActions;
	public float maxMovePoints;
	public float runActionExtraMovePoints = 3.0f;
	public bool noAttack;
	[Range(0f, 10f)] public float attackRange = 0.0f;

	[Header("Health")] 
	[Range(1, 99)] public int maxHealth;
	public float lastAttackDamage;
	public float lastAttackHitTime;
}