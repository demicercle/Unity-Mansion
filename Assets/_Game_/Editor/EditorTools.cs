using UnityEditor;
using UnityEngine;

public static class EditorTools
{
	[MenuItem("Tools/RevealInFinder/persistentDataPath")]
	private static void RevealInFinder_persistentDataPath()
	{
		EditorUtility.RevealInFinder(Application.persistentDataPath);
	}
	[MenuItem("Tools/RevealInFinder/consoleLogPath")]
	private static void RevealInFinder_consoleLogPath()
	{
		EditorUtility.RevealInFinder(Application.consoleLogPath);
	}
	[MenuItem("Tools/RevealInFinder/dataPath")]
	private static void RevealInFinder_dataPath()
	{
		EditorUtility.RevealInFinder(Application.dataPath);
	}
	[MenuItem("Tools/RevealInFinder/streamingAssetsPath")]
	private static void RevealInFinder_streamingAssetsPath()
	{
		EditorUtility.RevealInFinder(Application.streamingAssetsPath);
	}
	[MenuItem("Tools/RevealInFinder/temporaryCachePath")]
	private static void RevealInFinder_temporaryCachePath()
	{
		EditorUtility.RevealInFinder(Application.temporaryCachePath);
	}
}