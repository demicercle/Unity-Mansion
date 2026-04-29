using UnityEngine;
using Random = System.Random;


public static class DicePool
{
	public static int debugMode { get; set; }
	
	public static event System.Action onDiceRolled;

	public static void RollNewDices(int count)
	{
		while(count--> 0)
			RollNewDice(null);
	}
	
	public static void RollNewDice(object instigator)
	{
		if (seed == 0 || rng == null)
			SetSeed(GetRandomSeed());

		lastInstigator = instigator;
		lastResult = nextResult;
		nextResult = (float)rng.NextDouble();
		if(debugMode >0) Debug.Log("DicePool>> Last Result: " + lastResult.ToString("N4") + " Next Result: " + nextResult.ToString("N4"));
		
		onDiceRolled?.Invoke();
	}
	
	public static int GetRandomSeed() => int.Parse(System.DateTime.Now.Minute.ToString() + System.DateTime.Now.Second.ToString() + System.DateTime.Now.Millisecond.ToString());
	
	public static float LastResult => lastResult;
	public static object LastInstigator => lastInstigator;
	public static float NextResult => nextResult;
	public static int Seed => seed;

	public static void SetSeed(int newSeed)
	{
		seed = newSeed;
		rng = new Random(seed);
	}
	
	private static int seed;
	private static float lastResult;
	private static float nextResult;
	private static Random rng;
	private static object lastInstigator;
}