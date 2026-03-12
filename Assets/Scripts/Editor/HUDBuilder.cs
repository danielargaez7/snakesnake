using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using BellyFull;

/// <summary>
/// Menu: BellyFull / Build HUD
/// Creates per-player HUD panels inside the existing Canvas and wires GameHUD.
/// Layout: number sprite (large) + hole dots row + completed badges strip + crowns
/// </summary>
public class HUDBuilder : Editor
{
    // Number sprite GUIDs (number_1 .. number_10)
    static readonly string[] NumberGuids =
    {
        "ce6f9d45b72904d0bba3e089a8ddd77d", // 1
        "1b67181fef5314acda2ad1a3af7df09e", // 2
        "afa59068bd2e24ba1bad7d8b64613421", // 3
        "8614aa7e4c6ee4662ac55c0dc0ad2b80", // 4
        "a641b61807c92422c821493f005eef34", // 5
        "2f3b8426ee04146f28eeb8a18a4e5003", // 6
        "506cf2e4f406c43b4be5a3435b341fea", // 7
        "e6153937d24fc407a85f14a1b83e518f", // 8
        "5e9f889ae84be4a6e967fbe387d0765b", // 9
        "127edba02c1ad4582a55ee96edfeb438", // 10
    };

    const string CrownGuid = "293232315f72444d7adcf6d4e6d8403c";

