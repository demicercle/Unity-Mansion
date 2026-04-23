using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameTurnManager : MonoBehaviour
{
	static private GameTurnManager _instance;
	static public GameTurnManager Instance => _instance;

	public System.Action<bool> onLoadingGame, onPlayerChanged;
	public System.Action onTurnStarted, onTurnEnded;
	
	[SerializeField] private string nextLevelName = "Level_0";
	[SerializeField] private bool firstTurnIsPlayer = true;
	[SerializeField] private bool skipInvisibleCharacters = true;
	[SerializeField] [Range(0f,1f)] private float cameraStartZoom = .5f;
	[Header("Timings")]
	[SerializeField] [Range(0.3f,3f)] private float realTimeTicks = 0.5f;
	[SerializeField] [Min(0.1f)] private float postLoadDuration = 1.0f;
	[SerializeField] [Min(0.1f)] private float turnChangedSeconds = 3.0f;
	[SerializeField] [Min(0.1f)]  private  float focusCharacterSeconds = 1.5f;
	[SerializeField] [Min(0.1f)] private  float noActionsSeconds = 3.0f;
	[SerializeField] [Min(0.1f)] private float defaultActionsSeconds = 1.0f;
	[SerializeField] [Min(0.1f)] private  float firstTurnSeconds = 1.0f;
	[SerializeField] [Min(0.1f)] private  float aiThinksSeconds = 0.5f;
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
		                                       && selectedCharacter.CanUseAction(CharacterAction.Run);

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
	private IEnumerable<CharacterController> partyCharacters => CharacterController.GetPartyCharacters();
	
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

		foreach (var go in startupObjects)
		{
			Debug.Log(this + " Instantiate startupObjects " + go);
			Instantiate(go);
		}
		
		FadeScreen.FadeIn(true);
		
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

		Debug.Log(" wait NavmeshRebuilder(s)");
		do yield return null;	while (NavmeshRebuilder.IsRebuildingAsync()); // wait rebuild
		
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
			yield return StartCoroutine(RealTimeCoroutine());
			yield return StartCoroutine(TurnBasedCoroutine());
			yield return null; // safety for loop
		}
	}

	private IEnumerator TurnBasedCoroutine()
	{
		Debug.Log(this + " Enters TurnBasedCoroutine");
		currentTurn = 0;
		
		while (turnBasedMode)
		{
			if (!IsAnyEnemyAroundCheck())
			{
				turnBasedMode = false;
				Debug.Log(this + " Switch To RealTime");
				break;
			}

			if (currentTurn == 0)
			{
				if (!playerTurn && firstTurnIsPlayer) // probably useless unless GD ("initiative")
				{
					playerTurn = true;
					onPlayerChanged?.Invoke(true);
				}
			}
			
			NewTurn();
			
			if (currentTurn == 1) // it is the first turn
			{
				foreach (var otherCharacter in levelCharacters)
				{
					if (otherCharacter.InParty()) continue;
					otherCharacter.StopAgent();
					otherCharacter.Routines.StopAllRoutines();
					otherCharacter.Rest();
					
					if (otherCharacter.TryGetComponent(out CharacterBrain otherBrain))
						otherBrain.StopThinking();

					if (otherCharacter.TryGetAnimation(out CharacterAnimation otherAnimation))
						otherAnimation.TrySwitchWeapons(true);
				}
				
				foreach (var partyCharacter in partyCharacters) // stop agent and draw weapons
				{
					partyCharacter.StopAgent();
					partyCharacter.Routines.StopAllRoutines();
					partyCharacter.Rest();

					if (partyCharacter.TryGetAnimation(out var anim))
						anim.TrySwitchWeapons(true);
				}
				
				Debug.Log(this + " first turn setup, wait weapon switch");
				do
				{
					yield return null;
				} while (levelCharacters.Any(c => c.TryGetAnimation(out var anim) && anim.IsSwitchingWeapons));
				
				yield return new WaitForSeconds(firstTurnSeconds); 
			}
			else // it is NOT the first turn
			{
				SelectBestPlayerCharacter();
				yield return new WaitForSeconds(focusCharacterSeconds); 
			}
			
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
				
				otherCharacter.Rest(); // rest not player character

				if (skipInvisibleCharacters && !otherCharacter.IsCharacterRevealed())
				{
					Debug.Log("skipInvisibleCharacters: " + otherCharacter);
					yield return new WaitForSeconds(defaultActionsSeconds);
					continue;
				}
				
				cameraPivot.Focus(otherCharacter);
				//TODO "cameraPivot.Lock(true)"
				yield return new WaitForSeconds(focusCharacterSeconds);
				//TODO "cameraPivot.Lock(false)" ? if player can look around while another character is acting

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

	private IEnumerator RealTimeCoroutine()
	{
		Debug.Log(this + " Enters RealTimeCoroutine");
		
		currentTurn = -1;
		NewTurn();
		
		SelectBestPlayerCharacter();
		CameraPivot.Instance.SetZoom(cameraStartZoom);

		do
		{
			PartyRest(false);

			foreach (var otherCharacter in levelCharacters)
			{
				if (otherCharacter.InParty()) continue;
				if (otherCharacter.IsDead()) continue;
				otherCharacter.Rest(false);
				if (otherCharacter.Routines.AnyActive()) continue;
				if (!otherCharacter.TryGetComponent(out CharacterBrain otherBrain)) continue;
				
				if (!otherBrain.IsThinking)
					otherBrain.StartThink();
			}

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
		onPlayerChanged?.Invoke(false);
		PartyRest(true);
		Debug.Log(this + " EndTurn " + currentTurn);
		onTurnEnded?.Invoke();
	}
	
	private void NewTurn()
	{
		currentTurn += 1;
		playerTurn = true;
		onPlayerChanged?.Invoke(true);
		
		Debug.Log(this + " NewTurn " + currentTurn);
		
		if (currentTurn > 0)
		{
			onTurnStarted?.Invoke();
		}
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

	private bool IsAnyEnemyAroundCheck()
	{
		foreach (var enemyCharacter in levelCharacters)
		{
			if (!enemyCharacter.IsEnemyToPlayer()) continue;
			if (!enemyCharacter.IsDead() && enemyCharacter.IsCharacterRevealed())
			{
				foreach (var partyCharacter in partyCharacters)
				{
					if (enemyCharacter.HasDetectedOther(partyCharacter))
					{
						return true;
					}
				}
			}
		}

		return false;
	}
	
	#endregion
}