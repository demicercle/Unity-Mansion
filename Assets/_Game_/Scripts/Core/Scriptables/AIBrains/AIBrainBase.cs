using System.Collections;
using UnityEngine;

//[CreateAssetMenu(fileName = "AIBrain", menuName = "AIBrain", order = 0)]
public abstract class AIBrainBase : ScriptableObject
{
	public int defaultPriority;
	
	public abstract IEnumerator Think(CharacterController character);

	public virtual int GetPriority(CharacterController character) => defaultPriority;

	public virtual void DrawGizmos(CharacterController character)
	{
		
	}
}