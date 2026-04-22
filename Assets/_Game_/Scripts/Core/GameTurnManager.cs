using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameTurnManager : MonoBehaviour
{
	static private GameTurnManager _instance;
	static public GameTurnManager Instance => _instance;

	public System.Action<bool> onLoadingGame;
	public System.Action turnStarted, turnEnded;
	
	[SerializeField] private string nextLevelName = "Level_0";
	[SerializeField] private bool firstTurnIsPlayer = true;
	[SerializeField] private bool skipInvisibleCharacters = true;
	[SerializeField] [Range(0f,1f)] private float cameraStartZoom = .5f;
	[Header("Timings")]
	[SerializeField] [Range(0.1f,1f)] private float realTimeTicks = 0.5f;
	[SerializeField] private float postLoadDuration = 1.0f;
	[SerializeField] private float turnChangedSeconds = 3.0f;
	[SerializeField] private  float focusCharacterSeconds = 1.5f;
	[SerializeField] private  float noActionsSeconds = 3.0f;
	[SerializeField] private float defaultActionsSeconds = 1.0f;
	[SerializeField] private  float cameraStartsSeconds = 0.15f;
	[SerializeField] private  float aiThinksSeconds = 0.5f;
	[Space]
	[SerializeField] private GameObject[] startupObjects = new GameObject[0];

	public bool IsTurnBasedMode() => turnBasedMode;
	
	public bool IsPlayerTurn() => playerTurn;

	public bool AnyActiveRoutinesPlaying() => CharacterRoutines.AnyActiveRoutines();
	
	public bool PlayerCanEndTurn() => !AnyActiveRoutinesPlaying() && IsPlayerTurn();
	
	public bool PlayerShouldEndTurn() => PlayerCanEndTurn() 
	                               && partyCharacters.All(pc =>
									{
										return !pc.HasActions() && pc.GetRemainingMovePoints() < 0.5f;
									});
	
	public bool SelectedCharacterCanUseRunAction() =>
		!CharacterRoutines.AnyActiveRoutines() && IsPlayerTurn() 
		                                       && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
		                                       && selectedCharacter.CanUseAction(CharacterAction.RunAction);

	public bool SelectedCharacterIsControllable() => IsPlayerTurn() 
	                                                 && CharacterSelection.TryGetSelectedCharacter(out var selectedCharacter) 
	                                                 && selectedCharacter.IsControllableByPlayer();
	
	public int GetCurrentTurn() => currentTurn;

	public bool IsLoading() => isLoading;

	public void TryEndPlayerTurn()
	{
		if (IsPlayerTurn())
		{
			EndTurn();
		}
		else
		{
			Debug.Log(this + " TryEndPlayerTurn failed: not in player turn");
		}
	}
	
	#region PRIVATE
	private CameraPivot cameraPivot => CameraPivot.Instance;
	private CharacterController[] levelCharacters;
	private int currentTurn;
	private bool playerTurn;
	private bool isLoading;
	private bool turnBasedMode;
	
	private void Awake()
	{
		_instance = this;
		Debug.Log(this + " Awake");
	} 
	
	private IEnumerator Start()
	{
		Debug.Log(this + " Starts");
		
		FadeScreen.FadeIn(true);

		foreach (var go in startupObjects)
		{
			Debug.Log(this + " Instantiate startupObjects " + go);
			Instantiate(go);
		}
		
		var sceneLoader = gameObject.AddComponent<SceneLoader>(); // load level
		sceneLoader.SetSceneToLoad(nextLevelName, LoadSceneMode.Additive);

		isLoading = true;
		Debug.Log(this + " loading...");
		onLoadingGame?.Invoke(true);
		
		do // ensure that everything is properly loaded
		{
			do
			{
				yield return null;
			} while (SceneLoader.AnyInstanceActive);

			yield return new WaitForSeconds(postLoadDuration);
		} while (SceneLoader.AnyInstanceActive);

		while(NavmeshRebuilder.IsRebuildingAsync()) 
			yield return null;
		
		Debug.Log(" loading scene complete");
		
		yield return new WaitForSeconds(0.1f); // for triggers

		levelCharacters = CharacterController.AllCharacters
			.OrderBy(c => c.transform.GetSiblingIndex())
			.ToArray();
		
		Debug.Log(this + " found levelCharacters=" + levelCharacters.Length);
		
		var partyIndex = 0;
		foreach (var character in levelCharacters)
		{
			character.Spawn();
			if(character.CompareTag(Tags.TagPlayer))
				character.SetPartyIndex(partyIndex++);
		}
		
		Debug.Log("loading complete");
		isLoading = false;
		onLoadingGame?.Invoke(false);
		
		FadeScreen.FadeOut();
		yield return new WaitForSeconds(FadeScreen.FadeDuration / 2f);
		
		while (true)
		{
			yield return StartCoroutine(RealTimeModeCoroutine());
			yield return StartCoroutine(TurnModeCoroutine());
		}
	}

	private IEnumerator TurnModeCoroutine()
	{
		Debug.Log(this + " Enters TurnModeCoroutine");
		currentTurn = 0;
		
		while (turnBasedMode)
		{
			if (!IsAnyEnemyAroundCheck())
			{
				turnBasedMode = false;
				Debug.Log(this + " Switch To RealTime");
				break;
			}
			
			
			playerTurn = !firstTurnIsPlayer;
			NewTurn();

			SelectBestPlayerCharacter();
			yield return new WaitForSeconds(focusCharacterSeconds);
			
			while (IsPlayerTurn())
			{
				yield return null;
			}

			CharacterSelection.SelectCharacter(null);
			
			yield return new WaitForSeconds(turnChangedSeconds);
			
			yield return YieldRoutines();
		
			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
			
				if (otherCharacter.IsCharacterRevealed())
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

				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain))
				{
					Debug.LogWarning(otherCharacter + " has no brain");
					yield return new WaitForSeconds(noActionsSeconds);
					continue;
				}

				yield return StartCoroutine(otherBrain.Think());

				var startTime = Time.time;
				yield return YieldRoutines();
				while (Time.time - startTime < aiThinksSeconds)
					yield return null;
			}
		}
	}

	private IEnumerator RealTimeModeCoroutine()
	{
		Debug.Log(this + " Enters RealTimeModeCoroutine");
		
		currentTurn = -1;
		NewTurn();
		
		SelectBestPlayerCharacter();
		CameraPivot.Instance.SetZoom(cameraStartZoom);

		do
		{
			PartyRest(false);

			yield return new WaitForSeconds(realTimeTicks);

			if (IsAnyEnemyAroundCheck())
			{
				turnBasedMode = true;
				Debug.Log(this + " Switch Turn Based Mode");
			}
			
		} while (!turnBasedMode);
	}
	
	private Coroutine YieldRoutines() => StartCoroutine(WaitRoutines());
	private IEnumerator WaitRoutines()
	{
		while(CharacterRoutines.AnyActiveRoutines())
			yield return null;
	}
	
	private void EndTurn()
	{
		playerTurn = false;
		CharacterController.AllCharacters.ForEach((c) =>
		{
			c.EndTurn();
			if (!c.IsPlayer()) c.Rest();
		});
		Debug.Log(this + " EndTurn " + currentTurn);
		turnEnded?.Invoke();
	}
	
	private void NewTurn()
	{
		currentTurn += 1;
		playerTurn = true;
		CharacterController.AllCharacters.ForEach((c) =>
		{
			c.NewTurn(currentTurn);
			if (c.IsPlayer()) c.Rest();
		});
		Debug.Log(this + " NewTurn " + currentTurn);
		if(currentTurn > 0) turnStarted?.Invoke();
	}

	private void SelectBestPlayerCharacter()
	{
		var center = cameraPivot.GetFocusPosition();

		var bestCharacter = partyCharacters
			.OrderBy(pc => Vector3.Distance(pc.GetPosition(), center))
			.FirstOrDefault();
		
		Debug.Log(this + " SelectBestPlayerCharacter " + bestCharacter);
		CharacterSelection.SelectCharacter(bestCharacter);
	}

	private void PartyRest(bool verbose)
	{
		foreach (var character in partyCharacters)
			character.Rest(verbose);
	}

	private IEnumerable<CharacterController> partyCharacters => CharacterController.GetPartyCharacters();

	private bool IsAnyEnemyAroundCheck()
	{
		foreach (var character in levelCharacters)
		{
			if (character.IsEnemyToPlayer() && !character.IsDead() && character.IsCharacterRevealed())
			{
				return true;
			}
		}

		return false;
	}
	
	#endregion
}