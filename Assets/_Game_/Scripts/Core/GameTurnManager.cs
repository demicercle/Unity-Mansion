using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameTurnManager : MonoBehaviour
{
	static private GameTurnManager _instance;
	static public GameTurnManager Instance => _instance;

	public System.Action turnStarted, turnEnded;
	public System.Action sceneLoaded;
	
	[Header("Turn Settings")]
	[SerializeField] private string nextLevelName = "Level_0";
	[SerializeField] private bool firstTurnIsPlayer = true;
	[SerializeField] private bool skipInvisibleCharacters = true;
	
	[Header("Timings")]
	[SerializeField] private float postLoadDuration = 1.0f;
	[SerializeField] private float turnChangedSeconds = 3.0f;
	[SerializeField] private  float focusCharacterSeconds = 1.5f;
	[SerializeField] private  float noActionsSeconds = 3.0f;
	[SerializeField] private float defaultActionsSeconds = 1.0f;
	[SerializeField] private  float cameraStartsSeconds = 0.15f;
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
	
	#region PRIVATE
	private CameraPivot cameraPivot => CameraPivot.Instance;
	
	private void Awake()
	{
		_instance = this;
		Debug.Log(this + " Awake");
	} 
	
	private IEnumerator Start()
	{
		Debug.Log(this + " Starts");

		FadeScreen.FadeIn(true);
		
		var sceneLoader = gameObject.AddComponent<SceneLoader>(); // load level
		sceneLoader.SetSceneToLoad(nextLevelName, LoadSceneMode.Additive);

		do // ensure that scenes are properly loaded
		{
			do
			{
				yield return null;
			} while (SceneLoader.IsLoadingScene);

			yield return new WaitForSeconds(postLoadDuration);
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
		
		FadeScreen.FadeOut();
		yield return new WaitForSeconds(FadeScreen.FadeDuration / 2f);
		
		while (true)
		{
			playerTurn = !firstTurnIsPlayer;
			NewTurn();
			
			SelectBestPlayerCharacter();
			
			if (currentTurn == 1) // first turn animation
			{
				yield return new WaitForSeconds(cameraStartsSeconds);
				CameraPivot.Instance.SetStartZoom();
			}
			else 
			{
				yield return new WaitForSeconds(focusCharacterSeconds);
			}
			
			while (IsPlayerTurn())
			{
				yield return null;
			}

			yield return new WaitForSeconds(turnChangedSeconds);
			
			yield return YieldRoutines();
		
			foreach (var otherCharacter in CharacterController.instances)
			{
				if (otherCharacter.InParty()) continue;
			
				if (otherCharacter.IsCharacterVisible())
					cameraPivot.Focus(otherCharacter);
				else
				{
					if (skipInvisibleCharacters)
					{
						Debug.Log("skipInvisibleCharacters: " + otherCharacter);
						yield return new WaitForSeconds(defaultActionsSeconds);
					}
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

				var startTime = Time.time;
				yield return YieldRoutines();
				while (Time.time - startTime < aiThinksSeconds)
					yield return null;
			}
		}
	}

	private Coroutine YieldRoutines() => StartCoroutine(WaitRoutines());
	private IEnumerator WaitRoutines()
	{
		while(CharacterRoutines.AnyActiveRoutines())
			yield return null;
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
		var center = cameraPivot.GetFocusPosition();

		var bestCharacter = CharacterController.GetPartyCharacters()
			.OrderBy(pc => Vector3.Distance(pc.GetPosition(), center))
			.FirstOrDefault();
		
		Debug.Log(this + " SelectBestPlayerCharacter " + bestCharacter);
		CharacterSelection.SelectCharacter(bestCharacter);
	}
	#endregion
}