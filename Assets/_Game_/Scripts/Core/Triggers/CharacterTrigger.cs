using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class CharacterTrigger : MonoBehaviour
{
	public static int debugMode { get; set; }
	
	public static List<CharacterTrigger> activeTriggers = new List<CharacterTrigger>();
	
	[Header("Settings")]
	[SerializeField] private bool partyOnly = true;
	[SerializeField] private bool triggerOnEnter = true;
	[SerializeField] private bool triggerOnExit = false;
	[Min(-1)] [SerializeField] private int maxCount = 1;
	
	[SerializeField] private UnityEvent triggered;
	
	public event System.Action<CharacterTrigger> onTriggered;

	public bool HasCharacter(CharacterController character)
	{
		return characters.Contains(character);
	}

	public Bounds GetWorldBounds()
	{
		if(!collider) collider = GetComponent<Collider>();
		return collider.bounds;
	}

	private new Collider collider;
	private int triggerCount;
	
	[Header("Debug")]
	[ReadOnly][SerializeField] private List<CharacterController> characters = new List<CharacterController>();

	bool IsCharacterValid(CharacterController character) => !character.IsDead() && (!partyOnly || character.InParty());

	private void OnDrawGizmosSelected()
	{
		Utils.GetOrAddComponent<Collider>(gameObject).isTrigger = true;
	}

	private void TriggerNow()
	{
		if (maxCount == 0) return;
		triggerCount++;
		onTriggered?.Invoke(this);
		triggered.Invoke();
		if (triggerCount > maxCount)
		{
			collider.enabled = false;
		}
	}
	
	private void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent(out CharacterController character) && IsCharacterValid(character))
		{
			characters.Add(character);
			if(debugMode>0) Debug.Log(this + " OnTriggerEnter " + character);
			if(triggerOnEnter) TriggerNow();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent(out CharacterController character))
		{
			characters.Remove(character);
			if(debugMode>0) Debug.Log(this + " OnTriggerExit " + character);
			if (triggerOnExit) TriggerNow();
		}
	}

	private void Awake()
	{
		collider = GetComponent<Collider>();
		collider.isTrigger = true;
		collider.enabled = false;
	}

	private void OnEnable()
	{
		activeTriggers.Add(this);
	}

	private void OnDisable()
	{
		activeTriggers.Remove(this);
	}

	private void Start()
	{
		collider.enabled = true; // refresh events
	}
}
