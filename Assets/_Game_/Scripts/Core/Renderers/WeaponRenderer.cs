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
    [SerializeField] private LineRenderer shootLine, aimLine;
    [SerializeField] private float maxShootDistance = 5f;
    [SerializeField] private bool shootFromEyesPosition;
    [SerializeField] private bool aimFromEyesPosition;
    
    public UnityEvent onShoot;

    private Vector3 muzzlePosition;
    private Vector3 shootPosition;

    public void OnShoot()
    {
        if (shootLine)
        {
            var selfCharacter = GetComponentInParent<CharacterEntity>();
            if (selfCharacter)
            {
                var lastAttackTarget = selfCharacter.GetLastAttackTarget();

                muzzlePosition = shootFromEyesPosition ? selfCharacter.GetEyesPosition() : shootLine.transform.position;
                
                shootPosition = selfCharacter.TryGetLastShootHit(out var hit) ? hit.point :
                    selfCharacter.TryGetAimingPosition(out var aimingPos) ? aimingPos :
                    lastAttackTarget?.GetPosition() ?? shootLine.transform.TransformPoint(Vector3.forward * maxShootDistance);
                
                shootLine.gameObject.SetActive(true);
            }
        }
        
        onShoot?.Invoke();
    }

    public void HideAimLine()
    {
        if (!aimLine) return;
        aimLine.gameObject.SetActive(false);
        if(CharacterEntity.debugAim > 0) Debug.Log("HideAimLine");
    }

    public void DisplayAimLine(Vector3 aimingPosition)
    {
        if (!aimLine) return;
        aimLine.gameObject.SetActive(true);
        aimLine.loop = false;
        aimLine.useWorldSpace = false;
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, aimFromEyesPosition && parentCharacter ? aimLine.transform.InverseTransformPoint(parentCharacter.GetEyesPosition()) : Vector3.zero);
        aimLine.SetPosition(1, aimLine.transform.InverseTransformPoint(aimingPosition));
        if(CharacterEntity.debugAim > 0) Debug.Log("DisplayAimLine: " + aimingPosition );
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
        
        if(shootLine)shootLine.gameObject.SetActive(false);
        if(aimLine)aimLine.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (parentCharacter)
        {
            if (!parentCharacter.Routines.AnyActive())
            {
                wasAiming = true;
                if (parentCharacter.TryGetAimingPosition(out var aimingPos))
                    DisplayAimLine(aimingPos);
            }
            else if (wasAiming)
            {
                wasAiming = false;
                HideAimLine();
            }
        }

        if (shootLine && shootLine.gameObject.activeSelf)
        {
            shootLine.loop = false;
            shootLine.useWorldSpace = false;
            shootLine.positionCount = 2;
            shootLine.SetPosition(0, shootLine.transform.InverseTransformPoint(muzzlePosition));
            shootLine.SetPosition(1, shootLine.transform.InverseTransformPoint(shootPosition));
            if(CharacterEntity.debugAim > 0) Debug.Log("DisplayShootLine: " + shootPosition );
            
            HideAimLine();
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
