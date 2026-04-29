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

	public int allowedDiceRolls = 3;
	public TMPro.TMP_Text allowedRollsText;
	public Button launchButton;
	
	private int animCount;
	
	public void LaunchNew()
	{
		allowedDiceRolls -= 1;
		DicePool.RollNewDice();
	}

	private void Awake()
	{
		DicePool.onDiceRolled += OnDiceRolled;
		CharacterEntity.onAnyCharacterDead += OnAnyCharacterDead;
		launchButton.onClick.AddListener(LaunchNew);
	}

	private void OnDestroy()
	{
		DicePool.onDiceRolled -= OnDiceRolled;
		CharacterEntity.onAnyCharacterDead -= OnAnyCharacterDead;
		launchButton.onClick.RemoveListener(LaunchNew);
	}

	private void Start()
	{
		DicePool.RollNewDices(2);
	}

	private void OnAnyCharacterDead(CharacterEntity character)
	{
		foreach (var other in CharacterEntity.AllCharacters)
		{
			if (other == character || other.IsDead() || !other.InParty()) continue;
			if (other.GetLastAttackTarget() == character)
			{
				allowedDiceRolls += 1;
				if(DicePool.debugMode>0) Debug.Log(this + " " + other + " gains new dice for last kill! " + character);
				return;
			}
		}
	}

	private void OnDiceRolled()
	{
		animCount++;
		allowedRollsText.text = allowedDiceRolls.ToString();
		if(DicePool.debugMode>0) Debug.Log(this + " OnDiceRolled " + animCount);
		if(animCount == 1) StartCoroutine("Animate");
	}

	private IEnumerator Animate()
	{
		launchButton.interactable = false;
		allowedRollsText.enabled = false;

		while (animCount > 0)
		{
			var f = 0;
			while (f++ < framesDuration)
			{
				spriteImage.sprite = animatedSprites[Random.Range(0, animatedSprites.Length)];
				yield return null;
			}

			var result = DicePool.LastResult;
			var index = (int)Mathf.Lerp(0, 19, result);
			spriteImage.sprite = numberSprites[index];
			if(DicePool.debugMode>0) Debug.Log(this + " #" + animCount + " animated, index=" + index + " for result=" + result);
			animCount--;
			yield return null;
		}

		allowedRollsText.enabled = true;
		launchButton.interactable = allowedDiceRolls > 0;
	}
}