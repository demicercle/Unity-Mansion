using System;
using UnityEngine;
using UnityEngine.UI;

public class DiceSprite : MonoBehaviour
{
	private static DiceSprite firstInstance;
	
	[SerializeField] private float rollDuration = 1f;
	[SerializeField] private Image spriteImage;
	[SerializeField] private Sprite[] numberSprites;
	
	private int displayResult;
	private float time;
	private int lastIndex;

	public void Emit(GameObject owner, int lastResult)
	{
		var clone = Instantiate(gameObject).GetComponent<DiceSprite>();
		clone.transform.SetParent(transform.parent, false);
		clone.displayResult = lastResult;
		Utils.GetOrAddComponent<ScreenFollowsWorld>(clone.gameObject).target = owner.transform;
		clone.gameObject.SetActive(true);
	}

	private void Awake()
	{
		if (!firstInstance)
		{
			firstInstance = this;
			CharacterEntity.onAnyCharacterRollDice += OnAnyDiceRolled;
			
			gameObject.SetActive(false);
		}
	}

	private void OnDestroy()
	{
		if (firstInstance == this)
		{
			CharacterEntity.onAnyCharacterRollDice -= OnAnyDiceRolled;
		}
	}

	private void OnAnyDiceRolled(CharacterEntity character, int lastResult)
	{
		Emit(character.gameObject, lastResult);
	}

	private void LateUpdate()
	{
		if (time < rollDuration)
		{
			spriteImage.sprite = numberSprites[(lastIndex++) % numberSprites.Length];
		}
		else
		{
			spriteImage.sprite = numberSprites[Mathf.Clamp(displayResult-1, 0, numberSprites.Length)];
		}
		
		time += Time.deltaTime;
	}
}