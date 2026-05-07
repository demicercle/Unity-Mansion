using System;
using System.Linq;
using RPGCharacterAnims.Lookups;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.Text;

public class WeaponRenderer : MonoBehaviour
{
    public static int debugAim => CharacterEntity.debugAim;
    
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
    private Vector3 aimFrom;

    private void HideAimLine()
    {
        if (!aimLine) return;
        aimLine.gameObject.SetActive(false);
        if(CharacterEntity.debugAim > 1) Debug.Log("HideAimLine");
    }

    private void DisplayAimLine(Vector3 from, params Vector3[] aimingPositions)
    {
        if (!aimLine) return;
        if(debugAim > 1) Debug.Log(this + " DisplayAimLine " + aimingPositions.Length);
        aimLine.gameObject.SetActive(true);
        aimLine.loop = false;
        aimLine.useWorldSpace = true;
        aimLine.positionCount = aimingPositions.Length * 3;
        aimLine.transform.position = from;
        int i = 0;
        int a = 0;
        while (i < aimLine.positionCount && a < aimingPositions.Length)
        {
            if (debugAim > 2) Debug.Log(this + " DisplayAimLine " + i + "/" + aimLine.positionCount + " " + a + "/" + aimingPositions.Length);
            if (debugAim > 0) DebugDrawer.Line(from, aimingPositions[a], Color.magenta, 0f);
            aimLine.SetPosition(i++, from);
            aimLine.SetPosition(i++, aimingPositions[a++]);
            aimLine.SetPosition(i++, from);
        }
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
            aimFrom = transform.position;
            if (shootFromEyesPosition) aimFrom = parentCharacter.GetEyesPosition();
            
            if (!parentCharacter.IsActing() && parentCharacter.GetShootLines(out var positionsTo))
            {
                wasAiming = true;
                DisplayAimLine(aimFrom, positionsTo);
            }
            else if (wasAiming)
            {
                wasAiming = false;
                HideAimLine();
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
