using System;
using UnityEngine;
using System.Linq;

public class CharacterSelection : MonoBehaviour
{
	private const float CapsuleRadiusOffset = -0.1f;

	public static int debugMode => GameInput.debugMode;
	
	#region Static Selection Methods
	public static event System.Action selectionChanged;
	
	public static CharacterEntity GetSelectedCharacter() => selectedCharacter;

	public static bool TryGetSelectedCharacter(out CharacterEntity character)
	{
		character = GetSelectedCharacter();
		return character != null;
	}

	public static bool IsCharacterSelected(CharacterEntity character) => selectedCharacter == character;

	public static void SelectCharacterAt(int index)
	{
		var partyChars = CharacterEntity.GetPartyCharacters();
		if (index < 0 || index >= partyChars.Length)
		{
			Debug.LogWarning("SelectCharacterAt invalid index " + index + " / " + partyChars.Length);
			return;
		}
		
		SelectCharacter(partyChars[index]);
	}

	public static void SelectCharacter(CharacterEntity newCharacter, bool silent = false)
	{
		Debug.Log("SelectCharacter: " + newCharacter);

		if (selectedCharacter != null)
		{
			selectedCharacter.NotifySelected(false);
		}
		
		selectedCharacter = newCharacter;

		if (selectedCharacter != null)
		{
			selectedCharacter.NotifySelected(true);
		}
		
		if(!silent)
			selectionChanged?.Invoke();
	}

	public static void SelectPartyCharacter(int index)
	{
		var party = CharacterEntity.GetPartyCharacters().ToArray();
		if(index >= 0 && index < party.Length) SelectCharacter(party[index]);
	}
	
	public static int GetSelectedPartyCharacterIndex()
	{
		return selectedCharacter == null ? -1 : CharacterEntity.GetPartyCharacters().ToList().IndexOf(selectedCharacter);
	}

	public static void SelectFirstCharacter() =>
		SelectCharacter(CharacterEntity.GetPartyCharacters().FirstOrDefault());
	
	public static void SelectNextCharacter(bool withActions = false, bool checkDead = true)
	{
		var startIndex = TryGetSelectedCharacter(out var selected) ? selected.GetPartyIndex() : -1;
		if (startIndex < 0)
		{
			SelectFirstCharacter();
			return;
		}

		var party = CharacterEntity.GetPartyCharacters().ToArray();
		var partyCount = party.Length;
		var newIndex = (startIndex + 1) % partyCount;
		if(debugMode >0) Debug.Log("SelectNextCharacter " + startIndex + " " + selected + " > " + newIndex);
		SelectCharacter(party[newIndex]);
		if (checkDead && party[newIndex].IsDead() && CharacterEntity.CountLivingPartyCharacters() > 1)
		{
			if(debugMode >0) Debug.Log(party[newIndex] + " is dead, select next");
			SelectNextCharacter();
		}

		if (withActions && !party[newIndex].HasActions())
		{
			if(debugMode >0) Debug.Log(party[newIndex] + " !HasActions, select next");
			SelectNextCharacter();
		}
	}
	#endregion
	
	private static CharacterEntity selectedCharacter;
	private CharacterEntity _selfCharacter;
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private new Rigidbody rigidbody;

	[Header("Circles")] 
	[SerializeField] private float defaultScale = 1f;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	[SerializeField] private CircleRenderer selectionCircle;
	[SerializeField] private CircleRenderer movePointsCircle;
	
	public void CursorOver()
	{
		mouseOver = true;
	}
	
	#region PRIVATE
	
	private Color currentColor;
	private bool mouseOver;
	private float alphaColor;
	
	[ContextMenu("UpdateComponents")]
	private void UpdateComponents()
	{
		if (!_selfCharacter) _selfCharacter = GetComponent<CharacterEntity>();
		if (!_selfCharacter) return;
		var agent = _selfCharacter.GetAgent();
		var radius = agent.GetRadius();
		var height = agent.GetHeight();

		if (selectionCircle)
		{
			selectionCircle.transform.localScale = Vector3.one * radius * defaultScale;
		}
		
		if (capsuleCollider)
		{
			capsuleCollider.height = height;
			capsuleCollider.radius = radius + CapsuleRadiusOffset;
			capsuleCollider.center = Vector3.up * height / 2f;
		}

		if (rigidbody)
		{
			rigidbody.isKinematic = true;
			rigidbody.useGravity = false;
		}
	}

	private void Awake()
	{
		_selfCharacter = GetComponent<CharacterEntity>();
		if (!_selfCharacter)
		{
			Debug.LogWarning(this + " has no CharacterController");
			enabled = false;
			return;
		}
		
		gameObject.layer = Layers.LayerCharacter;
	}

	private void LateUpdate()//TODO optim
	{
		capsuleCollider.enabled = _selfCharacter.IsCharacterRevealed();
		
		currentColor = ColorSettings.GetCharacterColor(_selfCharacter, true, false);
		alphaColor = alphaCurve.Evaluate(CharacterSelection.IsCharacterSelected(_selfCharacter) ? 1f : mouseOver ? 0.5f : 0.0f);
		
		if (selectionCircle)
		{
			selectionCircle.gameObject.SetActive(!_selfCharacter.IsDead() || _selfCharacter.InParty());
			selectionCircle.SetColor(currentColor, alphaColor);
		}
		
		if (movePointsCircle)
		{
			movePointsCircle.enabled = GameTurnManager.Instance.IsTurnBasedMode();
			movePointsCircle.SetColor(currentColor, alphaColor);
			movePointsCircle.SetProgress(_selfCharacter.GetMovePointsProgress());
		}
		
		UpdateComponents();

		mouseOver = false;
	}
	#endregion
}