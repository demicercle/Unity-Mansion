using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameTurnManager : MonoBehaviour
{
	static private GameTurnManager _instance;

	static public GameTurnManager Instance
	{
		get
		{
			if(!_instance) _instance = Utils.FindFirstObject<GameTurnManager>();
			if(!_instance) Debug.LogError("No GameTurnManager found!");
			return _instance;
		}
	}

	public System.Action turnStarted, turnEnded;
	public System.Action sceneLoaded;

	[SerializeField] private string nextLevelName = "Level_0";
	[SerializeField] private float turnChangedSeconds = 3.0f;
	[SerializeField] private  float focusCharacterSeconds = 1.5f;
	[SerializeField] private  float noActionsSeconds = 3.0f;
	[SerializeField] private float defaultActionsSeconds = 1.0f;
	[SerializeField] private  float cameraStartsSeconds = 0.5f;
	[SerializeField] private  float aiThinksSeconds = 0.5f;
	
	[ReadOnly] [SerializeField] private int currentTurn;
	[ReadOnly] [SerializeField] private bool playerTurn;
	
	public int GetCurrentTurn() => currentTurn;
	public bool IsPlayerTurn() => playerTurn;

	public void EndTurn()
	{
		playerTurn = false;
		CharacterController.instances.ForEach(c => c.EndTurn());//TODO Event
		Debug.Log(this + " EndTurn " + currentTurn);
		turnEnded?.Invoke();
	}

	public void SceneLoaded() => sceneLoaded?.Invoke();
	
	private IEnumerator Start()
	{
		var cameraPivot = CameraPivot.Instance;
		
		var sceneLoader = gameObject.AddComponent<SceneLoader>(); // load level
		sceneLoader.SetSceneToLoad(nextLevelName, LoadSceneMode.Single);

		do
		{
			yield return null;
		} while (SceneLoader.IsLoadingScene);
		
		var levelCharacters = Utils.FindObjects<CharacterController>()
			.OrderBy(c => c.name)
			.ToList();
		
		Debug.Log(this + " found levelCharacters=" + levelCharacters.Count);
		
		var partyIndex = 0;
		foreach (var character in levelCharacters)
		{
			character.Spawn();
			if(character.CompareTag(Consts.TAG_PLAYER))
				character.SetPartyIndex(partyIndex++);
		}
		
		yield return FadeScreen.FadeOut();
		
		while (true)
		{
			NewTurn();
			
			SelectBestPlayerCharacter();
			
			yield return new WaitForSeconds(focusCharacterSeconds);
			
			if(currentTurn == 1)
				CameraPivot.Instance.SetStartZoom();
			
			while (IsPlayerTurn())
			{
				yield return null;
			}

			yield return new WaitForSeconds(turnChangedSeconds);
		
			foreach (var otherCharacter in CharacterController.instances)
			{
				if (otherCharacter.InParty()) continue;
			
				if (otherCharacter.IsCharacterVisible())
					cameraPivot.Focus(otherCharacter);
				else
				{
					Debug.Log("Do not focus invisible character: " + otherCharacter);
					yield return new WaitForSeconds(defaultActionsSeconds);
					continue;
				}

				yield return new WaitForSeconds(focusCharacterSeconds);

				if (!otherCharacter.TryGetAIBrain(out var aiBrain))
				{
					Debug.LogWarning(otherCharacter + " has no aiBrain");
					yield return new WaitForSeconds(noActionsSeconds);
					continue;
				}

				yield return new WaitForSeconds(aiThinksSeconds);
			
				Debug.Log("Think() " + aiBrain + " " + otherCharacter);
				yield return StartCoroutine(aiBrain.Think(otherCharacter));
				Debug.Log("End Think()");

				yield return new WaitForSeconds(aiThinksSeconds);
			}
		}
	}
	
	private void NewTurn()
	{
		currentTurn += 1;
		playerTurn = true;
		CharacterController.instances.ForEach(c => c.NewTurn(currentTurn));//TODO Event
		Debug.Log(this + " NewTurn " + currentTurn);
		turnStarted?.Invoke();
	}

	private void SelectBestPlayerCharacter()
	{
		var cameraPivot = CameraPivot.Instance;
		var center = cameraPivot.GetFocusPosition();

		var bestCharacter = CharacterController.GetPartyCharacters()
			.OrderBy(pc => Vector3.Distance(pc.GetPosition(), center))
			.FirstOrDefault();
		
		Debug.Log(this + " SelectBestPlayerCharacter " + bestCharacter);
		CharacterSelection.SelectCharacter(bestCharacter);
	}
}