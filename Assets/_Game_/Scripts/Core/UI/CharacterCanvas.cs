using System;
using UnityEngine;
using UnityEngine.UI;


public class CharacterCanvas : MonoBehaviour
{
	[SerializeField] private CharacterEntity selfCharacter;
	[SerializeField] private Image detectionFill;
	[SerializeField] private Toggle toggleDetection;

	private CharacterDetection selfDetection;
	private float detectionAmount;
	
	private void OnEnable()
	{
		if (!selfCharacter) selfCharacter = GetComponentInParent<CharacterEntity>();
		if (!selfCharacter) return;
		selfCharacter.TryGetComponent(out selfDetection);
		detectionFill.fillAmount = detectionAmount = 0f;
	}

	private void LateUpdate()
	{
		if (!selfCharacter) return;
		
		detectionAmount = selfDetection ? selfDetection.GetAwarenessValueMax() : 0f;
		
		toggleDetection.isOn = selfCharacter.IsCharacterRevealed() && !selfCharacter.IsDead()
		                                                           && !selfCharacter.HasDetectedAnyPartyCharacter()
		                                                           && selfCharacter.IsEnemyToPlayer()
		                                                           && detectionAmount > 0f;
		
		if (toggleDetection.isOn)
		{
			detectionFill.fillAmount = Mathf.MoveTowards(detectionFill.fillAmount, detectionAmount, Time.deltaTime);
		}
	}
}