using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class GameLog : MonoBehaviour
{
	[SerializeField] private TMP_Text linePrefab;
	[SerializeField] private bool noLog;
	[SerializeField] [Min(-1)] private int maxLines = 3;
	//[SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0,1,1,0);
	[SerializeField] [Range(0.1f, 10)] private float fadeDuration = 1f;
	
	public void AppendLine(string text)
	{
		if (maxLines == 0) return;
		while (maxLines > 0 && lines.Count + 1 > maxLines)
		{
			Destroy(lines[^1].gameObject);
			lines.RemoveAt(lines.Count-1);
		}
		
		lines.Insert(0, Instantiate(linePrefab.gameObject, linePrefab.transform.parent).GetComponent<TMP_Text>());
		lines[0].text = text;
		lines[0].gameObject.SetActive(true);
		
		if(!noLog) Debug.Log("GameLog> <b>" + text + "</b>");
	}
	
	private GameTurnManager gameTurnManager => GameTurnManager.Instance;
	
	private List<TMPro.TMP_Text> lines = new List<TMPro.TMP_Text>();

	private void Awake()
	{
		if(!linePrefab) Debug.LogError(this + " has no line prefab");
		linePrefab.gameObject.SetActive(false);
	}

	private void OnEnable()
	{
		gameTurnManager.onTurnEnded += TurnEnded;
		gameTurnManager.onTurnStarted += TurnStarted;
		CharacterController.onCharacterUsesAction += OnCharacterUsesAction;
	}

	private void OnDisable()
	{
		gameTurnManager.onTurnEnded -= TurnEnded;
		gameTurnManager.onTurnStarted -= TurnStarted;
		CharacterController.onCharacterUsesAction -= OnCharacterUsesAction;

		Clear();
	}

	private void TurnStarted()
	{
		AppendLine("Turn Started");
	}

	private void TurnEnded()
	{
		AppendLine("Turn Ended");
	}

	private void OnCharacterUsesAction(CharacterController character, CharacterAction action)
	{
		AppendLine(character.GetDisplayName() + " uses action " + action);
	}

	private void Clear()
	{
		while (lines.Count > 0)
		{
			Destroy(lines[0].gameObject);
			lines.RemoveAt(0);
		}
	}

	private void LateUpdate()
	{
		int i = 0;
		while (i < lines.Count)
		{
			lines[i].color = Utils.ColorAlpha(lines[i].color, lines[i].color.a - Time.deltaTime / fadeDuration);
			if (lines[i].color.a <= 0)
			{
				Destroy(lines[i].gameObject);
				lines.RemoveAt(i);
				continue;
			}

			i++;
		}
	}
}