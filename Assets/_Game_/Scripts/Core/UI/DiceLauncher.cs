using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class DiceLauncher : MonoBehaviour
{
	public static event System.Action onDiceRollEnds;
	
	public Image spriteImage;
	public Sprite[] numberSprites;
	public Sprite[] animatedSprites;
	public int framesDuration;

	public int allowedDiceRolls = 3;
	public TMPro.TMP_Text allowedRollsText;
	public Button launchButton;
	
	private int animCount;
	
	private void ClickedLaunch()
	{
		allowedDiceRolls -= 1;
		DicePool.RollNewDice();
	}

	private void Awake()
	{
		DicePool.onDiceRolled += OnDiceRolled;
		CharacterEntity.onAnyCharacterDead += OnAnyCharacterDead;
		launchButton.onClick.AddListener(ClickedLaunch);
	}

	private void OnDestroy()
	{
		DicePool.onDiceRolled -= OnDiceRolled;
		CharacterEntity.onAnyCharacterDead -= OnAnyCharacterDead;
		launchButton.onClick.RemoveListener(ClickedLaunch);
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

	private void Start()
	{
		DicePool.RollNewDice();
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
		allowedRollsText.gameObject.SetActive(false);

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

		allowedRollsText.gameObject.SetActive(true);
		launchButton.interactable = allowedDiceRolls > 0;
		onDiceRollEnds?.Invoke();
	}
}