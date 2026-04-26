using System;
using UnityEditor;

public class PostprocessTextures : AssetPostprocessor
{
	public static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
		string[] movedFromAssetPaths)
	{
			
	}

	private void OnPreprocessAsset()
	{
		TextureImporter texImporter = assetImporter as TextureImporter;
		if (texImporter)
		{
			if (assetPath.Contains("Generic Items"))
			{
				texImporter.alphaIsTransparency = true;
			}
		}
	}
}