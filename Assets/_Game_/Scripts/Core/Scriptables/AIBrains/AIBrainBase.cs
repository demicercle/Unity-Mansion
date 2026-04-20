using System.Collections;
using UnityEngine;

//[CreateAssetMenu(fileName = "AIBrain", menuName = "AIBrain", order = 0)]
public abstract class AIBrainBase : ScriptableObject
{
	public GameView gameView => GameView.GetInstance();
//	public GameData gameData => gameView.gameData;
	public int defaultPriority;
	
	abstract public IEnumerator Think(CharacterController character);
	
	public AIBrainBase Clone() => Instantiate(this);

	/*public Coroutine WaitForRoutines() => gameView.StartCoroutine(DoWaitForRoutines());
	
	IEnumerator DoWaitForRoutines()
	{
		while(gameView.HasCoroutines()) yield return null;
	}*/

	virtual public int GetPriority(CharacterController character) => defaultPriority;

	virtual public void DrawGizmos(CharacterController character)
	{
		
	}
}