using System.Collections;
using UnityEngine;

//[CreateAssetMenu(fileName = "AIBrain", menuName = "AIBrain", order = 0)]
public abstract class AIBrainBase : ScriptableObject
{
	public int defaultPriority;
	
	public abstract IEnumerator Think(CharacterController selfCharacter);

	public virtual int GetPriority(CharacterController selfCharacter) => defaultPriority;

	public virtual void DrawGizmos(CharacterController selfCharacter)
	{
		
	}

	public virtual void Init(CharacterController selfCharacter)
	{
	}

	public virtual void Dispose()
	{
	}
}