using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "CharacterData", order = 0)]
public class CharacterData : ScriptableObject
{
	public System.Action<int> partyIndexChanged;
	public System.Action<Vector3> positionChanged;
	public System.Action<int> rotationChanged;
	public System.Action<int> actionsChanged;
	public System.Action<float> movesPointsChanged;
	public System.Action onKill;
	public System.Action<float> onDamaged;
	public System.Action onHeal;
	
	[Header("Profile")] // todo use private and [SerializeField]
	public string displayName;
	public Texture2D portraitTexture;
	
	[Header("Actions")]
	public int maxActions;
	public float maxMovePoints;
	public float runActionExtraMovePoints = 3.0f;
	public bool noAttack;
	[Range(0f, 10f)] public float attackRange = 0.0f;

	[Header("Health")] 
	[Range(1, 99)] public int maxHealth;
	public float lastAttackDamage;

	public float GetLastAttackDamage()
	{
		return lastAttackDamage;
	}

	private bool isDead;
	private float damageAmount;
	public int GetMaxHealth() => maxHealth;
	public float GetRemainingHealth() => maxHealth - damageAmount;

	public float GetDamageProgress() => Mathf.Clamp01(damageAmount / (float)maxHealth);

	public void TakeDamage(float amount, bool healthCheck = true)
	{
		if (healthCheck && isDead)
		{
			return;
		}
		
		if (amount != 0f)
		{
			damageAmount += amount;
			onDamaged?.Invoke(damageAmount);
		}

		if (healthCheck && GetRemainingHealth() <= 0)
		{
			Kill();
		}
	}
	
	public void Kill()
	{
		if (!isDead)
		{
			isDead = true;
			onKill?.Invoke();
		}
	}

	public void Heal()
	{
		if (damageAmount > 0)
		{
			damageAmount = 0;
			onHeal?.Invoke();
		}
	}
	
	public bool IsDead() => isDead; 
	
	[Header("Debug")]
	[SerializeField] private float usedMovePoints;
	[SerializeField] private int usedActions;
	[SerializeField] private CharacterView characterViewOwner;
	[SerializeField] private Vector3 position;
	[SerializeField] private int rotation;
	[SerializeField] private int partyIndex;
// TODO
	public bool CanAttack() => GetRemainingActions() > 0 && !noAttack;
	public int GetPartyIndex() => partyIndex;

	public void SetPartyIndex(int index)
	{
		partyIndex = index;
		partyIndexChanged?.Invoke(partyIndex);
	}

	public CharacterView GetCharacter() => characterViewOwner;

	public void SetPosition(Vector3 newPos)
	{
		position = newPos;
		positionChanged?.Invoke(newPos);
	}

	public void SetRotation(int newRotation)
	{
		rotation = newRotation;
		rotationChanged?.Invoke(newRotation);
	}

	public void ConsumeMovePoints(float v)
	{
		if (v != 0f)
		{
			usedMovePoints += v;
			movesPointsChanged?.Invoke(v);
		}
	}
	
	public float GetTotalMovePoints() => maxMovePoints;
	
	public float GetRemainingMovePoints() => GetTotalMovePoints() - usedMovePoints;
	
	public float GetMovePointsProgress() => 1f - Mathf.Clamp01(maxMovePoints <= 0 ? 0f : usedMovePoints / GetTotalMovePoints());
	
	public int GetRemainingActions() => maxActions - usedActions;

	public void ConsumeActions(int q)
	{
		usedActions += q;
		actionsChanged?.Invoke(q);
		Debug.Log(this + " ConsumeActions x" + q + " remains " + GetRemainingActions());
	}
	
	public void TurnBegins(int turn)
	{
		Debug.Log(this + " TurnBegins " + turn);
		usedMovePoints = 0f;
		usedActions = 0;
	}

	public void RunAction()
	{
		ConsumeActions(1);
		usedMovePoints -= runActionExtraMovePoints;
	}

	public void Init(CharacterView c, int inParty = -1)
	{
		Debug.Log(this + " Init " + c + " " + inParty);
		characterViewOwner = c;
		usedMovePoints = 0;
		usedActions = 0;
		damageAmount = 0;
		SetPartyIndex(inParty);
	}

	public void Dispose()
	{
		
	}
}