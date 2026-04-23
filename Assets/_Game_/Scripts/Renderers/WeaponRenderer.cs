using System;
using RPGCharacterAnims.Lookups;
using UnityEngine;

public class WeaponRenderer : MonoBehaviour
{
    public bool isLeft;
    public Weapon rpgWeapon = Weapon.Relax;

    private new Renderer renderer;
    
    private void OnEnable()
    {
        if(!renderer) renderer = GetComponent<Renderer>();
        renderer.enabled = Application.isPlaying;
    }
}
