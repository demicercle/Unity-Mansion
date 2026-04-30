using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class CharacterEventTrigger : MonoBehaviour
{
	[System.Serializable]
	public class ActionEvent
	{
		public CharacterAction actionType;
		public string filters;
		public UnityEvent onActionUsed;
		public void Invoke() => onActionUsed?.Invoke();
	}
	
	[Header("Reference")]
	[SerializeField] private CharacterEntity character;
	[SerializeField] private bool revealedOnly;
	[Space]
	[Header("Action Events")]
	[SerializeField] private List<ActionEvent> actionEvents;

	[Space] [Header("Other Events")] 
	public UnityEvent<bool> onPartyChanged;
	public UnityEvent onDamaged;
	public UnityEvent onDeath;
	public UnityEvent onHealed;
	public UnityEvent onTurnStart;
	public UnityEvent onAttack;
	public UnityEvent<bool> onUseRangeWeapon;
	public UnityEvent onLastKill;
	public UnityEvent<float> onAwarenessChanged;

	[ContextMenu("Find Character")]
	private void FindCharacter()
	{
		if (!character) character = GetComponent<CharacterEntity>();
		if (!character) character = GetComponentInParent<CharacterEntity>();
		if (!character) character = GetComponentInChildren<CharacterEntity>();
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
		character.onPartyIndexChanged += OnPartyIndexChanged;
		if (character.TryGetComponent(out CharacterDetection detection))
		{
			detection.onAwarenessChanged += onAwarenessChanged.Invoke;
		}
		GameTurnManager.Instance.onTurnStarted += OnTurnChanged;
		GameTurnManager.Instance.onTurnEnded += OnTurnChanged;
		CharacterEntity.onAnyCharacterDamaged += OnAnyCharacterDamaged;
	}

	private void OnDisable()
	{
		if (!character) return;
		character.onUseAction -= OnCharacterUseAction;
		character.onDamaged -= OnCharacterDamaged;
		character.onDead -= OnCharacterDeath;
		character.onHealed -= OnCharacterHealed;
		character.onPartyIndexChanged -= OnPartyIndexChanged;
		if (character.TryGetComponent(out CharacterDetection detection))
		{
			detection.onAwarenessChanged -= onAwarenessChanged.Invoke;
		}
		GameTurnManager.Instance.onTurnStarted -= OnTurnChanged;
		GameTurnManager.Instance.onTurnEnded -= OnTurnChanged;
		CharacterEntity.onAnyCharacterDamaged -= OnAnyCharacterDamaged;
	}

	private void OnPartyIndexChanged(int newIndex, int count)
	{
		onPartyChanged?.Invoke(newIndex >= 0);
	}

	private void OnCharacterUseAction(CharacterAction action, object target)
	{
		foreach (var actionEvt in actionEvents)
		{
			if (action == actionEvt.actionType && TagCheck(actionEvt.filters, target))
			{
				Debug.Log(this + " OnCharacterUseAction " + actionEvt.actionType + " on " + target + " filters=" + actionEvt.filters + "onActionUsed.Invoke " + action);
				actionEvt.Invoke();
				break;
			};
		}

		if (action == CharacterAction.Attack)
		{
			onUseRangeWeapon?.Invoke(character.IsUsingRangeWeapon());
			onAttack?.Invoke();
		}
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

	private void OnTurnChanged()
	{
		if(character.IsCurrentTurn())
			onTurnStart?.Invoke();
	}

	private void OnAnyCharacterDamaged(CharacterEntity arg1, DamageInfo arg2, object arg3)
	{
		if (arg3 as CharacterEntity == character && CheckState())
		{
			StartCoroutine(CheckDead(arg1));
		}
	}

	private IEnumerator CheckDead(CharacterEntity other)
	{
		yield return new WaitForSeconds(0.5f);
		if (other && other.IsDead())
		{
			Debug.Log(this + " in " + character + " CheckDead " + other + " lastKill!");
			onLastKill?.Invoke();
		}
	}

	private bool CheckState() => character && !character.IsDead() && (!revealedOnly || character.IsCharacterRevealed());
	
	private bool TagCheck(string filters, object obj)
	{
		if (obj is ItemData)
			return (obj as ItemData).CompareTags(filters);
		
		return obj == null || string.IsNullOrWhiteSpace(filters) ||
		       filters.Split(',').ToList().Any(tag =>
		       {
			       if (obj is Component)
				       return (obj as Component).CompareTag(tag);

			       if (obj is GameObject)
				       return (obj as GameObject).CompareTag(tag);


			       return false;
		       });
	}
}