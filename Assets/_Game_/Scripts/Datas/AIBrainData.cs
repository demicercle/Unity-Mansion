using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "AIBrain", menuName = "AIBrain", order = 0)]
public abstract class AIBrainData : ScriptableObject
{
//	public GameView gameView => gameView;
//	public GameData gameData => gameView.gameData;
	public int defaultPriority;
	
	abstract public IEnumerator Think(CharacterView character);
	
	public AIBrainData Clone() => Instantiate(this);

	virtual public int GetPriority() => defaultPriority;
}