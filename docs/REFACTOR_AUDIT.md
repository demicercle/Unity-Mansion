# Audit & propositions de refactoring — Unity-Mansion

Branche de travail : `dev-level-1-retake` (commit `86537972`).
Périmètre audité : `Assets/_Game_/Scripts` (~99 scripts C#, ~14 500 lignes rien que dans `Core/`).

## 1. Vue d'ensemble

Le projet est un RPG temps réel / tour par tour avec :

- un système de dés maison (`DicePool`) : le prochain d20 est visible, relançable contre un point de **Karma** (`DiceLauncher.AllowedDiceRolls`), le karma se recharge sur les *miss* (`MissCount`) et sur les *last hits* ;
- un gestionnaire de tours (`GameTurnManager`) qui fait la navette entre mode temps réel et mode tour par tour via deux grosses coroutines (`RealTimeCoroutine` / `TurnBasedCoroutine`) ;
- des personnages = `CharacterEntity` (1 483 lignes) + `CharacterAgent` (AIPath/RVO d'AStar) + `CharacterBrain` (IA) + `CharacterRoutines` (coroutines d'actions) + `CharacterDetection` ;
- inventaire / loot (`Inventory`, `ItemData`) et sauvegarde JSON (`GameSave`).

Le code fonctionne, mais il est devenu « touffu » pour des raisons structurelles précises, listées ci-dessous.

## 2. Bugs concrets et fragilités d'exécution

1. **`GameTurnManager.IsCurrentCharacter` — bug de précédence** (`GameTurnManager.cs:868`) :
   `currentAICharacter == CharacterSelection.TryGetSelectedCharacter(out var selected) && selected == entity ...`
   Grâce à l'opérateur implicite `bool` de `UnityEngine.Object`, cette ligne compile comme
   `(bool)currentAICharacter == TryGetSelectedCharacter(...)`, et non comme la comparaison voulue.
   Résultat : en tour joueur, `IsCurrentCharacter` renvoie pratiquement toujours `false`
   (puisque `currentAICharacter` est null), ce qui fausse `CharacterAgent.UpdateObstacle()`.
   **Corrigé sur cette branche.**

2. **`CharacterEntity.TryGetLastDamageInstigator`** (`CharacterEntity.cs:265`) renvoie
   `lastAttackTarget` (la dernière cible *attaquée* par le perso) au lieu de
   `lastDamageInstigator` (dernier agresseur, renseigné dans `TakeDamage`).
   Conséquence : le karma de « last hit » (`DiceLauncher.OnAnyCharacterDead`) est crédité
   au mauvais personnage quand un PJ meurt après avoir attaqué quelqu'un.
   **Corrigé sur cette branche.**

3. **Ordre d'initialisation des singletons non garanti** : `CharacterEntity.OnEnable`,
   `DiceLauncher.Awake`, `CharacterAgent.Awake`, `CharacterPortrait.Awake`,
   `GameEventTrigger.Awake` utilisent `GameTurnManager.Instance` sans garde.
   Si l'ordre d'exécution des scripts change (nouvelle scène, prefab ajouté en premier),
   → `NullReferenceException`. **Corrigé en partie** via `[DefaultExecutionOrder(-100)]`
   sur `GameTurnManager` ; la cible propre est un service locator (voir §4).

4. **`DicePool` non déterministe par endroits** (`DicePool.cs:25,41`) :
   `RollDice()` (jets de l'IA / fallback) utilise `UnityEngine.Random` alors que les jets
   joueur passent par le RNG seedé (`rng`) — or la seed est sauvegardée (`GameSave.gameSeed`).
   Les jets IA ne sont donc pas rejouables à l'identique. De plus `GetRandomSeed()` concatène
   minute+seconde+milliseconde en string (`"1"+"2"+"3"` → 123), avec collisions fréquentes.
   **Corrigé sur cette branche** (RNG seedé partout + seed basée sur `DateTime.Ticks`).

5. **Coroutines/Invoke par nom de string** : `StartCoroutine("MainCoroutine")`,
   `StopCoroutine("MainCoroutine")` (`GameTurnManager.cs:165,205,321`),
   `StartCoroutine("Animate")` (`DiceLauncher.cs:108`), `Invoke("ClearActions", ...)`, etc.
   Un renommage casse silencieusement l'exécution. **Partiellement corrigé** (handles de
   coroutine pour `MainCoroutine` et `Animate`) ; le reste est à faire (voir §4).

6. **`GameTurnManager.FirstTurn`** (`GameTurnManager.cs:602`) : tri LINQ sur une clé string
   `faction + "_" + interval.ToString("N3") + "_" + name` — comparaison lexicographique
   dépendante de la culture, fausse dès que l'intervalle dépasse 10 s (`"10.000" < "9.999"`).
   **Corrigé sur cette branche** (tri par tuple `OrderBy/ThenBy`).

7. **`Inventory.DeserializeFromSlots`** (`Inventory.cs:73`) : `items[i] = newItem` sans
   vérifier `i < items.Count` → `IndexOutOfRangeException` si une sauvegarde contient plus
   de slots que le prefab. **Corrigé sur cette branche.**

8. **`CharacterAgent.DrawPath`** (`CharacterAgent.cs:546`) : accède à `pathRenderers[0]` et
   `[1]` sans vérifier la taille de la liste (alors que `ClearPath` null-check chaque élément)
   → crash si le prefab est mal configuré. **Corrigé sur cette branche.**

9. **Sauvegarde** (`GameSave.cs`) : `JsonUtility` + `File` sans `try/catch`, champ `version`
   (`LastGameVersion = 2`) jamais vérifié au chargement → une vieille save peut être lue
   silencieusement de travers. Le Karma est persisté (`SavedValue.Karma`) mais pas
   `MissCount` (commenté) → jauge de miss incohérente après chargement.

10. **État global statique tenace entre les scènes** : `CharacterEntity._allCharacters`,
    `_partyCharacters`, `Inventory.allInventories`, `RoomTrigger.allRooms/activeRooms`,
    `CharacterRoutines.activeInstances`, ~15 événements `static`, plus une dizaine de
    `public static int debugMode { get; set; }`. Tout dépend de `OnDestroy` pour nettoyer ;
    avec le domain reload désactivé (itération rapide), tout ce bazar persiste et fuit.

## 3. Problèmes d'architecture

1. **Aucun namespace, aucun asmdef** pour le code jeu : les ~99 scripts vivent dans
   l'`Assembly-CSharp` global (les seuls asmdefs sont ceux des plugins). C'est la première
   ligne du README (« REFACTO - add namespaces »). Conséquences : collisions de noms
   possibles (`Inventory`, `Utils`, `NodeDelay`...), compilation lente, **tests unitaires
   impossibles**.

2. **Tout est statique** : 136 références aux seuls singletons `*.Instance`
   (`GameTurnManager`, `GameInput`, `GameCanvas`, `CameraPivot`, `PauseCanvas`, `GameLog`)
   réparties dans ~30 fichiers ; `GameTurnManager` seul est touché depuis 30 fichiers.
   `DicePool` est 100 % statique, `CharacterSelection` est une classe statique avec état,
   la logique de règle du **Karma vit dans la classe d'UI** `DiceLauncher`
   (`AllowedDiceRolls`/`MissCount` statiques, persistés via `GameSave` qui lit
   directement `DiceLauncher`). Impossible à tester, ordre d'init fragile, couplage caché.

3. **God classes** : `CharacterEntity` (1 483 lignes, ~20 événements, registres statiques,
   combat, visée, LOS, faction, party, hooks de save), `GameInput` (1 361 lignes : input +
   raycast curseur + caméra), `GameTurnManager` (872 lignes : chargement de scènes +
   machine à états de tours + OnGUI de debug), `CharacterRoutines` (1 282 lignes de
   coroutines), `CharacterBrain` (784 lignes).

4. **La règle « prochain d20 visible + relance contre karma » est éparpillée** dans
   `DicePool` (état du dé), `DiceLauncher` (karma/miss, UI), `CharacterEntity.RollDice()`
   (consomme `LastResult` puis relance), `GameTurnManager` (relance au début/fin de tour),
   `GameCanvas` (aperçu de touche avec `LastResult`), `GameSave` (persistance du karma).
   Aucun propriétaire clair de la règle.

5. **Coroutines "fire and forget"** : `StopAllCoroutines()` disséminés (`CharacterBrain`,
   `CharacterAgent`, `CharacterEntity`), attentes par `yield return null` en boucle
   (`WaitRoutines`, `UpdateDetection` tourne **à chaque frame** pour chaque PNJ en
   itérant `AllCharacters` — O(n²) par frame), `Invoke`/`InvokeRepeating` par string.

6. **Perf / allocations dans les boucles d'Update** : `GameCanvas.gameLog` est un
   `GetComponentInChildren<GameLog>()` à chaque appel ; `GetPartyCharacters()` alloue un
   tableau à chaque appel (16 appelants, dont des chemins UI/frame) ; `OnGUI` de debug
   embarqué dans `GameTurnManager`, `CharacterBrain`, `CharacterAgent`, `CharacterEntity`.

7. **RobustesseDivers** : `CharacterEntity.Awake` crée des composants à la volée
   (`AddComponent<Inventory>()`, `WeaponData.CreateInstance<>()`) ce qui masque les prefabs
   mal configurés ; `CharacterEntity.Start` fait `gameObject.SetActive(false)` en attendant
   `Spawn()` — course possible entre le `Start` du personnage et le `Spawn()` appelé par
   `GameTurnManager` ; `using` inutilisés (`System.Net`, `Unity.VisualScripting` dans
   `CharacterBrain`...) ; 54 TODO/FIXME en suspens.

## 4. Propositions de refactoring (par paliers)

### P0 — quick wins (faits sur cette branche, sans changement d'architecture)
- Corriger `IsCurrentCharacter`, `TryGetLastDamageInstigator`, le tri de `FirstTurn`,
  la borne de `DeserializeFromSlots`, le guard de `DrawPath`.
- `[DefaultExecutionOrder(-100)]` sur `GameTurnManager` ; remplacer les coroutines
  string-based critiques par des handles ; rendre `DicePool` déterministe (RNG seedé).

### P1 — structure (prochaine étape recommandée)
1. **Namespaces + asmdefs** : `Mansion.Core`, `Mansion.Characters`, `Mansion.UI`,
   `Mansion.Triggers`, `Mansion.Editor`, `Mansion.Tests`. C'est le prérequis de tout le
   reste (et des tests).
2. **Un point d'entrée unique** (`GameBootstrapper` + `GameContext`) qui enregistre les
   services (`TurnManager`, `Input`, `Canvas`, `DicePool`...) dans un locator null-safe,
   au lieu de 6 `Instance` éparpillés. Les abonnements dans `Awake/OnEnable` deviennent
   sûrs.
3. **Sortir la règle Karma/D20 de l'UI** : un `KarmaSystem` (ou `DiceRules`) runtime,
   instance-based, avec événements `onKarmaChanged` / `onNextDiceChanged` ; `DiceLauncher`
   devient une vue bête ; `GameSave` sérialise le système, pas la classe d'UI.
4. **Découper `CharacterEntity`** en `partial` d'abord (Stats / Combat / Faction / Party),
   puis en composants (`CharacterStats`, `CharacterCombat`) ; déplacer les champs
   sérialisés de gameplay (`rollDiceToDodge`, `visionRange`...) dans `CharacterData`.
5. **Registre de personnages** (`CharacterRegistry`) à la place des statiques de
   `CharacterEntity` ; bus d'événements centralisé (`GameEvents`) au lieu d'événements
   statiques dispersés.
6. **Machine à états explicite** pour les tours (`enum TurnState { Loading, RealTime,
   PlayerTurn, AITurn }`) à la place des deux coroutines imbriquées + `waitTime` global.
7. **Remplacer `StopAllCoroutines`** par un runner de routines avec handle/annulation
   (`RoutineHandle`), et cadencer `CharacterDetection` (tous les N frames, early-out).
8. **Sauvegarde** : check de `version`, `try/catch` + log, migration, persistance du
   `MissCount`, sérialisation du karma via le système de règles.

### P2 — long terme
- Tests unitaires NUnit (dés/karma, inventaire, sauvegarde) rendus possibles par les
  asmdefs ; pooling des `FloatingText`/projectiles ; déplacer les `OnGUI` de debug dans un
  overlay dédié ; raycasts curseur uniquement en `FixedUpdate` (déjà noté dans le README).

## 5. Ordre conseillé

P0 (cette branche) → P1.1 (namespaces/asmdefs, mécanique pure) → P1.2 (GameContext) →
P1.3 (KarmaSystem) → P1.4/P1.5 (découpage CharacterEntity + registres) → P1.6 (state
machine des tours) → P1.7/P1.8 (routines, save) → P2 (tests).
