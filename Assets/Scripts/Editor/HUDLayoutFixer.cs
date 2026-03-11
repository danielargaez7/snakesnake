using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace BellyFull
{
    public class HUDLayoutFixer
    {
        [MenuItem("BellyFull/Fix HUD Layout")]
        public static void FixHUDLayout()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) { Debug.LogError("Canvas not found"); return; }

            // Rebuild both HUD panels (no energy bar - it's shared now)
            RebuildHUDPanel("HUD_P1", canvas, "_P1",
                new Vector2(0, 0.75f), new Vector2(0.25f, 1),
                PlayerIndex.Player1);

            RebuildHUDPanel("HUD_P2", canvas, "_P2",
                new Vector2(0.75f, 0.75f), new Vector2(1, 1),
                PlayerIndex.Player2);

            // Build shared energy bar at top-center
            BuildSharedEnergyBar(canvas);

            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[HUDLayoutFixer] HUD layout fixed!");
        }

        static void BuildSharedEnergyBar(GameObject canvas)
        {
            // Delete old shared bar if it exists
            var old = GameObject.Find("SharedEnergyBar");
            if (old != null) Object.DestroyImmediate(old);

            // Delete old per-player energy bar objects that are now orphaned
            foreach (var name in new[] { "EnergyBar_P1", "EnergyBarGlow_P1", "EnergyBar_P2", "EnergyBarGlow_P2" })
            {
                var obj = GameObject.Find(name);
                if (obj != null) Object.DestroyImmediate(obj);
            }

            // Create container - top center
            var container = new GameObject("SharedEnergyBar", typeof(RectTransform));
            container.transform.SetParent(canvas.transform, false);
            var containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(0.3f, 0.92f);
            containerRT.anchorMax = new Vector2(0.7f, 0.98f);
            containerRT.offsetMin = Vector2.zero;
            containerRT.offsetMax = Vector2.zero;

            // Fill image (behind frame) - uses energy_bar_fill sprite
            var fillGO = new GameObject("EnergyBarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGO.transform.SetParent(container.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(8, 6);   // inset so fill sits inside frame
            fillRT.offsetMax = new Vector2(-8, -6);
            var fillImg = fillGO.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;
            fillImg.color = new Color(0.2f, 0.8f, 0.2f); // green

            // Load fill sprite
            var fillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/energy_bar_fill.png");
            if (fillSprite != null) fillImg.sprite = fillSprite;

            // Frame image (on top) - uses energy_bar_frame sprite
            var frameGO = new GameObject("EnergyBarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameGO.transform.SetParent(container.transform, false);
            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero;
            frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = Vector2.zero;
            frameRT.offsetMax = Vector2.zero;
            var frameImg = frameGO.GetComponent<Image>();
            frameImg.raycastTarget = false;

            // Load frame sprite
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/energy_bar_frame.png");
            if (frameSprite != null) frameImg.sprite = frameSprite;

            // Add SharedEnergyBarUI component and wire references
            var barUI = container.AddComponent<SharedEnergyBarUI>();
            var so = new SerializedObject(barUI);
            so.FindProperty("fillImage").objectReferenceValue = fillImg;
            so.FindProperty("frameImage").objectReferenceValue = frameImg;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(container);
            Debug.Log("[HUDLayoutFixer] Created SharedEnergyBar at top-center");
        }

        static void RebuildHUDPanel(string panelName, GameObject canvas, string suffix,
            Vector2 anchorMin, Vector2 anchorMax, PlayerIndex playerIndex)
        {
            // Delete any existing panel with this name (broken or otherwise)
            var existing = GameObject.Find(panelName);
            while (existing != null)
            {
                for (int i = existing.transform.childCount - 1; i >= 0; i--)
                    existing.transform.GetChild(i).SetParent(canvas.transform, false);
                Object.DestroyImmediate(existing);
                existing = GameObject.Find(panelName);
            }

            // Create new panel WITH RectTransform
            var panel = new GameObject(panelName, typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(10, 0);
            rt.offsetMax = new Vector2(-10, -10);

            Debug.Log($"[HUDLayoutFixer] Created {panelName} with RectTransform");

            // Delete old BellyCount child (no longer separate)
            var oldBelly = GameObject.Find($"BellyCount{suffix}");
            if (oldBelly != null) Object.DestroyImmediate(oldBelly);

            // Find and reparent children by name (they may be anywhere in scene)
            string[] childNames = {
                $"Equation{suffix}",
                $"BlastOverlay{suffix}",
                $"BlastEatCount{suffix}"
            };

            foreach (var childName in childNames)
            {
                var child = GameObject.Find(childName);
                if (child != null)
                {
                    child.transform.SetParent(panel.transform, false);
                    Debug.Log($"  Reparented {childName}");
                }
            }

            // Equation text spans most of the panel (rich text: "3+2=2")
            LayoutChild(panel, $"Equation{suffix}",
                new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.95f), 40);
            LayoutChild(panel, $"BlastEatCount{suffix}",
                new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), 40);

            // Add and wire GameHUD component
            var hud = panel.AddComponent<GameHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("playerIndex").enumValueIndex = (int)playerIndex;

            WireRef<TextMeshProUGUI>(so, "equationText", panel, $"Equation{suffix}");
            WireRef<TextMeshProUGUI>(so, "blastEatCountText", panel, $"BlastEatCount{suffix}");

            var overlay = panel.transform.Find($"BlastOverlay{suffix}");
            if (overlay != null)
                so.FindProperty("blastOverlay").objectReferenceValue = overlay.gameObject;

            var countdown = GameObject.Find("CountdownText");
            if (countdown != null)
            {
                var tmp = countdown.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                    so.FindProperty("countdownText").objectReferenceValue = tmp;
            }

            so.FindProperty("crownIcons").arraySize = 3;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
        }

        static void WireRef<T>(SerializedObject so, string propName,
            GameObject parent, string childName) where T : Component
        {
            var t = parent.transform.Find(childName);
            if (t == null) return;
            var comp = t.GetComponent<T>();
            if (comp != null)
                so.FindProperty(propName).objectReferenceValue = comp;
        }

        static void LayoutChild(GameObject parent, string childName,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            var t = parent.transform.Find(childName);
            if (t == null) return;

            var rt = t.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                EditorUtility.SetDirty(t.gameObject);
            }

            if (fontSize > 0)
            {
                var tmp = t.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.fontSize = fontSize;
                    tmp.alignment = TextAlignmentOptions.Center;
                    EditorUtility.SetDirty(tmp);
                }
            }
        }
    }
}
