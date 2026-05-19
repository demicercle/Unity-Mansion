using System;
using UnityEditor;
using UnityEngine;

public class PostprocessModels : AssetPostprocessor
{
	public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
		string[] movedFromAssetPaths)
	{
			
	}

	private void OnPreprocessAsset()
	{
		ModelImporter modelImporter = assetImporter as ModelImporter;
		if (modelImporter)
		{
			if (assetPath.Contains("PolygonFantasyHeroCharacters"))
				modelImporter.globalScale = 0.025f;

			/*if (assetPath.Contains("RPG Character") && assetPath.Contains("Death1"))
			{
				foreach (var clip in modelImporter.clipAnimations)
				{
					clip.events = new[]
					{
						new AnimationEvent()
						{
							time = 0.1f,
							functionName = "Dead"
						}
					};
					Debug.Log(this + " OnPreprocessAsset " + assetPath + " " + clip.name + " events=" + Utils.ArrayStr(clip.events));
				}
			}*/
		}
	}
}