using System;
using UnityEngine;
using System.Linq;

public class CharacterSelection : MonoBehaviour, IView<CharacterController>, ICursorHandler
{
	#region Static Selection Methods
	static CharacterController selectedCharacter;
	static public event System.Action selectionChanged;
	
	static public CharacterController GetSelectedCharacter() => selectedCharacter;

	static public bool TryGetSelectedCharacter(out CharacterController character)
	{
		character = GetSelectedCharacter();
		return character != null;
	}

	static public bool IsCharacterSelected(CharacterController character) => selectedCharacter == character;

	static public void SelectCharacter(CharacterController character)
	{
		Debug.Log("SelectCharacter: " + character);

		if (selectedCharacter != null)
		{
			// deselect character
			//selectedCharacter.GetAgent().SetObstacle(true);
			selectedCharacter.NotifySelected(false);
		}
		
		selectedCharacter = character;

		if (selectedCharacter != null)
		{
			// select character
			//if(selectionSetObstacle) selectedCharacter.GetAgent().SetObstacle(false);
			selectedCharacter.NotifySelected(true);
		}
		
		selectionChanged?.Invoke();
	}

	static public void SelectPartyCharacter(int index)
	{
		var party = CharacterController.GetPartyCharacters().ToArray();
		if(index >= 0 && index < party.Length) SelectCharacter(party[index]);
	}
	
	static public int GetSelectedPartyCharacterIndex()
	{
		return selectedCharacter == null ? -1 : CharacterController.GetPartyCharacters().ToList().IndexOf(selectedCharacter);
	}
	
	static public void SelectNextCharacter()
	{
		var party = CharacterController.GetPartyCharacters().OrderBy(c => c.GetPartyIndex()).ToArray();
		var startIndex = GetSelectedPartyCharacterIndex();
		if (startIndex < 0)
		{
			Debug.Log(" Select First Character");
			SelectCharacter(party.FirstOrDefault());
			return;
		}
		
		var partyCount = party.Length;
		Debug.Log(" SelectNextCharacter " + startIndex + " partyCount=" + partyCount);
		while (partyCount-- > 0)
		{
			startIndex++;
			
			var newIndex = startIndex % party.Length;
			Debug.Log(" try select at " + newIndex);
			var newChar = party[newIndex];
			if (!newChar.IsDead())
			{
				SelectCharacter(newChar);
				return;
			}
		}
		
		Debug.LogWarning(" Has no character to select next!");
	}
	#endregion
	
	private CharacterController controller;
	public CharacterController GetController()
	{
		if(!controller) controller = GetComponent<CharacterController>();
		return controller;
	}
	
	[Header("Collision")]
	[SerializeField] private CapsuleCollider capsuleCollider;
	[SerializeField] private new Rigidbody rigidbody;

	[Header("Circles")] 
	[ReadOnly][SerializeField] private float defaultScale = 1f;
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
		var controller = GetController();
		var agent = controller.GetAgent();
		var radius = agent.GetRadius();
		var height = agent.GetHeight();

		if (selectionCircle)
		{
			selectionCircle.transform.localScale = Vector3.one * radius * defaultScale;
		}
		
		if (capsuleCollider)
		{
			capsuleCollider.height = height;
			capsuleCollider.radius = radius;
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
		controller = GetController();
		gameObject.layer = LayerMask.NameToLayer(Consts.LAYER_CHARACTER);
	}

	void LateUpdate()//TODO optim
	{
		currentColor = GameSettings.GetCharacterColor(GetController(), true, false);
		alphaColor = alphaCurve.Evaluate(CharacterSelection.IsCharacterSelected(controller) ? 1f : mouseOver ? 0.5f : 0.0f);
		
		if (selectionCircle)
		{
			selectionCircle.SetColor(currentColor, alphaColor);
		}

		if (movePointsCircle)
		{
			movePointsCircle.SetColor(currentColor, alphaColor);
			movePointsCircle.SetProgress(controller.GetMovePointsProgress());
		}
		
		UpdateComponents();

		mouseOver = false;
	}
	#endregion
}