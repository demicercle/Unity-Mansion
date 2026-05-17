using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class OpenEditorScenes
{
	private const string SCENE_PATH = "Assets/_Game_/Scenes/";
	
	[MenuItem("[Scenes]/StartScene")] static void OpenScenes_StartScene() => OpenScenes("StartScene");
	[MenuItem("[Scenes]/GameScene")] static void OpenScenes_GameScene() => OpenScenes("GameScene");

	[MenuItem("[Scenes]/Levels/Level_0")] static void OpenScenes_Levels_Level_0() => OpenLevel(0);
	[MenuItem("[Scenes]/Levels/Level_1")] static void OpenScenes_Levels_Level_1() => OpenLevel(1);


	private static void OpenLevel(int index) => OpenLevel(LevelData.GetLevel(index));
	private static void OpenLevel(LevelData levelData)
	{
		OpenScenes(new string[]{"GameScene"}.Concat(levelData.scenesToLoad).ToArray());
	}
	private static void OpenScenes(params string[] scenes)
	{
		for (int i = 0; i < scenes.Length; i++)
		{
			EditorSceneManager.OpenScene(SCENE_PATH + scenes[i] + ".unity", i == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
		}

		var level = Utils.FindFirstObject<LevelComponent>();
		if(level) Selection.activeGameObject = level.gameObject;
	}
}