using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


public class GameLog : MonoBehaviour
{
	public static GameLog Instance { get; private set; }
	
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
		Instance = this;
		if(!linePrefab) Debug.LogError(this + " has no line prefab");
		linePrefab.gameObject.SetActive(false);
	}

	private void OnEnable()
	{
		gameTurnManager.onTurnEnded += TurnEnded;
		gameTurnManager.onTurnStarted += TurnStarted;
		CharacterEntity.onAnyCharacterUsesAction += OnAnyCharacterUsesAction;
		CharacterEntity.onAddedToParty += CharacterAddedToParty;
		CharacterEntity.onRemovedFromParty += CharacterRemovedFromParty;
		CharacterEntity.onAnyCharacterDamaged += OnAnyCharacterDamaged;
		CharacterEntity.onAnyCharacterHealed += OnAnyCharacterHealed;
		CharacterEntity.onAttackResult += OnAttackResult;
		Inventory.onAnyItemAdded += OnItemAdded;
		Inventory.onAnyItemRemoved += OnItemRemoved;
	}

	private void OnDisable()
	{
		gameTurnManager.onTurnEnded -= TurnEnded;
		gameTurnManager.onTurnStarted -= TurnStarted;
		CharacterEntity.onAnyCharacterUsesAction -= OnAnyCharacterUsesAction;
		CharacterEntity.onAddedToParty -= CharacterAddedToParty;
		CharacterEntity.onRemovedFromParty -= CharacterRemovedFromParty;
		CharacterEntity.onAnyCharacterDamaged -= OnAnyCharacterDamaged;
		CharacterEntity.onAnyCharacterHealed -= OnAnyCharacterHealed;
		CharacterEntity.onAttackResult -= OnAttackResult;
		Inventory.onAnyItemAdded -= OnItemAdded;
		Inventory.onAnyItemRemoved -= OnItemRemoved;

		Clear();
	}

	private void OnAttackResult(CharacterEntity arg1, IAttackable arg2, CharacterEntity.AttackResult arg3)
	{
		 if (arg2 is CharacterEntity)
		{
			AppendLine(GetLocalizedLine(18, "<CHARACTER> attacks <TARGET>: <RESULT>")
				.Replace("<CHARACTER>", arg1.GetDisplayName())
				.Replace("<TARGET>", (arg2 as CharacterEntity).GetDisplayName())
				.Replace("<RESULT>", LanguageSettings.Main.AttackResult(arg3)));
		}
	}

	private void CharacterAddedToParty(CharacterEntity character)
	{
		//TODO
	}

	private void CharacterRemovedFromParty(CharacterEntity character)
	{
		//TODO
	}

	private void OnItemAdded(Inventory inventory, ItemData item)
	{
		AppendLine(GetLocalizedLine(13, "<CHARACTER> receives item <ITEM>")
			.Replace("<CHARACTER>", inventory.TryGetComponent(out CharacterEntity character) ? character.GetDisplayName() : "???")
			.Replace("<ITEM>", item.GetDisplayName()));
	}

	private void OnItemRemoved(Inventory inventory, ItemData item)
	{
		AppendLine(GetLocalizedLine(14, "<CHARACTER> looses item <ITEM>")
			.Replace("<CHARACTER>", inventory.TryGetComponent(out CharacterEntity character) ? character.GetDisplayName() : "???")
			.Replace("<ITEM>", item.GetDisplayName()));
	}

	private void TurnStarted()
	{
//		AppendLine(GetLocalizedLine(10, "Turn Started"));
	}

	private void TurnEnded()
	{
//		AppendLine(GetLocalizedLine(11, "Turn Ended"));
	}

	private void OnAnyCharacterUsesAction(CharacterEntity character, CharacterAction action)
	{
		AppendLine(GetLocalizedLine(16, "<CHARACTER> uses action <ACTION>")
			.Replace("<CHARACTER>", character.GetDisplayName())
			.Replace("<ACTION>", action.ToString())); // TODO Localization "CharacterAction"
	}

	private void OnAnyCharacterDamaged(CharacterEntity character, DamageInfo damageInfo, object objectFrom)
	{
		AppendLine(GetLocalizedLine(15, "<CHARACTER> receives <DAMAGE> damage(s)")
			.Replace("<CHARACTER>", character.GetDisplayName())
			.Replace("<DAMAGE>", Mathf.CeilToInt(damageInfo.amount).ToString()));
	}

	private void OnAnyCharacterHealed(CharacterEntity character, float amount)
	{
		AppendLine(GetLocalizedLine(17, "<CHARACTER> healed: <AMOUNT>")
			.Replace("<CHARACTER>", character.GetDisplayName())
			.Replace("<AMOUNT>", Mathf.CeilToInt(amount).ToString()));
	}

	private string GetLocalizedLine(int index, string fallback) =>
		LanguageSettings.GetDialogLines(DialogFiles.gamelog).GetLine(index, fallback);

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