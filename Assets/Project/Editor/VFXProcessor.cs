using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonGathering.VFX;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    [Serializable]
    public class PVFXSheet
    {
        public string image;
        public int x;
        public int y;
        public int width;
        public int height;
        public float scale;
    }

    [Serializable]
    public class PVFXDuration
    {
        public int numerator_ms;
        public int denominator;
    }

    [Serializable]
    public class PVFXFrame
    {
        public int frame;
        public PVFXDuration duration;
        public PVFXSheet sheet;
    }

    [Serializable]
    public class PVFXManifest
    {
        public string schema;
        public string program;
        public string loop_mode;
        public int[] source_size;
        public int[] pivot;
        public PVFXFrame[] frames;
    }

    public static class VFXProcessor
    {
        const string VFXRoot = "Assets/Project/Art/Sprites/VFX/PVFX/effects";
        const string OutputRoot = "Assets/Project/Prefabs/VFX";

        [MenuItem("Tools/Dungeon Gathering/Process All VFX into Prefabs")]
        public static void ProcessAll()
        {
            if (!Directory.Exists(VFXRoot))
            {
                Debug.LogError($"Khong tim thay thu muc VFX tai: {VFXRoot}");
                return;
            }

            var manifestFiles = Directory.GetFiles(VFXRoot, "manifest.json", SearchOption.AllDirectories)
                .Where(f => f.Replace("\\", "/").Contains("/lit/grid/"))
                .ToList();

            int total = manifestFiles.Count;
            int count = 0;

            try
            {
                for (int i = 0; i < total; i++)
                {
                    string manifestPath = manifestFiles[i].Replace("\\", "/");
                    EditorUtility.DisplayProgressBar("Processing VFX", manifestPath, (float)i / total);

                    if (ProcessSingleEffect(manifestPath))
                    {
                        count++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[VFXProcessor] Hoan tat tao {count}/{total} VFX Prefabs vao {OutputRoot}!");
        }

        public static bool ProcessSingleEffect(string manifestPath)
        {
            try
            {
                string jsonText = File.ReadAllText(manifestPath);
                var manifest = JsonUtility.FromJson<PVFXManifest>(jsonText);
                if (manifest == null || manifest.frames == null || manifest.frames.Length == 0)
                    return false;

                string effectDir = Path.GetDirectoryName(manifestPath).Replace("\\", "/");
                string texturePath = effectDir + "/sprite-sheet.png";

                if (!File.Exists(texturePath))
                    return false;

                // Phan tich ten category va ten effect tu manifestPath
                // Vi du: .../effects/melee/sword-combo/lit/grid/manifest.json
                string[] parts = manifestPath.Split('/');
                int effIdx = Array.IndexOf(parts, "effects");
                if (effIdx == -1 || effIdx + 2 >= parts.Length)
                    return false;

                string category = ToPascalCase(parts[effIdx + 1]);
                string effectName = ToPascalCase(parts[effIdx + 2]);

                // 1. Tinh toan kich thuoc texture
                var ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (ti == null) return false;

                // Doc kich thuoc that cua texture tu file
                int texW = 0, texH = 0;
                using (var fs = File.OpenRead(texturePath))
                using (var img = System.Drawing.Image.FromStream(fs, false, false))
                {
                    texW = img.Width;
                    texH = img.Height;
                }

                // 2. Thiet lap SpriteMetaData cho tung frame tu manifest
                int sourceW = manifest.source_size[0];
                int sourceH = manifest.source_size[1];
                float pivotX = manifest.pivot[0] / (float)sourceW;
                float pivotY = (sourceH - manifest.pivot[1]) / (float)sourceH;

                var metaList = new List<SpriteMetaData>();
                for (int f = 0; f < manifest.frames.Length; f++)
                {
                    var fr = manifest.frames[f];
                    var smd = new SpriteMetaData();
                    smd.name = $"{parts[effIdx + 2]}_{f}";
                    int yInUnity = texH - fr.sheet.y - fr.sheet.height;
                    smd.rect = new Rect(fr.sheet.x, yInUnity, fr.sheet.width, fr.sheet.height);
                    smd.alignment = (int)SpriteAlignment.Custom;
                    smd.pivot = new Vector2(pivotX, pivotY);
                    metaList.Add(smd);
                }

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Multiple;
                ti.spritePixelsPerUnit = 16;
                ti.filterMode = FilterMode.Point;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.spritesheet = metaList.ToArray();
                EditorUtility.SetDirty(ti);
                ti.SaveAndReimport();

                // 3. Load sprites da slice
                var loadedSprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                    .OfType<Sprite>()
                    .OrderBy(s =>
                    {
                        int id = 0;
                        string[] sp = s.name.Split('_');
                        int.TryParse(sp[sp.Length - 1], out id);
                        return id;
                    })
                    .ToList();

                if (loadedSprites.Count == 0) return false;

                // 4. Thu muc output
                string categoryOutDir = $"{OutputRoot}/{category}";
                string animDir = $"{categoryOutDir}/Animations";
                EnsureDirectory(animDir);

                // 5. Tao AnimationClip
                int durationMs = manifest.frames[0].duration != null && manifest.frames[0].duration.numerator_ms > 0
                    ? manifest.frames[0].duration.numerator_ms
                    : 70;
                float frameDuration = durationMs / 1000f;
                float frameRate = Mathf.Clamp(Mathf.Round(1000f / durationMs), 8f, 30f);

                string clipPath = $"{animDir}/VFX_{category}_{effectName}.anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    clip = new AnimationClip { name = $"VFX_{category}_{effectName}" };
                    AssetDatabase.CreateAsset(clip, clipPath);
                }

                clip.frameRate = frameRate;
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var keyframes = new ObjectReferenceKeyframe[loadedSprites.Count];
                for (int i = 0; i < loadedSprites.Count; i++)
                {
                    keyframes[i] = new ObjectReferenceKeyframe
                    {
                        time = i * frameDuration,
                        value = loadedSprites[i]
                    };
                }
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

                var animSettings = AnimationUtility.GetAnimationClipSettings(clip);
                bool isLoop = manifest.loop_mode != null && manifest.loop_mode.ToLowerInvariant().Contains("loop");
                animSettings.loopTime = isLoop;
                AnimationUtility.SetAnimationClipSettings(clip, animSettings);
                EditorUtility.SetDirty(clip);

                // 6. Tao AnimatorController
                string ctrlPath = $"{animDir}/VFX_{category}_{effectName}.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                }
                var sm = controller.layers[0].stateMachine;
                foreach (var child in sm.states.ToArray()) sm.RemoveState(child.state);
                var state = sm.AddState("Play");
                state.motion = clip;
                sm.defaultState = state;
                EditorUtility.SetDirty(controller);

                // 7. Tao GameObject va Prefab
                string prefabName = $"VFX_{category}_{effectName}";
                var go = new GameObject(prefabName);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = loadedSprites[0];
                var anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;

                if (!isLoop)
                {
                    var autoDestroy = go.AddComponent<AutoDestroyEffect>();
                    autoDestroy.Duration = loadedSprites.Count * frameDuration;
                }

                string prefabPath = $"{categoryOutDir}/{prefabName}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                UnityEngine.Object.DestroyImmediate(go);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VFXProcessor] Loi khi xu ly {manifestPath}: {ex.Message}");
                return false;
            }
        }

        static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        static string ToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var words = text.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + (words[i].Length > 1 ? words[i].Substring(1) : "");
                }
            }
            return string.Concat(words);
        }
    }
}
