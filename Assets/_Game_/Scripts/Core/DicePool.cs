using UnityEngine;
using Random = System.Random;


public static class DicePool
{
	public static int debugMode { get; set; }
	public static float DiceToFloat(int value, int faces = 20) => Mathf.InverseLerp(1, Mathf.Max(1, faces), value);
	public static int FloatToDice(int value, float faces = 20) => (int)Mathf.Lerp(1, faces, Mathf.Clamp01(value));
	
	public static event System.Action onDiceRolled;

	public static void RollNewDices(int count)
	{
		while(count--> 0)
			RollNewDice();
	}

	public static void SetNextDiceResult(int dice)
	{
		lastResult = Mathf.Clamp(dice, 1, 20);
		onDiceRolled?.Invoke();
	}

	public static int RollDice()
	{
		if (seed == 0 || rng == null)
			SetSeed(GetRandomSeed());
		return rng.Next(0, 20) + 1;
	}
	
	public static void RollNewDice()
	{
		if (seed == 0 || rng == null)
		{
			Debug.LogWarning("DicePool GetRandomSeed()");
			SetSeed(GetRandomSeed());
		}

		lastResult =rng.Next(0, 20) + 1;
		if(debugMode >0) Debug.Log("DicePool>> Last Result: " + lastResult);
		
		onDiceRolled?.Invoke();
	}

	public static int GetRandomSeed()
	{
		var newSeed = (int)(System.DateTime.Now.Ticks % int.MaxValue);
		return newSeed == 0 ? 1 : newSeed; // 0 means "unset"
	}
	
	public static int LastResult => lastResult;
	public static int Seed => seed;

	public static void SetSeed(int newSeed)
	{
		seed = newSeed;
		rng = new Random(seed);
		Debug.Log("DicePool SetSeed " + newSeed);
	}
	
	private static int seed;
	private static int lastResult;
	private static Random rng;
}