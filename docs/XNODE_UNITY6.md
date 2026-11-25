# xNode + Unity 6 : fix CS0619 (`EndNameEditAction` obsolete)

## Symptôme

Sur Unity 6000.x (projet passé à **6000.6.10f1**), la compilation échoue avec :

```
Assets/xNode-1.8.0/Scripts/Editor/NodeEditorUtilities.cs(226,41): error CS0619:
'EndNameEditAction' is obsolete: 'EndNameEditAction is obsolete.
Use AssetCreationEndAction that uses EntityId instead of int for instance IDs.'
```

## Cause

Depuis Unity 6, `UnityEditor.ProjectWindowCallback.EndNameEditAction` est marquée
`[Obsolete(..., error: true)]` (remplacée par `AssetCreationEndAction`, qui prend un
`EntityId` au lieu d'un `int`). xNode 1.8.0 déclare encore (ligne 226/227 du fichier) :

```csharp
public class DoCreateCodeFile : UnityEditor.ProjectWindowCallback.EndNameEditAction {
    public override void Action(int instanceId, string pathName, string resourceFile) { ... }
}
```

Cette classe ne sert que pour le menu « Assets/Create/xNode/… » (création de script de nœud
depuis un template) — le runtime n'est pas impacté. Aucun fix upstream à ce jour
([issue Siccity/xNode#396](https://github.com/Siccity/xNode/issues/396)).

## Fix appliqué sur cette branche

xNode est désormais importé en local dans **`Assets/xNode-1.8.0`** (la dépendance git
`com.github.siccity.xnode` a été retirée de `Packages/manifest.json` et
`Packages/packages-lock.json`), donc le code du package est versionné et patchable.

Patch : `#pragma warning disable CS0619` ajouté en haut de
`Assets/xNode-1.8.0/Scripts/Editor/NodeEditorUtilities.cs` (ligne 10, après les `using`).
CS0619 étant un warning promu en erreur par `[Obsolete(error: true)]`, le pragma le supprime ;
ça compile sur Unity 6 comme sur les versions plus anciennes, et couvre aussi l'appel
`ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, …)` (conversion `int` → `EntityId`
obsolete) dans `CreateFromTemplate`.

## Alternative « propre » (optionnelle, plus tard)

Si le projet ne cible plus que Unity 6, migrer vers la nouvelle API :

```csharp
#if UNITY_6000_0_OR_NEWER
public class DoCreateCodeFile : UnityEditor.ProjectWindowCallback.AssetCreationEndAction {
    public override void Action(UnityEngine.EntityId entityId, string pathName, string resourceFile) { ... }
}
#else
// version int historique
#endif
```

(voir la [PR Siccity/xNode#395](https://github.com/Siccity/xNode/pull/395), mais elle est
volumineuse — UITK, etc. ; le pragma suffit tant que le projet reste multi-versions).

## Mise à jour de xNode

Remplacer le contenu de `Assets/xNode-1.8.0` par la nouvelle version, puis ré-appliquer le
pragma dans `Scripts/Editor/NodeEditorUtilities.cs` si le fichier a changé.
