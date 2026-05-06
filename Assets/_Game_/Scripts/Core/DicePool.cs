using UnityEngine;
using Random = System.Random;


public static class DicePool
{
	public static int debugMode { get; set; }
	public static float DiceToFloat(int faces, int value) => Mathf.InverseLerp(1, Mathf.Max(1, faces), value);
	public static int FloatToDice(int faces, float value) => (int)Mathf.Lerp(1, faces, Mathf.Clamp01(value));
	
	public static event System.Action onDiceRolled;

	public static void RollNewDices(int count)
	{
		while(count--> 0)
			RollNewDice();
	}

	public static void SetNextDiceResult(float diceToFloat)
	{
		lastResult = diceToFloat;
		onDiceRolled?.Invoke();
	}
	
	public static void RollNewDice()
	{
		if (seed == 0 || rng == null)
		{
			Debug.LogWarning("DicePool GetRandomSeed()");
			SetSeed(GetRandomSeed());
		}

		lastResult = (float)rng.NextDouble();
		if(debugMode >0) Debug.Log("DicePool>> Last Result: " + lastResult.ToString("N4"));
		
		onDiceRolled?.Invoke();
	}

	public static int GetRandomSeed() => int.Parse(System.DateTime.Now.Minute.ToString() +
	                                               System.DateTime.Now.Second.ToString() +
	                                               System.DateTime.Now.Millisecond.ToString());
	
	public static float LastResult => lastResult;
	public static int Seed => seed;

	public static void SetSeed(int newSeed)
	{
		seed = newSeed;
		rng = new Random(seed);
		Debug.Log("DicePool SetSeed " + newSeed);
	}
	
	private static int seed;
	private static float lastResult;
	private static float nextResult;
	private static Random rng;
}