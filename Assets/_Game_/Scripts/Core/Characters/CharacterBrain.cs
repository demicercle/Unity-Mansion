using System.Collections;
using UnityEngine;

public interface ICharacterBrain
{
	IEnumerator Think();
}

public class CharacterBrain : MonoBehaviour, ICharacterBrain
{
	[SerializeField] private bool canPatrol = true;
	[SerializeField] private bool canChase = true;
	[SerializeField] private float defaultWait = 1.5f;

	public IEnumerator Think()
	{
		Debug.Log(this + " Thinking...");
		yield return new WaitForSeconds(defaultWait);

		if (_chaseCharacter == null)
		{
			
		}
		
		yield return new WaitForSeconds(defaultWait);
		Debug.Log(this + " ends");
	}
	
	private CharacterController _selfCharacter;
	private CharacterDetection _characterDetection;
	private CharacterController _chaseCharacter;
	
	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterController>();
		if (!_selfCharacter)
		{
			Debug.LogWarning(this + " has no character controller!");
			enabled = false;
			return;
		}
		
		_characterDetection = GetComponent<CharacterDetection>();
		if (_characterDetection)
		{
			_characterDetection.onCharacterDetected += OnCharacterDetected;
			_characterDetection.onCharacterLost += OnCharacterLost;
		}
	}

	private void OnDestroy()
	{
		if (_characterDetection)
		{
			_characterDetection.onCharacterDetected -= OnCharacterDetected;
			_characterDetection.onCharacterLost -= OnCharacterLost;
		}
	}

	private void OnCharacterDetected(CharacterController otherCharacter)
	{
		
	}

	private void OnCharacterLost(CharacterController otherCharacter)
	{
		
	}
}