using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Vẽ từng prefab UI lên Canvas 1280×720 (Screen Space – Camera → RenderTexture) và lưu PNG vào Design/previews/,
    /// để so trực tiếp với bản thiết kế Design/canvas/project/UI-*.dc.html. Chạy được ở Edit mode.
    /// </summary>
    public static class UIPreviewRenderer
    {
        const int W = 1280, H = 720;
        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Design", "previews"));

        [MenuItem("Tools/Dungeon Gathering/UI/Render Previews (Prefabs/UI)")]
        public static void RenderAll()
        {
            Directory.CreateDirectory(OutDir);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project/Prefabs/UI" }))
                Render(AssetDatabase.GUIDToAssetPath(guid));
            Render("Assets/Project/Core/Prefabs/UIRoot.prefab", "UIRoot_Loading");
            Debug.Log($"[UIPreview] Đã lưu ảnh vào {OutDir}");
        }

        public static string Render(string prefabPath, string outName = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogWarning($"[UIPreview] Không thấy {prefabPath}"); return null; }

            var camGo = new GameObject("~PreviewCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1b, 0x14, 0x20, 0xff);
            cam.cullingMask = 1 << 5; // UI layer
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var canvasGo = new GameObject("~PreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)) { hideFlags = HideFlags.HideAndDontSave, layer = 5 };
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 10; canvas.pixelPerfect = true;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(W, H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand; scaler.referencePixelsPerUnit = 100;

            string pname = Path.GetFileNameWithoutExtension(prefabPath);
            if (pname.Contains("Menu") || pname.Contains("Loading"))
            {
                var bg = new GameObject("~Backdrop", typeof(RectTransform), typeof(Image)) { hideFlags = HideFlags.HideAndDontSave, layer = 5 };
                bg.transform.SetParent(canvasGo.transform, false);
                var bimg = bg.GetComponent<Image>();
                bimg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/Generated/bg_title_wide.png");
                ((RectTransform)bg.transform).sizeDelta = new Vector2(1600, 720);
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.transform.SetParent(canvasGo.transform, false);
            SetLayer(inst.transform, 5);
            var irt = inst.transform as RectTransform;
            bool nestedCanvas = inst.GetComponent<Canvas>() != null;
            if (nestedCanvas)
            {
                foreach (var cs in inst.GetComponents<CanvasScaler>()) cs.enabled = false;
                inst.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace; // để RectTransform theo cha
                inst.transform.localScale = Vector3.one; inst.transform.localPosition = Vector3.zero;
            }
            if (irt != null && (nestedCanvas || (irt.anchorMin == irt.anchorMax && irt.sizeDelta.x < 1))) { irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one; irt.offsetMin = irt.offsetMax = Vector2.zero; irt.pivot = new Vector2(0.5f, 0.5f); }

            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = prev;

            string file = Path.Combine(OutDir, (outName ?? Path.GetFileNameWithoutExtension(prefabPath)) + ".png");
            File.WriteAllBytes(file, tex.EncodeToPNG());

            Object.DestroyImmediate(tex); Object.DestroyImmediate(inst); Object.DestroyImmediate(canvasGo);
            cam.targetTexture = null; Object.DestroyImmediate(camGo); rt.Release(); Object.DestroyImmediate(rt);
            return file;
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayer(c, layer);
        }
    }
}
