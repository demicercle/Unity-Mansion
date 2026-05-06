using System;
using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

public class WeaponRenderer : MonoBehaviour
{
    public static int debugMode => CharacterAnimation.debugMode;
    
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

    public void OnShoot(Vector3 from, Vector3 to)
    {
        if (shootLine)
        {
            var clone = Instantiate(shootLine.gameObject).GetComponent<LineRenderer>();
            clone.transform.position = from;
            clone.useWorldSpace = true;
            clone.positionCount = 2;
            clone.SetPosition(0, from);
            clone.SetPosition(1, to);
            Utils.GetOrAddComponent<AutoDestroy>(clone.gameObject).delay = 1.0f;
            clone.gameObject.SetActive(true);
        }
        
        onShoot?.Invoke();
    }
    
    public UnityEvent onHit;

    
    public void OnHit() => onHit?.Invoke();

    private CharacterEntity parentCharacter;
    private bool wasAiming;

    private void HideAimLine()
    {
        if (!aimLine) return;
        aimLine.gameObject.SetActive(false);
        if(CharacterEntity.debugAim > 1) Debug.Log("HideAimLine");
    }

    private void DisplayAimLine(Vector3 aimingPosition)
    {
        if (!aimLine) return;
        aimLine.gameObject.SetActive(true);
        aimLine.loop = false;
        aimLine.useWorldSpace = false;
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, aimFromEyesPosition && parentCharacter ? aimLine.transform.InverseTransformPoint(parentCharacter.GetEyesPosition()) : Vector3.zero);
        aimLine.SetPosition(1, aimLine.transform.InverseTransformPoint(aimingPosition));
        if(CharacterEntity.debugAim > 1) Debug.Log("DisplayAimLine: " + aimingPosition );
    }
    
    private void OnEnable()
    {
//        if(debugMode > 0) Debug.Log(this + " OnEnable() parentCharacter=" + parentCharacter);
        
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
            if (!parentCharacter.IsActing() && parentCharacter.TryGetAimingPosition(out var aimingPos))
            {
                wasAiming = true;
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
            if(CharacterEntity.debugAim > 1) Debug.Log("DisplayShootLine: " + shootPosition );
            
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
    [ContextMenu("Collect Renderers")]
    private void CollectRenderers()
    {
        UnityEditor.Undo.RecordObject(this, "CollectRenderers");
        renderers = GetComponentsInChildren<Renderer>().Concat(GetComponents<Renderer>()).ToArray();
        Utils.SetDirty(this);
    }
    [ContextMenu("SHOW Renderers")]
    private void SetRendererVisible_On() => SetRendererVisible(true);
    [ContextMenu("HIDE Renderers")]
    private void SetRendererVisible_Off() => SetRendererVisible(false);
    private void SetRendererVisible(bool visible)
    {
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                r.enabled = visible;
                Utils.SetDirty(r);
            }
        }
    }
#endif
}
