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

	public CharacterEntity[] GetCharacters() => characters.ToArray();

	public bool HasCharacter(CharacterEntity character)
	{
		return characters.Contains(character);
	}

	public bool HasPosition(Vector3 world)
	{
		if (collider is SphereCollider)
		{
			return Vector3.Distance(transform.position, world) <= (collider as SphereCollider).radius;
		}
		else
		{
			return GetWorldBounds().Contains(world);
		}
	}

	public Bounds GetWorldBounds(float grow = 0.1f) // TODO if it's a sphere
	{
		if(!collider) collider = GetComponent<Collider>();
		worldBounds = collider.bounds;
		worldBounds.Expand(grow);
		return worldBounds; // TODO optim calculate this before
	}

	private new Collider collider;
	private int triggerCount;
	private Bounds worldBounds;
	
	[Header("Debug")]
	[ReadOnly][SerializeField] private List<CharacterEntity> characters = new List<CharacterEntity>();

	bool IsCharacterValid(CharacterEntity character) => !character.IsDead() && (!partyOnly || character.InParty());

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
		if (other.TryGetComponent(out CharacterEntity character) && IsCharacterValid(character))
		{
			characters.Add(character);
			if(debugMode>0) Debug.Log(this + " OnTriggerEnter " + character);
			if(triggerOnEnter) TriggerNow();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.TryGetComponent(out CharacterEntity character))
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
