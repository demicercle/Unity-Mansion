using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(fileName = "DecalData", menuName = "DecalData", order = 0)]
public class DecalData : ScriptableObject
{
	private static DecalData[] allDecals;

	public static bool TryGetDecal(Material material, out DecalData decal)
	{
		if (allDecals == null)
			allDecals = Resources.LoadAll<DecalData>(Consts.ResPath_Decals);

		decal = allDecals.FirstOrDefault(decal => decal.renderedMaterials.Contains(material));
		return decal;
	}
	
		public List<Material> renderedMaterials = new List<Material>();
		public GameObject decalPrefab;
}