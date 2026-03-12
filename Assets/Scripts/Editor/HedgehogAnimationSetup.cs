using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Menu: BellyFull / Setup Hedgehog Animation
/// Slices sprite sheets, creates animation clips, builds Animator Controller,
/// and wires everything on the Hedgehog prefab.
/// </summary>
public class HedgehogAnimationSetup : Editor
{
    const string MovePNG    = "Assets/Art/Sprites/pixel_art_hedgehog/move/move.png";
    const string IdlePNG    = "Assets/Art/Sprites/pixel_art_hedgehog/idle/idle.png";
    const string OutDir     = "Assets/Art/Sprites/pixel_art_hedgehog";
    const string PrefabPath = "Assets/Prefabs/Hedgehog.prefab";

    const int FrameW     = 24;
    const int FrameH     = 24;
    const int MoveFrames = 6;
    const int IdleFrames = 2;
    const float MoveFPS  = 8f;
    const float IdleFPS  = 4f;

    [MenuItem("BellyFull/Setup Hedgehog Animation")]
    public static void Run()
    {
        // 1. Slice sprite sheets
        var moveSprites = SliceSheet(MovePNG, MoveFrames, "move");
        var idleSprites = SliceSheet(IdlePNG, IdleFrames, "idle");

        if (moveSprites.Count == 0 || idleSprites.Count == 0)
        {
            Debug.LogError("[HedgehogAnim] Sprite slicing failed. Check paths.");
            return;
        }

        Debug.Log($"[HedgehogAnim] Sliced {moveSprites.Count} move frames, {idleSprites.Count} idle frames.");

        // 2. Delete old clips/controller if they exist
        DeleteIfExists(OutDir + "/Hedgehog_Walk.anim");
        DeleteIfExists(OutDir + "/Hedgehog_Idle.anim");
        DeleteIfExists(OutDir + "/HedgehogAnimator.controller");

        // 3. Create animation clips
        var moveClip = CreateClip(moveSprites, "Hedgehog_Walk", MoveFPS);
        var idleClip = CreateClip(idleSprites, "Hedgehog_Idle", IdleFPS);

        // 4. Build Animator Controller
        string controllerPath = OutDir + "/HedgehogAnimator.controller";
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("isMoving", AnimatorControllerParameterType.Bool);

        var sm = controller.layers[0].stateMachine;

        var idleState = sm.AddState("Idle");
        idleState.motion = idleClip;
        sm.defaultState = idleState;

        var walkState = sm.AddState("Walk");
        walkState.motion = moveClip;

        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;

        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;

        AssetDatabase.SaveAssets();

        // 5. Wire onto Hedgehog prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[HedgehogAnim] Hedgehog.prefab not found."); return; }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
        {
            var root = scope.prefabContentsRoot;

            var anim = root.GetComponent<Animator>();
            if (anim == null) anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;

            var sr = root.GetComponent<SpriteRenderer>();
            if (sr != null && idleSprites.Count > 0)
                sr.sprite = idleSprites[0];
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[HedgehogAnim] Done! Hedgehog.prefab wired with walk + idle animations.");
    }

    // ── Sprite Slicing (Unity 2022+ API) ────────────────────────────────────

    static List<Sprite> SliceSheet(string path, int frameCount, string prefix)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) { Debug.LogError($"[HedgehogAnim] No importer at {path}"); return new List<Sprite>(); }

        // Basic import settings
        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.filterMode          = FilterMode.Point;
        importer.mipmapEnabled       = false;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize      = 256;

        // Use ISpriteEditorDataProvider (Unity 2021+ proper API)
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        var rects = new SpriteRect[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            rects[i] = new SpriteRect
            {
                name      = prefix + "_" + i,
                spriteID  = GUID.Generate(),
                rect      = new Rect(i * FrameW, 0, FrameW, FrameH),
                pivot     = new Vector2(0.5f, 0.5f),
                alignment = SpriteAlignment.Center
            };
        }

        dataProvider.SetSpriteRects(rects);
        dataProvider.Apply();
        ((AssetImporter)dataProvider.targetObject).SaveAndReimport();

        // Load the resulting sprites
        var result = new List<Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            if (asset is Sprite s) result.Add(s);

        result.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return result;
    }

    // ── Animation Clip ──────────────────────────────────────────────────────

    static AnimationClip CreateClip(List<Sprite> sprites, string name, float fps)
    {
        var clip = new AnimationClip { name = name, frameRate = fps };

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "",
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[sprites.Count];
        float frameDur = 1f / fps;
        for (int i = 0; i < sprites.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i * frameDur, value = sprites[i] };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        string clipPath = OutDir + "/" + name + ".anim";
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
