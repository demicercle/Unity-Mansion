using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class DiceLauncher : MonoBehaviour
{
	public static int AllowedDiceRolls { get; set; }
	
	public static int debugMode => DicePool.debugMode;

	public int defaultDiceRolls = 3;
	public Image spriteImage;
	public Sprite[] numberSprites;
	public Sprite[] animatedSprites;
	public int framesDuration;
	public TMPro.TMP_Text allowedRollsText;
	public Button launchButton;
	
	private int animCount;
	
	private void ClickedLaunch()
	{
		AllowedDiceRolls -= 1;
		DicePool.RollNewDice();
	}

	private void Awake()
	{
		AllowedDiceRolls = defaultDiceRolls;
		
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
		if (character.TryGetLastDamageInstigator(out var killer))
		{
			if (killer.InParty())
			{
				// last hit
				AllowedDiceRolls += 1;
				if(debugMode > 0) Debug.Log(this + " last hit " + killer + " " + character + " => allowedDiceRolls=" + AllowedDiceRolls);
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
		if(DicePool.debugMode>0) Debug.Log(this + " OnDiceRolled " + animCount);
		if(animCount == 1) StartCoroutine("Animate");
	}

	private IEnumerator Animate()
	{
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
	}

	private void LateUpdate()
	{
		allowedRollsText.text = AllowedDiceRolls.ToString();
		launchButton.interactable = AllowedDiceRolls > 0 && animCount <= 0;
	}
}