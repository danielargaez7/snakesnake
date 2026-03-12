using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class MissingScriptCleaner : Editor
{
    [MenuItem("BellyFull/Remove Missing Scripts")]
    public static void RemoveMissingScripts()
    {
        int removed = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (count > 0)
            {
                Debug.Log($"[MissingScriptCleaner] Removed {count} missing script(s) from '{go.name}'");
                removed += count;
            }
        }
        if (removed == 0)
            Debug.Log("[MissingScriptCleaner] No missing scripts found.");
        else
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"[MissingScriptCleaner] Done. Total removed: {removed}");
    }
}
