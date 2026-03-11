using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BellyFull
{
    public class SpriteAssigner
    {
        [MenuItem("BellyFull/Assign All Sprites")]
        public static void AssignAllSprites()
        {
            // Background
            var bgGO = GameObject.Find("Background");
            if (bgGO != null)
            {
                var sr = bgGO.GetComponent<SpriteRenderer>();
                if (sr == null) sr = bgGO.AddComponent<SpriteRenderer>();
                sr.sprite = LoadSprite("Art/Backgrounds/bg_meadow");
                sr.sortingOrder = -10;
                // PPU handles sizing now
                bgGO.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(bgGO);
                Debug.Log("[SpriteAssigner] Background assigned");
            }

            // Snake heads
            AssignSpriteRenderer("Snake_P1", "Art/Sprites/Snake/snake_p1_head");
            AssignSpriteRenderer("Snake_P2", "Art/Sprites/Snake/snake_p2_head");

            // Prefabs (with sorting orders: field objects at 2, snakes at 10)
            AssignPrefabSprite("Assets/Prefabs/Ball.prefab", "Art/Sprites/Objects/ball", 2);
            AssignPrefabSprite("Assets/Prefabs/Hedgehog.prefab", "Art/Sprites/Objects/hedgehog", 2);
            AssignPrefabSprite("Assets/Prefabs/Flower.prefab", "Art/Sprites/Objects/flower", 1);

            // Belly ball prefab + wire into snake controllers
            UpdateBellyBallPrefab();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[SpriteAssigner] All sprites assigned!");
        }

        static void AssignSpriteRenderer(string goName, string spritePath)
        {
            var go = GameObject.Find(goName);
            if (go == null) { Debug.LogWarning($"[SpriteAssigner] {goName} not found"); return; }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { Debug.LogWarning($"[SpriteAssigner] No SpriteRenderer on {goName}"); return; }
            var sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                EditorUtility.SetDirty(go);
                Debug.Log($"[SpriteAssigner] {goName} sprite set");
            }
        }

        static void AssignPrefabSprite(string prefabPath, string spritePath, int sortingOrder = 0)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogWarning($"[SpriteAssigner] Prefab not found: {prefabPath}"); return; }
            var sr = prefab.GetComponent<SpriteRenderer>();
            if (sr == null) { Debug.LogWarning($"[SpriteAssigner] No SpriteRenderer on prefab {prefabPath}"); return; }
            var sprite = LoadSprite(spritePath);
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color = Color.white;
                sr.sortingOrder = sortingOrder;
                // Reset scale — PPU handles sizing now
                prefab.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(prefab);
                Debug.Log($"[SpriteAssigner] Prefab {prefabPath} sprite set (sort: {sortingOrder})");
            }
        }

        static void UpdateBellyBallPrefab()
        {
            string path = "Assets/Prefabs/BellyBall.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                var go = new GameObject("BellyBall");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LoadSprite("Art/Sprites/Snake/snake_belly_ball");
                sr.sortingOrder = 5;
                go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                Debug.Log("[SpriteAssigner] BellyBall prefab created");
            }
            else
            {
                var sr = existing.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = LoadSprite("Art/Sprites/Snake/snake_belly_ball");
                    EditorUtility.SetDirty(existing);
                }
            }

            var bellyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            WireSnakeController("Snake_P1", bellyPrefab);
            WireSnakeController("Snake_P2", bellyPrefab);
        }

        static void WireSnakeController(string goName, GameObject bellyPrefab)
        {
            var go = GameObject.Find(goName);
            if (go == null) return;
            var sc = go.GetComponent<SnakeController>();
            if (sc == null) return;
            var so = new SerializedObject(sc);
            so.FindProperty("bellyBallPrefab").objectReferenceValue = bellyPrefab;
            so.FindProperty("bodyRenderer").objectReferenceValue = go.GetComponent<SpriteRenderer>();
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);
            Debug.Log($"[SpriteAssigner] {goName} controller wired");
        }

        static Sprite LoadSprite(string path)
        {
            string fullPath = "Assets/" + path + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            if (sprite == null)
                Debug.LogWarning($"[SpriteAssigner] Sprite not found at {fullPath}");
            return sprite;
        }
    }
}