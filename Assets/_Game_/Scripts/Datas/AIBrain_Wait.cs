using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain_Wait", menuName = "AIBrain/Wait", order = 0)]
public class AIBrain_Wait : AIBrainData
{
	public float time = 1.0f;
	
	public override IEnumerator Think(CharacterView character)
	{
		yield return new WaitForSeconds(time);
	}
}