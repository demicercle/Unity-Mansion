using Pathfinding;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
	[Header("Navigation")]
	public AIPath aiAgent;
	
	[Header("Animation")]
	public Animator animator;
	public float velocityScale = 1.0f;
	
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
