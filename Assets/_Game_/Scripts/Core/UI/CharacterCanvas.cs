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
		
		if (selfCharacter.TryGetComponent(out selfDetection))
		{
			//selfDetection.onAwarenessChanged += OnAwarenessChanged;
		}

		detectionFill.fillAmount = detectionAmount = 0f;
	}

	private void OnDisable()
	{
		if (selfDetection)
		{
			//selfDetection.onAwarenessChanged -= OnAwarenessChanged;
		}
	}

	private void OnAwarenessChanged(float maxValue)
	{
		detectionAmount = maxValue;
	}

	private void LateUpdate()
	{
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