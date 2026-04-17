using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain", menuName = "AIBrain", order = 0)]
public abstract class AIBrainData : ScriptableObject
{
	public GameView gameView => GameView.GetInstance();
//	public GameData gameData => gameView.gameData;
	public int defaultPriority;
	
	abstract public IEnumerator Think(CharacterView character);
	
	public AIBrainData Clone() => Instantiate(this);

	public void StartCoroutine(IEnumerator routine) => gameView.QueueRoutine(routine);

	virtual public int GetPriority(CharacterView characterView) => defaultPriority;

	virtual public void DrawGizmos(CharacterView characterView)
	{
		
	}
}