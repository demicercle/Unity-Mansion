using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class CharacterEventHandler : MonoBehaviour
{
	[Header("Reference")]
	[SerializeField] private CharacterController character;
	[SerializeField] private bool revealedOnly;
	[Space]
	[Header("Action Events")]
	[SerializeField] private CharacterAction actionType;
	[SerializeField] private string filters;
	[SerializeField] private UnityEvent onActionUsed;
	[Space]
	[Header("Other Events")]
	public UnityEvent onDamaged;
	public UnityEvent onDeath;
	public UnityEvent onHealed;
	public UnityEvent onTurnStart;
	public UnityEvent onAttack;
	public UnityEvent onLastKill;

	[ContextMenu("Find Character")]
	private void FindCharacter()
	{
		if (!character) character = GetComponent<CharacterController>();
		if (!character) character = GetComponentInParent<CharacterController>();
		if (!character) character = GetComponentInChildren<CharacterController>();
		Utils.SetDirty(this);
	}

	private void Awake()
	{
		if (!character) 
			FindCharacter();
		
		if (!character)
		{
			Debug.LogWarning(this + " has no character");
			enabled = false;
		}
	}
	
	private void OnEnable()
	{
		if (!character) return;
		character.onUseAction += OnCharacterUseAction;
		character.onDamaged += OnCharacterDamaged;
		character.onDead += OnCharacterDeath;
		character.onHealed += OnCharacterHealed;
		GameTurnManager.Instance.onTurnStarted += OnTurnStarted;
		CharacterController.onAnyCharacterDamaged += OnAnyCharacterDamaged;
	}

	private void OnDisable()
	{
		if (!character) return;
		character.onUseAction -= OnCharacterUseAction;
		character.onDamaged -= OnCharacterDamaged;
		character.onDead -= OnCharacterDeath;
		character.onHealed -= OnCharacterHealed;
		GameTurnManager.Instance.onTurnStarted -= OnTurnStarted;
		CharacterController.onAnyCharacterDamaged -= OnAnyCharacterDamaged;
	}

	private void OnCharacterUseAction(CharacterAction action, object target)
	{
		if (action == actionType && Utils.TagCheck(filters, target))
		{
			Debug.Log(this + " OnCharacterUseAction onActionUsed.Invoke " + action);
			onActionUsed.Invoke();
		};
	}

	private void OnCharacterDamaged(float amount)
	{
		onDamaged?.Invoke();
	}

	private void OnCharacterDeath()
	{
		onDeath?.Invoke();
	}

	private void OnCharacterHealed(float amount)
	{
		onHealed?.Invoke();
	}

	private void OnTurnStarted()
	{
		if(character.IsCurrentTurn())
			onTurnStart?.Invoke();
	}

	private void OnAnyCharacterDamaged(CharacterController arg1, float arg2, object arg3)
	{
		if (arg3 as CharacterController == character && CheckState())
		{
			StartCoroutine(CheckDead(arg1));
		}
	}

	private IEnumerator CheckDead(CharacterController other)
	{
		yield return new WaitForSeconds(0.5f);
		if (other && other.IsDead())
		{
			Debug.Log(this + " in " + character + " CheckDead " + other + " lastKill!");
			onLastKill?.Invoke();
		}
	}

	private bool CheckState() => character && !character.IsDead() && (!revealedOnly || character.IsCharacterRevealed());
}