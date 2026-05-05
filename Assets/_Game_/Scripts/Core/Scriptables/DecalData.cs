using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(fileName = "DecalData", menuName = "DecalData", order = 0)]
public class DecalData : ScriptableObject
{
	public static bool TryGetDecal(Material material, DecalData[] decals, out DecalData outData)
	{
		outData = decals.FirstOrDefault(decal => decal && material && decal.renderedMaterials.Contains(material));
		return outData;
	}
	
		public List<Material> renderedMaterials = new List<Material>();
		public GameObject decalPrefab;
}