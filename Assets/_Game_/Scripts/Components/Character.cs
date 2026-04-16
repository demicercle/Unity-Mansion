using System;
using Pathfinding;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour, ICursorHandler
{
	[Header("Stats")] 
	[SerializeField] private string displayName;
	public string GetDisplayName() => displayName;
	[SerializeField] private Texture2D portrait;
	public Texture GetPortraitTexture() => portrait;
	
	[Header("Selection Circle")]
	[SerializeField] private CircleRenderer circleRenderer;
	[SerializeField] private Color defaultColor = Color.blue, partyColor = Color.green, enemyColor = Color.red;
	[SerializeField] private AnimationCurve alphaCurve = new AnimationCurve();
	
	[Header("Navigation")]
	[SerializeField] private AIPath aiAgent;
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float velocityScale = 1.0f;

	public void CursorOver()
	{
		mouseOver = true;
	}
	
	private Color currentColor;
	private bool mouseOver;
	private bool isSelected;
	private bool inParty;

	private void Awake()
	{
		currentColor = defaultColor;

		GameEvents.characterAddedToParty += CharacterAddedToParty;
		GameEvents.characterSelected += CharacterSelected;
	}

	private void CharacterSelected(Character arg1, bool arg2)
	{
		if (arg1 == this)
		{
			isSelected = arg2;
		}
	}

	private void OnDestroy()
	{
		GameEvents.characterAddedToParty -= CharacterAddedToParty;
		GameEvents.characterSelected -= CharacterSelected;
	}

	private void CharacterAddedToParty(Character arg1, int arg2)
	{
		if (arg1 == this)
		{
			inParty = true;
			currentColor = partyColor;
		}
	}

	void Start()
    {
	    if (animator != null)
	    {
		    animator.SetFloat(AnimationParameters.VelocityX, 0.0f);      
		    animator.SetFloat(AnimationParameters.VelocityZ, 0.0f);        
		    animator.SetInteger(AnimationParameters.Weapon, -1);
		    animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		    animator.SetTrigger(AnimationParameters.Trigger);
	    }
    }

    void LateUpdate()
    {
	    circleRenderer.SetColor(currentColor, alphaCurve.Evaluate(isSelected ? 1f : mouseOver ? 0.5f : 0.0f));

	    if (animator != null)
	    {
		    var localVelocity = aiAgent.velocity;
		    animator.SetFloat(AnimationParameters.VelocityX, 0);      
		    animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
		    animator.SetBool(AnimationParameters.Moving, aiAgent.isStopped == false);
	    }

	    mouseOver = false;
    }
}
