using System.Linq;
using UnityEngine;
using UnityEngine.Events;


public class CharacterEventHandler : MonoBehaviour
{
	[SerializeField] private CharacterController character;
	[SerializeField] private UnityEvent onActionUsed;
	[SerializeField] private CharacterAction actionType;
	[SerializeField] private string filters;
	
	

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
		FindCharacter();
		if (!character)
		{
			Debug.LogWarning(this + " has no character");
			enabled = false;
		}
	}
	
	private void OnEnable()
	{
		character.onUseAction += OnCharacterUseAction;
	}

	private void OnDisable()
	{
		if (!character) return;
		character.onUseAction -= OnCharacterUseAction;
	}

	private void OnCharacterUseAction(CharacterAction action, object target)
	{
		if (action == actionType && Utils.TagCheck(filters, target))
		{
			Debug.Log(this + " OnCharacterUseAction onActionUsed.Invoke " + action);
			onActionUsed.Invoke();
		};
	}
}