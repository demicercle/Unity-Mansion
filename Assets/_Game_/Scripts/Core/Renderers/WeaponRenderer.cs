using System;
using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

public class WeaponRenderer : MonoBehaviour
{
    public bool isLeft;
    public Weapon rpgWeapon = Weapon.Relax;
    public string itemTags = "";
    public bool inHand = true;
    public bool offHand = true;

    [SerializeField] private Renderer[] renderers = Array.Empty<Renderer>();
    [SerializeField] private LineRenderer shootLine;
    
    public UnityEvent onShoot;

    public void OnShoot()
    {
        if (shootLine)
        {
            var selfCharacter = GetComponentInParent<CharacterEntity>();
            if (selfCharacter)
            {
                var lastAttackTarget = selfCharacter.GetLastAttackTarget();
                var shootPosition = lastAttackTarget?.GetPosition() ?? shootLine.transform.position 
                    + Vector3.Scale(new Vector3(1,0,1), shootLine.transform.forward) 
                    * (selfCharacter.GetLastAttackTarget().GetPosition() - selfCharacter.GetPosition()).magnitude;

                DisplayLine(shootPosition, true);
            }
        }
    }

    public void HideLine()
    {
        if (!shootLine) return;
        shootLine.gameObject.SetActive(false);
        if(CharacterEntity.debugAim > 0) Debug.Log("HideLine");
    }

    public void DisplayLine(Vector3 aimingPosition,bool animate)
    {
        if (!shootLine) return;
        shootLine.gameObject.SetActive(true);
        shootLine.loop = false;
        shootLine.useWorldSpace = false;
        shootLine.positionCount = 2;
        shootLine.SetPosition(0, Vector3.zero);
        shootLine.SetPosition(1, shootLine.transform.InverseTransformPoint(aimingPosition));
        if(animate)
            shootLine.GetComponent<Animation>()?.Play();
        if(CharacterEntity.debugAim > 0) Debug.Log("DisplayLine: " + aimingPosition + " " + animate);
    }

    public bool DisplaysItem(ItemData itemData)
    {
        return (rpgWeapon == Weapon.RightItem || rpgWeapon == Weapon.LeftItem) && itemData.CompareTags(itemTags);
    }
    
    public UnityEvent onHit;

    
    public void OnHit() => onHit?.Invoke();

    private CharacterEntity parentCharacter;
    private bool wasAiming;
    
    private void OnEnable()
    {
        foreach (var rd in renderers)
        {
            rd.gameObject.layer = Layers.LayerEquipment;
            rd.enabled = true;
        }
    }

    private void LateUpdate()
    {
        if (parentCharacter)
        {
            if (parentCharacter.TryGetAimingPosition(out var aimingPos))
            {
                wasAiming = true;
                DisplayLine(aimingPos, false);
            }
            else if (wasAiming)
            {
                wasAiming = false;
                HideLine();
            }
        }
    }

    private void Awake()
    {
        parentCharacter = GetComponentInParent<CharacterEntity>();
        if (shootLine)
            shootLine.gameObject.SetActive(false);
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
        var isVisible = prefabStage && prefabStage.prefabContentsRoot == gameObject;
        foreach (var rd in renderers)
        {
            rd.enabled = isVisible;
        }
    }
    #endif
}
