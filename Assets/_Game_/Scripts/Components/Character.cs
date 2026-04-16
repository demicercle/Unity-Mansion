using Pathfinding;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
	[Header("Stats")] 
	[SerializeField] private string displayName;
	public string GetDisplayName() => displayName;
	[SerializeField] private Texture2D portrait;
	public Texture GetPortraitTexture() => portrait;
	
	[Header("Navigation")]
	[SerializeField] private AIPath aiAgent;
	
	[Header("Animation")]
	[SerializeField] private Animator animator;
	[SerializeField] private float velocityScale = 1.0f;
	
    void Start()
    {
		animator.SetFloat(AnimationParameters.VelocityX, 0.0f);      
		animator.SetFloat(AnimationParameters.VelocityZ, 0.0f);        
		animator.SetInteger(AnimationParameters.Weapon, -1);
		animator.SetInteger(AnimationParameters.TriggerNumber, (int)AnimatorTrigger.InstantSwitchTrigger);
		animator.SetTrigger(AnimationParameters.Trigger);
    }

    void LateUpdate()
    {
	    var localVelocity = aiAgent.velocity;
	    animator.SetFloat(AnimationParameters.VelocityX, 0);      
	    animator.SetFloat(AnimationParameters.VelocityZ, localVelocity.magnitude * velocityScale);  
	    animator.SetBool(AnimationParameters.Moving, aiAgent.isStopped == false);
    }
}
