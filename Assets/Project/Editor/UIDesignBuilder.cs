using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using DungeonGathering.UI;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Dựng prefab UI theo đúng bản thiết kế (Design/canvas/project/UI-*.dc.html), chỉ dùng sprite của dự án.
    /// Quy ước: 1 đơn vị UI = 1 px thiết kế; frame 9-slice 6px nguồn hiển thị 24/12/8 đơn vị như class f-*, f-*-s, f-*-xs.
    /// Ghi đè prefab cũ tại cùng đường dẫn (giữ GUID/address). Chạy lại bất cứ lúc nào: kết quả giống hệt.
    /// Docs: docs/UI_SYSTEM.md §4 (neo), §5 (luồng), §6 (thành phần).
    /// </summary>
    public static partial class UIDesignBuilder
    {
        // ---------- nguồn ----------
        const string Sheet = "Assets/Project/Art/Sprites/UI/HUD/Basic Buttons 3 Fv.png";
        const string Items = "Assets/Project/Art/Sprites/Items/spritesheet_16x16.png";
        const string Gen = "Assets/Project/Art/Sprites/UI/Generated/";
        const string DfDir = "Assets/Project/Art/Sprites/UI/DarkFantasy/";
        const string UiDir = "Assets/Project/Prefabs/UI/";
        const string FontPath = "Assets/Project/Art/Fonts/dearpix SDF.asset";
        const float RefPpu = 100f;          // CanvasScaler.referencePixelsPerUnit của UIRoot
        const float SrcBorder = 6f;         // viền nguồn mặc định (px)

        static readonly Color Ink = new Color32(0x17, 0x11, 0x1e, 0xff);     // chữ tối trên nút sáng (FrLight)
        static readonly Color Paper = new Color32(0xef, 0xe6, 0xdc, 0xff);   // chữ sáng
        static readonly Color Soft = new Color32(0xc8, 0xbc, 0xc9, 0xff);
        static readonly Color Gold = new Color32(0xff, 0xd4, 0x66, 0xff);
        static readonly Color Title = new Color32(0xf2, 0xb3, 0x3d, 0xff);
        static readonly Color Green = new Color32(0x6d, 0xe0, 0x8c, 0xff);

        static TMP_FontAsset font;
        static Dictionary<string, string> en;

        // frame Dark Fantasy & giữ nguyên Rarity Slot SqF từ sheet gốc
        static Sprite FrDark => D("fr_dark");
        static Sprite FrMid => D("fr_mid");
        static Sprite FrLight => D("fr_light");
        static Sprite FrBtn => D("fr_btn");
        static Sprite FrCrim => D("fr_crimson");
        static Sprite SqF => At(Sheet, 480, 230);
        static Sprite IcBack => D("btn_back");
        static Sprite IcNext => D("btn_next");
        static Sprite IcGear => D("btn_gear");
        static Sprite IcPause => D("btn_pause");
        static Sprite IcSound => D("btn_sound");
        static Sprite IcMusic => D("btn_music");
        static Sprite IcSwordBtn => D("btn_attack");
        static Sprite IcClose => D("btn_close");
        static Sprite IcSwap => At(Sheet, 515, 229);
        static Sprite Coin => Single("Assets/Project/Art/Sprites/Items/Coins/coin_1.png");
        static Sprite Item(int c, int r) => At(Items, c * 16 + 8, r * 16 + 8);
        static Sprite G(string n) => Single(Gen + n + ".png");
        static Sprite D(string n) => Single(DfDir + n + ".png");

        // [Legacy — tắt] [MenuItem("Tools/Dungeon Gathering/UI/Build Design UI (Menu, Lobby, HUD, Pause)")]
        public static void BuildAll()
        {
            Prepare();
            BuildMenu();
            BuildLobbyHud();
            BuildCharacterPanel();
            BuildPlayHud();
            BuildPause();
            PatchUIRoot();
            AssetDatabase.SaveAssets();
            UIPreviewRenderer.RenderAll();
            Debug.Log("[UIDesign] Đã dựng lại Menu, LobbyHud, CharacterPanel, PlayHud, PauseScreen + Backdrop của UIRoot.");
        }

        // ================= đợt 2: Loadout, Shop, Settings popup, Kết thúc, Loading =================

        // [Legacy — tắt] [MenuItem("Tools/Dungeon Gathering/UI/Build Design UI (Loadout, Shop, Results, Loading)")]
        public static void BuildStage2()
        {
            Prepare();
            BuildEquipment();
            BuildWeapon();
            BuildShop();
            BuildSettingsPopup();
            BuildResult(true);
            BuildResult(false);
            BuildLoading();
            AssetDatabase.SaveAssets();
            UIPreviewRenderer.RenderAll();
            Debug.Log("[UIDesign] Đã dựng Equipment, Weapon, Shop, SettingsPopup, Victory, Defeat, LoadingOverlay.");
        }

        // ================= kit =================

        enum A { TL, T, TR, L, C, R, BL, B, BR, Stretch }

        static void Prepare()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            en = LoadEnglish();
            // import settings cho sprite sinh thêm
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Gen.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 16 || ti.filterMode != FilterMode.Point || ti.textureCompression != TextureImporterCompression.Uncompressed;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 16;
                ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            }
            // import settings cho sprite DarkFantasy (kèm 9-slice border cho fr_* và bar_cell_*)
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { DfDir.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                string fn = Path.GetFileNameWithoutExtension(p);
                Vector4 wantBorder = fn.StartsWith("fr_") ? new Vector4(8, 8, 8, 8)
                    : fn.StartsWith("bar_cell_") ? new Vector4(2, 2, 2, 2)
                    : Vector4.zero;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                    || ti.spritePixelsPerUnit != 16 || ti.filterMode != FilterMode.Point
                    || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.spriteBorder != wantBorder;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 16;
                ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp; ti.spriteBorder = wantBorder; ti.SaveAndReimport();
            }
            // viền 9-slice cho frame trên sheet: dark, mid, light, btn, 5 ô rarity
            SetBorders(Sheet, new[] { (240, 70), (240, 20), (290, 20), (290, 70), (480, 100), (480, 132), (480, 164), (480, 196), (480, 230) }, SrcBorder);
        }

        static Sprite At(string path, int x, int yTop)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning($"[UIDesign] Thiếu {path}"); return null; }
            int y = tex.height - 1 - yTop;
            foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                var r = s.rect;
                if (x >= r.x && x < r.xMax && y >= r.y && y < r.yMax) return s;
            }
            Debug.LogWarning($"[UIDesign] Không có slice tại ({x},{yTop}) trong {path}");
            return null;
        }

        static Sprite Single(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

        static void SetBorders(string path, (int x, int yTop)[] points, float border)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var so = new SerializedObject(importer);
            var sprites = so.FindProperty("m_SpriteSheet.m_Sprites");
            bool changed = false;
            for (int i = 0; i < sprites.arraySize; i++)
            {
                var e = sprites.GetArrayElementAtIndex(i);
                var r = e.FindPropertyRelative("m_Rect").rectValue;
                foreach (var (x, yTop) in points)
                {
                    int y = tex.height - 1 - yTop;
                    if (x < r.x || x >= r.xMax || y < r.y || y >= r.yMax) continue;
                    var b = new Vector4(border, border, border, border);
                    var bp = e.FindPropertyRelative("m_Border");
                    if (bp.vector4Value != b) { bp.vector4Value = b; changed = true; }
                }
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        static Dictionary<string, string> LoadEnglish()
        {
            var map = new Dictionary<string, string>();
            string csv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Design", "localization", "DG_Multilanguage.csv"));
            if (!File.Exists(csv)) return map;
            foreach (var row in ParseCsv(File.ReadAllText(csv, Encoding.UTF8)).Skip(1))
                if (row.Length >= 2) map[row[0]] = row[1];
            return map;
        }

        static string En(string key) => key != null && en != null && en.TryGetValue(key, out var v) ? v : key;

        static GameObject Root<T>(string name, UILayer layer) where T : UIScreen
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var screen = go.AddComponent<T>();
            SetEnum(screen, "layer", (int)layer);
            return go;
        }

        static RectTransform Rt(Component parent, string name, A a, float x, float y, float w, float h) => Rt(parent.transform, name, a, x, y, w, h);
        static RectTransform Rt(GameObject parent, string name, A a, float x, float y, float w, float h) => Rt(parent.transform, name, a, x, y, w, h);
        static RectTransform Rt(Transform parent, string name, A a, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; Place(rt, a, x, y, w, h);
            return rt;
        }

        /// <summary>Đặt theo tọa độ thiết kế. x,y = khoảng cách tới mép của góc neo (y tính từ trên với neo trên). Stretch: x,y,w,h = inset trái, trên, phải, dưới.</summary>
        static void Place(RectTransform rt, A a, float x, float y, float w, float h)
        {
            Vector2 p = a switch
            {
                A.TL => new(0, 1), A.T => new(0.5f, 1), A.TR => new(1, 1), A.L => new(0, 0.5f), A.C => new(0.5f, 0.5f),
                A.R => new(1, 0.5f), A.BL => new(0, 0), A.B => new(0.5f, 0), A.BR => new(1, 0), _ => new(0.5f, 0.5f)
            };
            if (a == A.Stretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new(0.5f, 0.5f);
                rt.offsetMin = new(x, h); rt.offsetMax = new(-w, -y);
                return;
            }
            rt.anchorMin = rt.anchorMax = rt.pivot = p;
            rt.sizeDelta = new(w, h);
            float px = a is A.TR or A.R or A.BR ? -x : x;
            float py = a is A.TL or A.T or A.TR ? -y : y;
            rt.anchoredPosition = new(px, py);
        }

        static Image Img(Component parent, string name, Sprite s, A a, float x, float y, float w, float h) => Img(parent.transform, name, s, a, x, y, w, h);
        static Image Img(GameObject parent, string name, Sprite s, A a, float x, float y, float w, float h) => Img(parent.transform, name, s, a, x, y, w, h);
        static Image Img(Transform parent, string name, Sprite s, A a, float x, float y, float w, float h)
        {
            var rt = Rt(parent, name, a, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s; img.raycastTarget = false;
            if (s == null) img.color = Color.clear;
            return img;
        }

        /// <summary>Khung 9-slice: viền nguồn 6px hiển thị <paramref name="borderUnits"/> đơn vị (24 = f-*, 12 = f-*-s, 8 = f-*-xs).</summary>
        static RectTransform Frame(Component parent, string name, Sprite s, float borderUnits, A a, float x, float y, float w, float h) => Frame(parent.transform, name, s, borderUnits, a, x, y, w, h);
        static RectTransform Frame(GameObject parent, string name, Sprite s, float borderUnits, A a, float x, float y, float w, float h) => Frame(parent.transform, name, s, borderUnits, a, x, y, w, h);
        static RectTransform Frame(Transform parent, string name, Sprite s, float borderUnits, A a, float x, float y, float w, float h)
        {
            var img = Img(parent, name, s, a, x, y, w, h);
            img.type = Image.Type.Sliced; img.fillCenter = true;
            float srcB = s != null && s.border.x > 0f ? s.border.x : SrcBorder;
            img.pixelsPerUnitMultiplier = srcB / (borderUnits * (s != null ? s.pixelsPerUnit : 16f) / RefPpu);
            return img.rectTransform;
        }

        /// <summary>Icon ở bội số nguyên <paramref name="k"/> (§3.2 UI_SYSTEM: ưu tiên ×2/×4).</summary>
        static Image Icon(Component parent, string name, Sprite s, float k, A a, float x, float y)
        {
            Vector2 size = s != null ? s.rect.size * k : new Vector2(16, 16) * k;
            var img = Img(parent.transform, name, s, a, x, y, size.x, size.y);
            img.preserveAspect = true;
            return img;
        }
        static Image Icon(GameObject parent, string name, Sprite s, float k, A a, float x, float y) => Icon(parent.transform, name, s, k, a, x, y);

        static TextMeshProUGUI Txt(Component parent, string name, string key, string raw, float size, Color color, TextAlignmentOptions align, A a, float x, float y, float w, float h)
        {
            var rt = Rt(parent.transform, name, a, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false; t.richText = true;
            t.text = key != null ? En(key) : raw ?? string.Empty;
            if (key != null) AddLoc(t, key);
            return t;
        }
        static TextMeshProUGUI Txt(GameObject parent, string name, string key, string raw, float size, Color color, TextAlignmentOptions align, A a, float x, float y, float w, float h)
            => Txt(parent.transform, name, key, raw, size, color, align, a, x, y, w, h);

        static LocalizedTMPText AddLoc(TMP_Text t, string key)
        {
            var loc = t.GetComponent<LocalizedTMPText>() ?? t.gameObject.AddComponent<LocalizedTMPText>();
            var f = typeof(LocalizedTMPText).GetField("localizedString", BindingFlags.NonPublic | BindingFlags.Instance);
            var ls = new LocalizedString();
            if (key != null) ls.SetReference(LocalizationFontConfig.TableName, key);
            else ls.TableReference = LocalizationFontConfig.TableName;
            f.SetValue(loc, ls);
            EditorUtility.SetDirty(loc);
            return loc;
        }

        static void Shadow(TMP_Text t, Color c, float dy)
        {
            var copy = Object.Instantiate(t.gameObject, t.transform.parent);
            copy.name = t.name + "_Shadow";
            copy.transform.SetSiblingIndex(t.transform.GetSiblingIndex());
            var ct = copy.GetComponent<TMP_Text>(); ct.color = c;
            ((RectTransform)copy.transform).anchoredPosition += new Vector2(0, -dy);
            var loc = copy.GetComponent<LocalizedTMPText>(); if (loc != null) Object.DestroyImmediate(loc);
        }

        static Button IconButton(Component parent, string name, Sprite icon, float k, A a, float x, float y, float w, float h)
        {
            var rt = Rt(parent.transform, name, a, x, y, w, h);
            var hit = rt.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var ic = Icon(rt, "Icon", icon, k, A.C, 0, 0);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = ic;
            return b;
        }
        static Button IconButton(GameObject parent, string name, Sprite icon, float k, A a, float x, float y, float w, float h) => IconButton(parent.transform, name, icon, k, a, x, y, w, h);

        static Button TextButton(GameObject parent, string name, Sprite frame, float border, string key, float size, Color color, A a, float x, float y, float w, float h, Sprite icon, float k, string raw = null)
            => TextButton(parent.transform, name, frame, border, key, size, color, a, x, y, w, h, icon, k, raw);
        static Button TextButton(Component parent, string name, Sprite frame, float border, string key, float size, Color color, A a, float x, float y, float w, float h, Sprite icon, float k, string raw = null)
        {
            var rt = Frame(parent.transform, name, frame, border, a, x, y, w, h);
            var img = rt.GetComponent<Image>(); img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            float shift = 0;
            if (icon != null) { Icon(rt, "Icon", icon, k, A.L, 22, -2); shift = icon.rect.width * k * 0.5f + 6; }
            Txt(rt, "Label", key, raw, size, color, TextAlignmentOptions.Center, A.C, shift, -2, w - 24, h - 16);
            return b;
        }

        /// <summary>Thanh chia ô động: khung tối 9-slice + HorizontalLayoutGroup chứa SegmentedBar + các ô cell 9-slice.</summary>
        static SegmentedBar SegmentedBarTrack(Component parent, string name, string cellSprite, A a, float x, float y, float w, float h, float border, int cur, int max, int unitsPerCell, int prewarm = 12, float spacing = 2f)
        {
            var track = Frame(parent.transform, name, FrDark, border, a, x, y, w, h);
            var cont = Rt(track, "Segments", A.Stretch, border, border, border, border);
            var hlg = cont.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            var bar = cont.gameObject.AddComponent<SegmentedBar>();
            Sprite fillS = D(cellSprite);
            Sprite emptyS = D("bar_cell_empty");
            float ppuMul = 2f / (2f * (fillS != null ? fillS.pixelsPerUnit : 16f) / RefPpu);
            var cells = new Object[prewarm];
            for (int i = 0; i < prewarm; i++)
            {
                var c = Img(cont, "Seg_" + i, fillS, A.Stretch, 0, 0, 0, 0);
                c.type = Image.Type.Sliced;
                c.pixelsPerUnitMultiplier = ppuMul;
                cells[i] = c;
            }
            Wire(bar, ("filledSprite", fillS), ("emptySprite", emptyS));
            var so = new SerializedObject(bar);
            so.FindProperty("pixelsPerUnitMultiplier").floatValue = ppuMul;
            so.ApplyModifiedPropertiesWithoutUndo();
            WireArray(bar, "cells", cells);
            bar.SetValue(cur, max, unitsPerCell);
            return bar;
        }

        /// <summary>Thanh: rãnh khung tối 9-slice + dải màu Filled Horizontal (driver: Image.fillAmount) + chữ giá trị ở giữa.</summary>
        static (Image fill, TMP_Text value) Bar(Component parent, string name, string strip, A a, float x, float y, float w, float h, float border, float fontSize, string val)
        {
            var track = Frame(parent.transform, name, FrDark, border, a, x, y, w, h);
            var fill = Img(track, "Fill", G(strip), A.Stretch, border, border, border, border);
            MakeFilled(fill);
            var t = Txt(track, "Value", null, val, fontSize, Paper, TextAlignmentOptions.Center, A.Stretch, 0, -6, 0, -6);
            return (fill, t);
        }

        static void MakeFilled(Image img)
        {
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal; img.fillOrigin = 0; img.fillAmount = 0.8f;
        }

        static ItemTile Tile(GameObject prefab, Component parent, string name, A a, float x, float y, float w, float h, bool letter)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.name = name;
            Place((RectTransform)go.transform, a, x, y, w, h);
            var tile = go.GetComponent<ItemTile>();
            SetBool(tile, "showRarityLetter", letter);
            tile.SetEmpty(null);   // trạng thái mặc định của prefab = ô trống (runtime SetItem/SetEmpty ghi đè)
            tile.SetSelected(false);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static void Wire(Object target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (f, v) in pairs)
            {
                var p = so.FindProperty(f);
                if (p == null) { Debug.LogWarning($"[UIDesign] {target.GetType().Name} không có field '{f}'"); continue; }
                p.objectReferenceValue = v;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[UIDesign] {target.GetType().Name} không có field '{field}'"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool v)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p != null) { p.boolValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetEnum(Object target, string field, int v)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p != null) { p.enumValueIndex = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void Save(GameObject root, string path, string address)
        {
            SetLayerRecursive(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrEmpty(address)) return;
            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
        }

        static void SetLayerRecursive(Transform t)
        {
            t.gameObject.layer = 5;
            foreach (Transform c in t) SetLayerRecursive(c);
        }

        static List<string[]> ParseCsv(string text)
        {
            var rows = new List<string[]>(); var row = new List<string>(); var cell = new StringBuilder(); bool q = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (q) { if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else if (ch == '"') q = false; else cell.Append(ch); continue; }
                switch (ch)
                {
                    case '"': q = true; break;
                    case ',': row.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n': row.Add(cell.ToString()); cell.Clear(); rows.Add(row.ToArray()); row.Clear(); break;
                    default: cell.Append(ch); break;
                }
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row.ToArray()); }
            return rows;
        }
    }
}
