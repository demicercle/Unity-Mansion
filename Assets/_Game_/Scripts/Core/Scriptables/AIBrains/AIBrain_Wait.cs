using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain_Wait", menuName = "AIBrain/Wait", order = 0)]
public class AIBrain_Wait : AIBrainBase
{
	[SerializeField] private float waitTime = 1.0f;
	
	public override IEnumerator Think(CharacterController character)
	{
		yield return new WaitForSeconds(waitTime);
	}
}