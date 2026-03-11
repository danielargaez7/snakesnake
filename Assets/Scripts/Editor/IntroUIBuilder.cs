using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using BellyFull;

/// <summary>
/// Menu: BellyFull / Build Intro UI
/// Creates the TitlePanel, WaitingPanel, and CountdownPanel inside a new
/// IntroCanvas, then wires all references on IntroManager.
/// Run once after adding IntroManager to the GameManager object.
/// </summary>
public class IntroUIBuilder : Editor
{
    [MenuItem("BellyFull/Build Intro UI")]
    public static void Build()
    {
        // ── 1. Find IntroManager ────────────────────────────────────────────
        var introManager = Object.FindFirstObjectByType<IntroManager>();
        if (introManager == null)
        {
            Debug.LogError("[IntroUIBuilder] No IntroManager found in scene. " +
                           "Add IntroManager component to GameManager first.");
            return;
        }

        // Remove existing IntroCanvas if rebuilding
        var existing = GameObject.Find("IntroCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        // ── 2. Create IntroCanvas (overlay, sort order above HUD canvas) ────
        var canvasGO = new GameObject("IntroCanvas", typeof(RectTransform));
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;  // above game HUD (sort order 0)

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── 3. TitlePanel ───────────────────────────────────────────────────
        var titlePanel = MakeFullPanel(canvasGO.transform, "TitlePanel", new Color(0, 0, 0, 1f));

        // Placeholder image for the title graphic (full-screen, user drops sprite here)
        var titleImgGO = MakeRect(titlePanel.transform, "TitleImage");
        StretchFull(titleImgGO.GetComponent<RectTransform>());
        var titleImg = titleImgGO.AddComponent<Image>();
        titleImg.color = Color.white;  // neutral — becomes invisible once sprite is assigned

        // "Tap to Start" blinking label at bottom
        var tapText = MakeText(titlePanel.transform, "TapToStartText",
            "Tap anywhere to start!", 52, Color.white);
        var tapRT = tapText.GetComponent<RectTransform>();
        tapRT.anchorMin = new Vector2(0, 0);
        tapRT.anchorMax = new Vector2(1, 0);
        tapRT.pivot     = new Vector2(0.5f, 0);
        tapRT.anchoredPosition = new Vector2(0, 60);
        tapRT.sizeDelta        = new Vector2(0, 70);
        tapText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // ── 4. WaitingPanel ─────────────────────────────────────────────────
        // Semi-transparent dark bar at the bottom third
        var waitingPanel = new GameObject("WaitingPanel", typeof(RectTransform));
        waitingPanel.transform.SetParent(canvasGO.transform, false);
        var wRT = waitingPanel.GetComponent<RectTransform>();
        wRT.anchorMin       = new Vector2(0, 0);
        wRT.anchorMax       = new Vector2(1, 0.38f);
        wRT.offsetMin       = Vector2.zero;
        wRT.offsetMax       = Vector2.zero;
        var wBg = waitingPanel.AddComponent<Image>();
        wBg.color = new Color(0f, 0f, 0f, 0.72f);

        // "Place Your Tokens!" label
        var placeText = MakeText(waitingPanel.transform, "PlaceTokensText",
            "Place Your Tokens!", 72, Color.white);
        var ptRT = placeText.GetComponent<RectTransform>();
        ptRT.anchorMin = new Vector2(0.1f, 0.55f);
        ptRT.anchorMax = new Vector2(0.9f, 1f);
        ptRT.offsetMin = ptRT.offsetMax = Vector2.zero;
        placeText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // P1 indicator (green circle + checkmark glow)
        var p1Indicator = MakePlayerIndicator(waitingPanel.transform, "P1Indicator",
            "P1", new Color(0.2f, 0.85f, 0.3f), -300);
        var p1Glow = MakeReadyGlow(p1Indicator.transform, new Color(0.2f, 1f, 0.4f, 0.8f));

        // P2 indicator (blue circle + checkmark glow)
        var p2Indicator = MakePlayerIndicator(waitingPanel.transform, "P2Indicator",
            "P2", new Color(0.3f, 0.6f, 1f), 300);
        var p2Glow = MakeReadyGlow(p2Indicator.transform, new Color(0.4f, 0.7f, 1f, 0.8f));

        // Editor hint
        var hintText = MakeText(waitingPanel.transform, "EditorHintText",
            "[Editor: press Space]", 28, new Color(1f, 1f, 1f, 0.4f));
        var hRT = hintText.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0.1f, 0f);
        hRT.anchorMax = new Vector2(0.9f, 0.2f);
        hRT.offsetMin = hRT.offsetMax = Vector2.zero;
        hintText.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        // ── 5. CountdownPanel ───────────────────────────────────────────────
        var countdownPanel = new GameObject("CountdownPanel", typeof(RectTransform));
        countdownPanel.transform.SetParent(canvasGO.transform, false);
        var cdRT = countdownPanel.GetComponent<RectTransform>();
        cdRT.anchorMin = new Vector2(0.3f, 0.3f);
        cdRT.anchorMax = new Vector2(0.7f, 0.7f);
        cdRT.offsetMin = cdRT.offsetMax = Vector2.zero;

        var cdText = MakeText(countdownPanel.transform, "CountdownText", "3", 160,
            new Color(1f, 0.92f, 0.2f));
        var cdTMP = cdText.GetComponent<TMP_Text>();
        cdTMP.alignment = TextAlignmentOptions.Center;
        cdTMP.fontStyle = FontStyles.Bold;
        StretchFull(cdText.GetComponent<RectTransform>());

        // ── 6. Initial visibility ───────────────────────────────────────────
        titlePanel.SetActive(true);
        waitingPanel.SetActive(false);
        countdownPanel.SetActive(false);
        p1Glow.SetActive(false);
        p2Glow.SetActive(false);

        // ── 7. Wire IntroManager via SerializedObject ───────────────────────
        var so = new SerializedObject(introManager);
        so.FindProperty("titlePanel").objectReferenceValue    = titlePanel;
        so.FindProperty("waitingPanel").objectReferenceValue  = waitingPanel;
        so.FindProperty("countdownPanel").objectReferenceValue = countdownPanel;
        so.FindProperty("titleImage").objectReferenceValue    = titleImg;
        so.FindProperty("p1ReadyGlow").objectReferenceValue   = p1Glow;
        so.FindProperty("p2ReadyGlow").objectReferenceValue   = p2Glow;
        so.FindProperty("countdownText").objectReferenceValue = cdTMP;

        // Wire snakes if present
        var snake1 = GameObject.Find("Snake_P1");
        var snake2 = GameObject.Find("Snake_P2");
        if (snake1) so.FindProperty("snake1Transform").objectReferenceValue = snake1.transform;
        if (snake2) so.FindProperty("snake2Transform").objectReferenceValue = snake2.transform;

        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[IntroUIBuilder] Done! Intro UI built and wired.\n" +
                  ">> Drop your title graphic onto: IntroCanvas/TitlePanel/TitleImage (Image component → Source Image field)");
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    static GameObject MakeFullPanel(Transform parent, string name, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin       = Vector2.zero;
        rt.anchorMax       = Vector2.one;
        rt.offsetMin       = Vector2.zero;
        rt.offsetMax       = Vector2.zero;
    }

    static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject MakeText(Transform parent, string name, string text, int fontSize, Color color)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static GameObject MakePlayerIndicator(Transform parent, string name, string label, Color color, float xOffset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0.5f, 0.0f);
        rt.anchorMax       = new Vector2(0.5f, 0.55f);
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xOffset, 0);
        rt.sizeDelta        = new Vector2(200, 0);

        // Colored circle background
        var circle = new GameObject("Circle", typeof(RectTransform));
        circle.transform.SetParent(go.transform, false);
        var cRT = circle.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.1f, 0.1f);
        cRT.anchorMax = new Vector2(0.9f, 0.9f);
        cRT.offsetMin = cRT.offsetMax = Vector2.zero;
        var img = circle.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0.25f);

        // Label text
        var txt = MakeText(go.transform, "Label", label, 48, color);
        StretchFull(txt.GetComponent<RectTransform>());
        txt.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;

        return go;
    }

    static GameObject MakeReadyGlow(Transform parent, Color color)
    {
        var go = new GameObject("ReadyGlow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }
}
