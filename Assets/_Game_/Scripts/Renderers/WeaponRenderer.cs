using System;
using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;

public class WeaponRenderer : MonoBehaviour
{
    public bool isLeft;
    public Weapon rpgWeapon = Weapon.Relax;
    public bool inHand = true;
    public bool offHand = true;

    [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();

    private void OnEnable()
    {
        foreach (var rd in renderers)
        {
            rd.gameObject.layer = Layers.LayerEquipment;
            rd.enabled = true;
        }
    }

#if UNITY_EDITOR

    [ContextMenu("Collect Renderers/Include Inactive")]
    private void CollectRenderers_On() => CollectRenderers(true);
    [ContextMenu("Collect Renderers/Active Only")]
    private void CollectRenderers_Off() => CollectRenderers(false);
    private void CollectRenderers(bool includeInactive)
    {
        UnityEditor.Undo.RecordObject(this, "CollectRenderers");
        renderers = GetComponentsInChildren<Renderer>(includeInactive).Concat(GetComponents<Renderer>()).ToArray();
        Utils.SetDirty(this);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying) return;
        var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        var isVisible = prefabStage; //&& prefabStage.prefabContentsRoot == gameObject;
        foreach (var rd in renderers)
        {
            rd.enabled = isVisible;
        }
    }
    #endif
}