    [MenuItem("BellyFull/Build HUD")]
    public static void Build()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("[HUDBuilder] No Canvas found."); return; }

        // Load number sprites
        var numberSprites = new Sprite[10];
        for (int i = 0; i < 10; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(NumberGuids[i]);
            numberSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (numberSprites[i] == null)
                Debug.LogWarning($"[HUDBuilder] number_{i+1} sprite not found at {path}");
        }

        var crownSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            AssetDatabase.GUIDToAssetPath(CrownGuid));

        // Remove old HUD panels if rebuilding
        DestroyChild(canvas, "HUD_P1");
        DestroyChild(canvas, "HUD_P2");
        DestroyChild(canvas, "BlastUI");

        // Build both panels
        var hud1GO = BuildPlayerHUD(canvas.transform, "HUD_P1", PlayerIndex.Player1,
                                     isLeft: true,  numberSprites, crownSprite);
        var hud2GO = BuildPlayerHUD(canvas.transform, "HUD_P2", PlayerIndex.Player2,
                                     isLeft: false, numberSprites, crownSprite);

        // Build center blast UI (countdown + confetti)
        BuildBlastUI(canvas.transform, numberSprites);

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[HUDBuilder] Done! HUD_P1, HUD_P2, and BlastUI built and wired.");
    }

    // ── Per-player panel ─────────────────────────────────────────────────────

    static GameObject BuildPlayerHUD(Transform canvas, string name,
        PlayerIndex player, bool isLeft, Sprite[] numSprites, Sprite crownSprite)
    {
        // Root panel — anchored to middle-left or middle-right
        var root = MakeRect(canvas, name);
        var rootRT = root.GetComponent<RectTransform>();
        float xAnchor = isLeft ? 0f : 1f;
        rootRT.anchorMin        = new Vector2(xAnchor, 0.5f);
        rootRT.anchorMax        = new Vector2(xAnchor, 0.5f);
        rootRT.pivot            = new Vector2(xAnchor, 0.5f);
        rootRT.anchoredPosition = new Vector2(isLeft ? 10f : -10f, 0f);
        rootRT.sizeDelta        = new Vector2(420f, 620f);

        // No opaque background — number sprite provides its own visual

        // ── Completed badges strip (very top) ──────────────────────────────
        var completedRow = MakeRect(root.transform, "CompletedRow");
        var crRT = completedRow.GetComponent<RectTransform>();
        crRT.anchorMin        = new Vector2(0f, 1f);
        crRT.anchorMax        = new Vector2(1f, 1f);
        crRT.pivot            = new Vector2(0.5f, 1f);
        crRT.anchoredPosition = new Vector2(0f, -4f);
        crRT.sizeDelta        = new Vector2(0f, 64f);
        var crLayout = completedRow.AddComponent<HorizontalLayoutGroup>();
        crLayout.childAlignment      = TextAnchor.MiddleCenter;
        crLayout.spacing             = 2f;
        crLayout.childForceExpandWidth  = false;
        crLayout.childForceExpandHeight = false;
        crLayout.padding = new RectOffset(4, 4, 2, 2);

        // ── Crown icons row ────────────────────────────────────────────────
        var crownRow = MakeRect(root.transform, "CrownRow");
        var crowRT = crownRow.GetComponent<RectTransform>();
        crowRT.anchorMin        = new Vector2(0f, 1f);
        crowRT.anchorMax        = new Vector2(1f, 1f);
        crowRT.pivot            = new Vector2(0.5f, 1f);
        crowRT.anchoredPosition = new Vector2(0f, -74f);
        crowRT.sizeDelta        = new Vector2(0f, 36f);
        var crownLayout = crownRow.AddComponent<HorizontalLayoutGroup>();
        crownLayout.childAlignment      = TextAnchor.MiddleCenter;
        crownLayout.spacing             = 6f;
        crownLayout.childForceExpandWidth  = false;
        crownLayout.childForceExpandHeight = false;

        var crownIcons = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            var cGO = MakeRect(crownRow.transform, $"Crown_{i}");
            var cRT = cGO.GetComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(30f, 30f);
            var cImg = cGO.AddComponent<Image>();
            if (crownSprite != null) cImg.sprite = crownSprite;
            else cImg.color = new Color(1f, 0.85f, 0.1f);
            cGO.SetActive(false);
            crownIcons[i] = cGO;
        }

        // ── Number image (large, centered) ────────────────────────────────
        var numGO = MakeRect(root.transform, "NumberImage");
        var numRT = numGO.GetComponent<RectTransform>();
        numRT.anchorMin        = new Vector2(0.5f, 0.5f);
        numRT.anchorMax        = new Vector2(0.5f, 0.5f);
        numRT.pivot            = new Vector2(0.5f, 0.5f);
        numRT.anchoredPosition = new Vector2(0f, 60f);
        numRT.sizeDelta        = new Vector2(380f, 380f);
        var numImg = numGO.AddComponent<Image>();
        numImg.preserveAspect = true;
        if (numSprites[0] != null) numImg.sprite = numSprites[0];

        // ── Holes container — sits directly below the NumberImage ──────────
        // NumberImage bottom = anchoredPosition.y - sizeDelta.y/2 = 60 - 190 = -130
        var holesGO = MakeRect(root.transform, "HolesContainer");
        var holesRT = holesGO.GetComponent<RectTransform>();
        holesRT.anchorMin        = new Vector2(0.5f, 0.5f);
        holesRT.anchorMax        = new Vector2(0.5f, 0.5f);
        holesRT.pivot            = new Vector2(0.5f, 1f);   // top edge is the anchor point
        holesRT.anchoredPosition = new Vector2(0f, -138f);  // top edge just below number bottom
        holesRT.sizeDelta        = new Vector2(380f, 80f);
        var holesLayout = holesGO.AddComponent<HorizontalLayoutGroup>();
        holesLayout.childAlignment      = TextAnchor.MiddleCenter;
        holesLayout.spacing             = 6f;
        holesLayout.childForceExpandWidth  = false;
        holesLayout.childForceExpandHeight = false;
        holesLayout.padding = new RectOffset(8, 8, 4, 4);

        // ── Blast overlay ──────────────────────────────────────────────────
        var blastOverlay = MakeRect(root.transform, "BlastOverlay");
        var bRT = blastOverlay.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;
        var bBg = blastOverlay.AddComponent<Image>();
        bBg.color = new Color(1f, 0.5f, 0f, 0.25f);

        var blastCountGO = MakeText(blastOverlay.transform, "BlastCount", "0", 96,
                                     new Color(1f, 0.9f, 0.1f));
        var bcRT = blastCountGO.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0f, 0.45f); bcRT.anchorMax = new Vector2(1f, 1f);
        bcRT.offsetMin = bcRT.offsetMax = Vector2.zero;
        blastCountGO.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;

        var blastTimerGO = MakeText(blastOverlay.transform, "BlastTimer", "8", 40,
                                     Color.white);
        var btRT = blastTimerGO.GetComponent<RectTransform>();
        btRT.anchorMin = new Vector2(0f, 0f); btRT.anchorMax = new Vector2(1f, 0.5f);
        btRT.offsetMin = btRT.offsetMax = Vector2.zero;

        blastOverlay.SetActive(false);

        // ── Win banner ─────────────────────────────────────────────────────
        var winBanner = MakeRect(root.transform, "WinBanner");
        var wRT = winBanner.GetComponent<RectTransform>();
        wRT.anchorMin = Vector2.zero; wRT.anchorMax = Vector2.one;
        wRT.offsetMin = wRT.offsetMax = Vector2.zero;
        var wBg = winBanner.AddComponent<Image>();
        wBg.color = new Color(0.9f, 0.75f, 0.1f, 0.85f);
        var wTxt = MakeText(winBanner.transform, "WinText", "WIN!", 72, Color.white);
        var wtRT = wTxt.GetComponent<RectTransform>();
        wtRT.anchorMin = Vector2.zero; wtRT.anchorMax = Vector2.one;
        wtRT.offsetMin = wtRT.offsetMax = Vector2.zero;
        winBanner.SetActive(false);

        // ── Wire GameHUD ───────────────────────────────────────────────────
        var hud = root.AddComponent<GameHUD>();
        var so  = new SerializedObject(hud);

        so.FindProperty("playerIndex").enumValueIndex = (int)player;

        // Number sprites array
        var spritesProp = so.FindProperty("numberSprites");
        spritesProp.arraySize = 10;
        for (int i = 0; i < 10; i++)
            spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = numSprites[i];

        so.FindProperty("numberImage").objectReferenceValue         = numImg;
        so.FindProperty("holesContainer").objectReferenceValue      = holesGO.transform;
        so.FindProperty("completedRowContainer").objectReferenceValue = completedRow.transform;
        so.FindProperty("blastOverlay").objectReferenceValue        = blastOverlay;
        so.FindProperty("blastEatCountText").objectReferenceValue   = blastCountGO.GetComponent<TMP_Text>();
        so.FindProperty("blastTimerText").objectReferenceValue      = blastTimerGO.GetComponent<TMP_Text>();
        so.FindProperty("winBanner").objectReferenceValue           = winBanner;

        // Crown icons array
        var crownProp = so.FindProperty("crownIcons");
        crownProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            crownProp.GetArrayElementAtIndex(i).objectReferenceValue = crownIcons[i];

        so.ApplyModifiedProperties();

        return root;
    }

    // ── Blast UI (center countdown + confetti) ────────────────────────────────

    static void BuildBlastUI(Transform canvas, Sprite[] numSprites)
    {
        var root = MakeRect(canvas, "BlastUI");
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = rootRT.offsetMax = Vector2.zero;

        // ── #5 sprite shown centre-top during pre-blast ────────────────────
        var cnGO = MakeRect(root.transform, "CenterNumberDisplay");
        var cnRT = cnGO.GetComponent<RectTransform>();
        cnRT.anchorMin = new Vector2(0.35f, 0.58f);
        cnRT.anchorMax = new Vector2(0.65f, 0.95f);
        cnRT.offsetMin = cnRT.offsetMax = Vector2.zero;
        var cnImg = cnGO.AddComponent<Image>();
        cnImg.preserveAspect = true;
        if (numSprites != null && numSprites.Length >= 5 && numSprites[4] != null)
            cnImg.sprite = numSprites[4];
        cnGO.SetActive(false);

        // ── Countdown number — large, centre ──────────────────────────────
        var cdGO = MakeText(root.transform, "CountdownText", "", 200, Color.white);
        var cdRT = cdGO.GetComponent<RectTransform>();
        cdRT.anchorMin = new Vector2(0.25f, 0.3f);
        cdRT.anchorMax = new Vector2(0.75f, 0.7f);
        cdRT.offsetMin = cdRT.offsetMax = Vector2.zero;
        var cdTmp = cdGO.GetComponent<TMP_Text>();
        cdTmp.fontStyle    = FontStyles.Bold;
        cdTmp.outlineWidth = 0.2f;
        cdTmp.outlineColor = new Color32(0, 0, 0, 180);
        cdGO.SetActive(false);

        // ── "GREAT JOB!" text ─────────────────────────────────────────────
        var gjGO = MakeText(root.transform, "GreatJobText", "GREAT JOB!", 110,
                             new Color(1f, 0.92f, 0.1f));
        var gjRT = gjGO.GetComponent<RectTransform>();
        gjRT.anchorMin = new Vector2(0.15f, 0.4f);
        gjRT.anchorMax = new Vector2(0.85f, 0.6f);
        gjRT.offsetMin = gjRT.offsetMax = Vector2.zero;
        var gjTmp = gjGO.GetComponent<TMP_Text>();
        gjTmp.fontStyle    = FontStyles.Bold;
        gjTmp.outlineWidth = 0.25f;
        gjTmp.outlineColor = new Color32(0, 0, 0, 200);
        gjGO.SetActive(false);

        // ── Confetti container ────────────────────────────────────────────
        var confettiGO = MakeRect(root.transform, "ConfettiContainer");
        var confettiRT = confettiGO.GetComponent<RectTransform>();
        confettiRT.anchorMin = Vector2.zero;
        confettiRT.anchorMax = Vector2.one;
        confettiRT.offsetMin = confettiRT.offsetMax = Vector2.zero;

        // ── Wire BlastUI ──────────────────────────────────────────────────
        var blastUI = root.AddComponent<BlastUI>();
        var so = new SerializedObject(blastUI);

        so.FindProperty("centerNumberImage").objectReferenceValue   = cnImg;
        so.FindProperty("countdownText").objectReferenceValue       = cdTmp;
        so.FindProperty("greatJobText").objectReferenceValue        = gjTmp;
        so.FindProperty("confettiContainer").objectReferenceValue   = confettiRT;

        // Wire the same number sprites so #5 can be shown
        if (numSprites != null)
        {
            var sp = so.FindProperty("numberSprites");
            sp.arraySize = numSprites.Length;
            for (int i = 0; i < numSprites.Length; i++)
                sp.GetArrayElementAtIndex(i).objectReferenceValue = numSprites[i];
        }

        so.ApplyModifiedProperties();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static GameObject MakeRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static GameObject MakeText(Transform parent, string name, string text,
                                int fontSize, Color color)
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

    static void DestroyChild(GameObject parent, string childName)
    {
        var t = parent.transform.Find(childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }
}
