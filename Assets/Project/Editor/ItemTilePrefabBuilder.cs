using System.Linq;
using DungeonGathering.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    /// <summary>Builds Assets/Project/Prefabs/UI/ItemTile.prefab from the project's own UI sprites.</summary>
    public static class ItemTilePrefabBuilder
    {
        const string PrefabPath = "Assets/Project/Prefabs/UI/ItemTile.prefab";
        const string SmallPrefabPath = "Assets/Project/Prefabs/UI/ItemTileSmall.prefab";
        const string ButtonsSheet = "Assets/Project/Art/Sprites/UI/HUD/Basic Buttons 3 Fv.png";
        const string SelectFrame = "Assets/Project/Art/Sprites/UI/Indicators/square_right_4.png";

        // Basic Buttons 3 Fv slices, ordered S, A, B, C, F.
        static readonly string[] SquareNames = { "Basic Buttons 3 Fv_75", "Basic Buttons 3 Fv_101", "Basic Buttons 3 Fv_109", "Basic Buttons 3 Fv_124", "Basic Buttons 3 Fv_141" };
        static readonly string[] LetterNames = { "Basic Buttons 3 Fv_27", "Basic Buttons 3 Fv_28", "Basic Buttons 3 Fv_29", "Basic Buttons 3 Fv_30", "Basic Buttons 3 Fv_31" };

        // Square slice is 26x27 source px; the interactive tile is drawn at x4 (104x108) per UI_SYSTEM §6.1.
        public const float Scale = 4f;
        public static readonly Vector2 TileSize = new(26 * Scale, 27 * Scale);
        public const float SmallScale = 2f;
        public static readonly Vector2 SmallTileSize = new(26 * SmallScale, 27 * SmallScale);
        const float ReferencePixelsPerUnit = 100f;

        [MenuItem("Tools/Dungeon Gathering/Build ItemTile Prefab")]
        public static void Build()
        {
            EnsureSelectFrameBorder();
            BuildVariant(PrefabPath, Scale, 28f, 12f, "ItemTile");
            BuildVariant(SmallPrefabPath, SmallScale, 16f, 6f, "ItemTileSmall");
        }

        static void BuildVariant(string prefabPath, float scale, float fontSize, float selectInset, string goName)
        {
            var sheet = AssetDatabase.LoadAllAssetsAtPath(ButtonsSheet).OfType<Sprite>().ToDictionary(s => s.name);
            Sprite[] squares = SquareNames.Select(n => sheet[n]).ToArray();
            Sprite[] letters = LetterNames.Select(n => sheet[n]).ToArray();
            Sprite frame = AssetDatabase.LoadAllAssetsAtPath(SelectFrame).OfType<Sprite>().First();

            var root = new GameObject(goName, typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(26 * scale, 27 * scale);

            Image background = AddImage(root.transform, "Background", squares[(int)Inventory.Rarity.F]);
            Stretch(background.rectTransform, 0f);
            background.raycastTarget = true;

            Image icon = AddImage(root.transform, "Icon", null);
            Center(icon.rectTransform, new Vector2(16 * scale, 16 * scale), new Vector2(0f, 0.5f * scale));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Image letter = AddImage(root.transform, "RarityLetter", letters[(int)Inventory.Rarity.F]);
            var lrt = letter.rectTransform;
            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0f, 1f);
            lrt.sizeDelta = new Vector2(9 * (scale * 0.5f), 10 * (scale * 0.5f));
            lrt.anchoredPosition = new Vector2(2 * scale, -2 * scale);
            letter.raycastTarget = false;

            Image cooldown = AddImage(root.transform, "CooldownOverlay", squares[(int)Inventory.Rarity.F]);
            Stretch(cooldown.rectTransform, 0f);
            cooldown.color = new Color(0.07f, 0.06f, 0.1f, 0.65f);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.raycastTarget = false;
            cooldown.enabled = false;

            var textGo = new GameObject("CooldownText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(root.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            Stretch(text.rectTransform, 0f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enabled = false;

            Image select = AddImage(root.transform, "SelectionFrame", frame);
            Stretch(select.rectTransform, -selectInset);
            select.type = Image.Type.Sliced;
            select.fillCenter = false;
            // 4px source border drawn at (4 * scale) canvas px
            select.pixelsPerUnitMultiplier = 4f / (frame.pixelsPerUnit / ReferencePixelsPerUnit * (4f * scale));
            select.raycastTarget = false;
            select.enabled = false;

            var tile = root.AddComponent<ItemTile>();
            var so = new SerializedObject(tile);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("rarityLetter").objectReferenceValue = letter;
            so.FindProperty("cooldownOverlay").objectReferenceValue = cooldown;
            so.FindProperty("cooldownText").objectReferenceValue = text;
            so.FindProperty("selectionFrame").objectReferenceValue = select;
            SetArray(so.FindProperty("raritySquares"), squares);
            SetArray(so.FindProperty("rarityLetters"), letters);
            so.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Project/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ItemTilePrefabBuilder] Saved {prefabPath}");
        }

        /// <summary>square_right_4 needs a 4px 9-slice border so the brackets hug any tile size.</summary>
        static void EnsureSelectFrameBorder()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(SelectFrame);
            var so = new SerializedObject(importer);
            var sprites = so.FindProperty("m_SpriteSheet.m_Sprites");
            bool changed = false;
            for (int i = 0; i < sprites.arraySize; i++)
            {
                var border = sprites.GetArrayElementAtIndex(i).FindPropertyRelative("m_Border");
                if (border.vector4Value != new Vector4(4, 4, 4, 4)) { border.vector4Value = new Vector4(4, 4, 4, 4); changed = true; }
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        static Image AddImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void Center(RectTransform rt, Vector2 size, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = offset;
        }

        static void SetArray(SerializedProperty prop, Sprite[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
