using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FogCubeCombiner : MonoBehaviour
{
    [Tooltip("Si activé, les faces intérieures (face à face) seront supprimées.")]
    public bool removeInnerFaces = true;

    [Tooltip("Seuil d'angle pour considérer deux faces comme 'face à face' (en degrés).")]
    public float normalAngleThreshold = 5f; // 5 degrés

    [Tooltip("Seuil de distance pour considérer deux faces comme 'collées'.")]
    public float distanceThreshold = 0.01f; // 1 cm

    [SerializeField] private MeshFilter[] meshFilters;

    [ContextMenu("Collect Meshes")]
    public void CollectMeshes()
    {
        meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
        Utils.SetDirty(this);
    }
    
    void Start()
    {
        CombineMeshes();
    }

    [ContextMenu("Combiner les Meshes")]
    public void CombineMeshes()
    {
        // 1. Récupère tous les meshes enfants
        if (meshFilters.Length == 0)
        {
            return;
        }

        // 2. Combine tous les meshes en un seul
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combineInstances[i].mesh = meshFilters[i].sharedMesh;
            combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false); // Désactive les enfants
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances, true, true);

        // 3. Supprime les faces intérieures si activé
        if (removeInnerFaces)
        {
            combinedMesh = RemoveInnerFaces(combinedMesh);
        }

        // 4. Applique le mesh combiné à cet objet
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = combinedMesh;

        // 5. Met à jour le MeshRenderer
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;
        
        Utils.SetDirty(this);
    }

    private Mesh RemoveInnerFaces(Mesh mesh)
    {
        // 1. Récupère les triangles et les normales
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        // 2. Crée une liste pour stocker les triangles à garder
        List<int> keptTriangles = new List<int>();

        // 3. Parcourt tous les triangles par paires
        for (int i = 0; i < triangles.Length; i += 3)
        {
            bool isInnerFace = false;

            // Récupère les 3 sommets et la normale du triangle actuel
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];
            Vector3 normal1 = normals[triangles[i]];

            // 4. Compare avec tous les autres triangles
            for (int j = 0; j < triangles.Length; j += 3)
            {
                if (i == j) continue; // Évite de comparer avec lui-même

                Vector3 u1 = vertices[triangles[j]];
                Vector3 u2 = vertices[triangles[j + 1]];
                Vector3 u3 = vertices[triangles[j + 2]];
                Vector3 normal2 = normals[triangles[j]];

                // 5. Vérifie si les triangles sont "face à face"
                if (AreFacesFacingEachOther(v1, v2, v3, u1, u2, u3, normal1, normal2))
                {
                    isInnerFace = true;
                    break;
                }
            }

            // 6. Si ce n'est pas une face intérieure, on la garde
            if (!isInnerFace)
            {
                keptTriangles.Add(triangles[i]);
                keptTriangles.Add(triangles[i + 1]);
                keptTriangles.Add(triangles[i + 2]);
            }
        }

        // 7. Crée un nouveau mesh avec les triangles restants
        Mesh cleanedMesh = new Mesh();
        cleanedMesh.vertices = vertices;
        cleanedMesh.normals = normals;
        cleanedMesh.uv = mesh.uv;
        cleanedMesh.triangles = keptTriangles.ToArray();
        cleanedMesh.RecalculateBounds();

        return cleanedMesh;
    }

    private bool AreFacesFacingEachOther(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 u1, Vector3 u2, Vector3 u3,
        Vector3 normal1, Vector3 normal2)
    {
        // 1. Vérifie si les normales sont opposées (angle > 180° - threshold)
        float angle = Vector3.Angle(normal1, normal2);
        if (angle < 180f - normalAngleThreshold)
        {
            return false;
        }

        // 2. Vérifie si les triangles sont proches (distance moyenne < threshold)
        Vector3 center1 = (v1 + v2 + v3) / 3f;
        Vector3 center2 = (u1 + u2 + u3) / 3f;
        float distance = Vector3.Distance(center1, center2);

        if (distance > distanceThreshold)
        {
            return false;
        }

        // 3. Vérifie si les triangles sont parallèles et proches
        // (On suppose que si les normales sont opposées et que les centres sont proches,
        // alors les faces sont "face à face")
        return true;
    }

    [ContextMenu("Réactiver les enfants")]
    public void ReactivateChildren()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
    }
}