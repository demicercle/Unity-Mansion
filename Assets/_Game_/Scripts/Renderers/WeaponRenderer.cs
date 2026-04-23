using System;
using RPGCharacterAnims.Lookups;
using UnityEngine;

public class WeaponRenderer : MonoBehaviour
{
    public bool isLeft;
    public Weapon rpgWeapon = Weapon.Relax;
    public bool inHand = true;
    public bool offHand = true;
    
    private new Renderer renderer;
    
    private void OnEnable()
    {
        if(!renderer) renderer = GetComponent<Renderer>();
        renderer.enabled = Application.isPlaying;
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if(!renderer) renderer = GetComponent<Renderer>();
        renderer.enabled = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
    }
#endif
}
