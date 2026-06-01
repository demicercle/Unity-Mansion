using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneViewSnapModifier
{
    // Snap actuel (défaut : 1.0)
    private static float currentSnap = 1.0f;
    // Snap minimum pour éviter des valeurs trop petites
    private static readonly float minSnap = 0.015625f; // 1/64 (évite les valeurs infiniment petites)
    // Snap maximum pour éviter des valeurs trop grandes
    private static readonly float maxSnap = 1024.0f;

    // Initialise le snap au démarrage de l'éditeur
    static SceneViewSnapModifier()
    {
        // Abonne l'événement de clavier
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        // Vérifie si une touche est pressée
        Event e = Event.current;
        if (e.type == EventType.KeyDown)
        {
            // Multiplie le snap par 2 avec la touche "+"
            if (e.keyCode == KeyCode.Plus || e.keyCode == KeyCode.KeypadPlus)
            {
                currentSnap *= 2.0f;
                // Limite le snap maximum
                if (currentSnap > maxSnap)
                    currentSnap = maxSnap;

                UpdateSnapSettings();
                e.Use(); // Empêche la propagation de l'événement
            }
            // Divise le snap par 2 avec la touche "-"
            else if (e.keyCode == KeyCode.Minus || e.keyCode == KeyCode.KeypadMinus)
            {
                currentSnap /= 2.0f;
                // Limite le snap minimum
                if (currentSnap < minSnap)
                    currentSnap = minSnap;

                UpdateSnapSettings();
                e.Use(); // Empêche la propagation de l'événement
            }
        }
    }

    // Met à jour les paramètres de snap de la SceneView
    private static void UpdateSnapSettings()
    {
        EditorSnapSettings.move = Vector3.one * currentSnap;

        // Affiche le snap actuel dans la console (optionnel)
        Debug.Log($"Snap mis à jour : {currentSnap}");
    }

    // Réinitialise le snap à 1.0 (optionnel, pour un bouton de réinitialisation)
    [MenuItem("Tools/SceneView/Reset Snap to 1.0")]
    private static void ResetSnap()
    {
        currentSnap = 1.0f;
        UpdateSnapSettings();
    }
}