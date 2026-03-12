using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixThanksScreenOrder
{
    [MenuItem("BellyFull/Fix ThanksScreen Render Order")]
    public static void Fix()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("No Canvas found"); return; }

        var t = canvas.transform.Find("ThanksScreen");
        if (t == null) { Debug.LogError("ThanksScreen not found under Canvas"); return; }

        t.SetAsLastSibling();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixThanksScreenOrder] ThanksScreen moved to last — now renders on top of everything.");
    }
}
