using System;
using UnityEditor;

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
		}
	}
}