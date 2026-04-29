using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class DiceLauncher : MonoBehaviour
{
	public Image spriteImage;
	public Sprite[] numberSprites;
	public Sprite[] animatedSprites;
	public int framesDuration;

	public UnityEvent<bool> launching;

	private int animCount;
	
	public void LaunchNew()
	{
		DicePool.RollNewDice();
	}

	private void Awake()
	{
		DicePool.onDiceRolled += OnDiceRolled;
	}

	private void OnDestroy()
	{
		DicePool.onDiceRolled -= OnDiceRolled;
	}

	private void Start()
	{
		LaunchNew();
		LaunchNew();
	}

	private void OnDiceRolled()
	{
		animCount++;
		if(DicePool.debugMode>0) Debug.Log(this + " OnDiceRolled " + animCount);
		StopAllCoroutines();
		StartCoroutine("Animate");
	}

	private IEnumerator Animate()
	{
		launching.Invoke(false);

		while (animCount > 0)
		{
			var f = 0;
			while (f++ < framesDuration)
			{
				spriteImage.sprite = animatedSprites[Random.Range(0, animatedSprites.Length)];
				yield return null;
			}

			var result = DicePool.LastResult;
			var index = Mathf.RoundToInt(result * numberSprites.Length);
			spriteImage.sprite = numberSprites[index];
			if(DicePool.debugMode>0) Debug.Log(this + " #" + animCount + " animated, index=" + index + " for result=" + result);
			animCount--;
			yield return null;
		}
		
		launching.Invoke(true);
	}
}