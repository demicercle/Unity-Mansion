using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class CharacterTrigger : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField] private bool partyOnly = true;
	[SerializeField] private bool triggerOnEnter = true;
	[SerializeField] private bool triggerOnExit = false;
	[Min(-1)] [SerializeField] private int maxCount = 1;
	
	[SerializeField] private UnityEvent triggered;
	
	public event System.Action<CharacterTrigger> onTriggered;

	public CharacterController[] GetOverlappingCharacters() => characters.ToArray();

	private new Collider collider;
	private int triggerCount;
	
	[Header("Debug")]
	[ReadOnly][SerializeField] private List<CharacterController> characters = new List<CharacterController>();

	bool IsCharacterValid(CharacterController character) => !character.IsDead() && (!partyOnly || character.InParty());

	private void OnDrawGizmosSelected()
	{
		Utils.GetOrAddComponent<Collider>(gameObject).isTrigger = true;
	}

	void TriggerNow()
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
	
	void OnTriggerEnter(Collider other)
	{
		if (other.TryGetComponent(out CharacterController character) && IsCharacterValid(character))
		{
			characters.Add(character);
			if(triggerOnEnter) TriggerNow();
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent(out CharacterController character))
		{
			characters.Remove(character);
			if (triggerOnExit) TriggerNow();
		}
	}

	void Awake()
	{
		collider = GetComponent<Collider>();
	}
}
