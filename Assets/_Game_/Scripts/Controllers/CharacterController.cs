using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
	private const int PLAYER_FACTION = 1;

	private GameController gameController => GameView.GetInstance().GetController();
	
	/*public enum ActionType TODO
	{
		None,
		InteractAction,
		RunAction
	}*/
	
	public event System.Action statsChanged;
	public event System.Action<bool> selectionChanged;
	public event System.Action<float> onHealed;
	public event System.Action<float> onDamaged;
	public event System.Action<int> onPartyIndexChanged;
	public event System.Action onKilled;
	public event System.Action onMoved;
	public event System.Action onRotate;
	
	[SerializeField] private CharacterData characterData;
	[SerializeField] private List<AIBrainData> aiBrains =  new List<AIBrainData>();

	[ReadOnly] [SerializeField] private bool isSelected = false;
	[ReadOnly] [SerializeField] private int partyIndex = -1;
	[ReadOnly] [SerializeField] private float usedMovePoints;
	[ReadOnly] [SerializeField] private int usedActions;
	[ReadOnly] [SerializeField] private float totalDamages;
	[ReadOnly] [SerializeField] private int rotation;
	[ReadOnly] [SerializeField] private Vector3 position;
	[ReadOnly] [SerializeField] private bool isDead;

	public bool IsSelected() => isSelected;
	public void SetSelected(bool value)
	{
		if (value != isSelected)
		{
			isSelected = value;
			selectionChanged?.Invoke(value);
			gameController.SelectionChanged();
			GameView.GetInstance().SelectionChanged();
		}
	}

	public void NewTurn(int turn)
	{
		usedActions = 0;
		usedMovePoints = 0;
	}

	public bool HasActions() => usedActions < characterData.maxActions;

	public void ConsumeActions(int actions = 1)
	{
		usedActions += actions;
		statsChanged?.Invoke();
	}

	public bool IsEnemyToPlayer() => characterData.factionIndex > PLAYER_FACTION;
	
	public bool InParty() => partyIndex >= 0;
	public int GetPartyIndex() => partyIndex;

	public void SetPartyIndex(int index)
	{
		partyIndex = index;
		onPartyIndexChanged?.Invoke(index);
		gameController.PartyChanged();
	}
	
	public float GetRemainingMovePoints() => Mathf.Max(0, characterData.maxMovePoints - usedMovePoints);
	public float GetMovePointsProgress() => 1f - usedMovePoints / characterData.maxMovePoints;

	public bool CanMove() => GetRemainingMovePoints() > 0f;
	public bool CanUseRunAction()
	{
		return  HasActions();
	}
	public bool CanInteract()
	{
		return HasActions();
	}
	
	public Vector3 GetPosition() => position;
	public void SetPosition(Vector3 newPos)
	{
		position = newPos;
		onMoved?.Invoke();
	}

	public int GetRotation() => rotation;
	public void SetRotation(int newRot)
	{
		rotation = newRot;
		onRotate?.Invoke();
	}
	
	public void ConsumeMovePoints(float amount)
	{
		usedMovePoints += amount;
		statsChanged?.Invoke();
	}
	
	public float GetTotalDamages() => totalDamages;
	public int GetMaxHealth() => characterData.maxHealth;
	public int GetCurrentHealthInt() => Mathf.CeilToInt(GetCurrentHealth());
	public float GetCurrentHealth() => GetMaxHealth() - GetTotalDamages();
	public float GetDamageProgress() => Mathf.Clamp01(GetCurrentHealth() / (float)GetMaxHealth());
	public bool IsDead() => isDead;

	public Texture2D GetPortraitTexture() => characterData.portraitTexture;
	public string GetDisplayName() => characterData.displayName;
	
	public void Heal(float amount = 0f)
	{
		if (amount == 0f)
			totalDamages = 0;
		else
			totalDamages = Mathf.Max(0, totalDamages - amount);
		
		onHealed?.Invoke(amount);
		statsChanged?.Invoke();
	}

	public void TakeDamage(float amount)
	{
		totalDamages += amount;
		
		onDamaged?.Invoke(amount);
		
		if (GetCurrentHealth() <= 0f)
		{
			TryKill();
			return;
		}
		
		statsChanged?.Invoke();
	}

	public void TryKill()
	{
		if (!IsDead())
		{
			isDead = true;
			onKilled?.Invoke();
			statsChanged?.Invoke();
		}
	}
	

	public float GetAttackRange() => characterData.attackRange;
	
	public bool CanAttack() => !characterData.noAttack;

	public bool TryGetAIBrain(out AIBrainData aiBrain)
	{
		aiBrains = aiBrains.OrderBy(b => b.GetPriority(this)).ToList();
		Debug.Log(this + " TryGetAIBrain " + Utils.ArrayStr(aiBrains));
		return (aiBrain = aiBrains.FirstOrDefault()) != null;
	}
}