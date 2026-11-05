using System.Linq;
using System.Reflection;
using DungeonGathering.Catalog;
using DungeonGathering.Localization;
using DungeonGathering.Lobby;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Dựng 12 chỗ đứng nhân vật trong Lobby.unity theo roster (docs/DATA.md §3).
    /// Vị trí = tọa độ thiết kế 1280×720 (UI-Lobby.dc.html) quy ra world: camera ortho 7.5 → 1 đơn vị = 48 px.
    /// Hình: nhân vật có animator (đã ra mắt) → Animator + SpriteRenderer; còn lại → SpriteRenderer portrait.
    /// Nhân vật chưa ra mắt có ComingSoonMarker (chữ "CHƯA RA MẮT" world-space) — CharacterSpot bật/tắt lúc chạy.
    /// Chạy lại bất cứ lúc nào: chỗ đứng đã có được cập nhật, không nhân đôi.
    /// </summary>
    public static class LobbyRosterBuilder
    {
        const string ScenePath = "Assets/Project/Scenes/Lobby.unity";
        const string CatalogPath = "Assets/Project/Data/Catalog/ItemCatalog.asset";
        const string FontPath = "Assets/Project/Art/Fonts/PixelifySans-Regular SDF.asset";
        const float PxPerUnit = 48f;
        const float HeroHeightUnits = 1.45f; // chiều cao phần có hình của nhân vật trong sảnh (≈ 70 px thiết kế)

        // (id, x, y) — 5 vị trí đầu theo UI-Lobby.dc.html, 7 vị trí mới ở dải sàn trống (tránh Portal, kệ thương nhân, bàn giữa)
        static readonly (string id, float x, float y)[] Roster =
        {
            ("knight", 352, 380), ("ranger", 520, 590), ("wizard", 900, 440), ("rogue", 200, 600), ("paladin", 1070, 580),
            ("monk", 470, 300), ("lancer", 810, 300), ("hunter", 200, 420), ("necromancer", 1080, 400),
            ("frost-wraith", 700, 560), ("berserker", 360, 520), ("shadow-knight", 880, 600),
        };

        [MenuItem("Tools/Dungeon Gathering/Lobby/Build Character Spots")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(CatalogPath);
            if (catalog == null) { Debug.LogError("[Lobby] Chưa có ItemCatalog — chạy Tools ▸ Dungeon Gathering ▸ Data ▸ Seed All trước."); return; }

            var entry = Object.FindFirstObjectByType<LobbyEntry>();
            if (entry == null) { Debug.LogError("[Lobby] Lobby.unity thiếu LobbyEntry."); return; }
            var so = new SerializedObject(entry);
            so.FindProperty("fallbackCatalog").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();

            var parent = entry.transform.Find("CharacterSpots");
            if (parent == null) { parent = new GameObject("CharacterSpots").transform; parent.SetParent(entry.transform, false); }

            var existing = parent.GetComponentsInChildren<CharacterSpot>(true).ToList();
            // Đổi id cũ "mage" → "wizard" (khớp key chữ char.wizard.*)
            foreach (var s in existing.Where(s => s.CharacterId == "mage")) SetString(s, "characterId", "wizard");
            var template = existing.FirstOrDefault()?.GetComponent<BoxCollider2D>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            foreach (var (id, x, y) in Roster)
            {
                var data = catalog.GetCharacter(id);
                if (data == null) { Debug.LogWarning($"[Lobby] Catalog không có nhân vật '{id}'."); continue; }

                var spot = existing.FirstOrDefault(s => s.CharacterId == id);
                if (spot == null)
                {
                    var go = new GameObject();
                    go.transform.SetParent(parent, false);
                    var box = go.AddComponent<BoxCollider2D>();
                    if (template != null) { box.size = template.size; box.offset = template.offset; } else box.size = new Vector2(1f, 1.4f);
                    spot = go.AddComponent<CharacterSpot>();
                    existing.Add(spot);
                }
                var t = spot.transform;
                t.name = "CharacterSpot_" + id;
                t.localPosition = new Vector3((x - 640f) / PxPerUnit, (360f - y) / PxPerUnit, 0f);

                var visualRoot = Child(t, "VisualRoot", Vector3.zero);
                var selected = Child(t, "SelectedMarker", new Vector3(0f, 1.35f, 0f));
                var oldLocked = t.Find("LockedMarker");
                if (oldLocked != null) Object.DestroyImmediate(oldLocked.gameObject);
                var soon = Child(t, "ComingSoonMarker", new Vector3(0f, 1.05f, 0f));
                BuildVisual(visualRoot, data, Mathf.RoundToInt(-t.localPosition.y * 10f));
                BuildSelectedMarker(selected);
                BuildComingSoonLabel(soon, font);
                soon.gameObject.SetActive(!data.released);
                selected.gameObject.SetActive(false);

                var sso = new SerializedObject(spot);
                sso.FindProperty("characterId").stringValue = id;
                sso.FindProperty("visualRoot").objectReferenceValue = visualRoot;
                sso.FindProperty("selectedMarker").objectReferenceValue = selected.gameObject;
                sso.FindProperty("lockedMarker").objectReferenceValue = null;
                sso.FindProperty("comingSoonMarker").objectReferenceValue = soon.gameObject;
                sso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(spot);
            }

            // Thứ tự trong Hierarchy theo roster
            for (int i = 0; i < Roster.Length; i++)
            {
                var s = existing.FirstOrDefault(e => e.CharacterId == Roster[i].id);
                if (s != null) s.transform.SetSiblingIndex(i);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Lobby] Đã dựng {Roster.Length} chỗ đứng nhân vật ({Roster.Count(r => catalog.GetCharacter(r.id)?.released == true)} đã ra mắt).");
        }

        static Transform Child(Transform parent, string name, Vector3 localPos)
        {
            var c = parent.Find(name);
            if (c == null) { c = new GameObject(name).transform; c.SetParent(parent, false); }
            c.localPosition = localPos;
            return c;
        }

        /// <summary>Thay hình trong VisualRoot: animator (nhân vật đã ra mắt) hoặc portrait tĩnh.</summary>
        static void BuildVisual(Transform root, CharacterData data, int order)
        {
            for (int i = root.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            var go = new GameObject(data.animator != null ? "Animated" : "Portrait");
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = data.portrait;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = order;
            if (data.animator != null) go.AddComponent<Animator>().runtimeAnimatorController = data.animator;
            // Cùng chiều cao PHẦN CÓ HÌNH cho mọi sheet (FantasyKnights ô 48px thân ~31px; Dungeon_Character 16px kín ô)
            float k = 1f;
            if (data.portrait != null)
            {
                float contentUnits = DungeonGathering.UI.SpriteContentFit.ContentPixels(data.portrait).height / data.portrait.pixelsPerUnit;
                k = contentUnits > 0.01f ? HeroHeightUnits / contentUnits : 1f;
            }
            go.transform.localScale = new Vector3(k, k, 1f);
        }

        static void BuildSelectedMarker(Transform marker)
        {
            for (int i = marker.childCount - 1; i >= 0; i--) Object.DestroyImmediate(marker.GetChild(i).gameObject);
            var sr = marker.GetComponent<SpriteRenderer>();
            if (sr == null) sr = marker.gameObject.AddComponent<SpriteRenderer>();
            var arrow = AssetDatabase.LoadAllAssetsAtPath("Assets/Project/Art/Sprites/UI/Indicators/arrow_1.png").OfType<Sprite>().FirstOrDefault();
            sr.sprite = arrow;
            sr.color = new Color32(0xff, 0xd2, 0x7a, 0xff);
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 240;
            marker.localPosition = new Vector3(0f, 1.35f, 0f); // Mũi tên chỉ xuống trên đầu nhân vật
            marker.localRotation = Quaternion.identity;
            marker.localScale = new Vector3(1.3f, 1.3f, 1f);

            // Hiệu ứng nhấp nhô & chớp sáng
            if (marker.GetComponent<IndicatorPulse>() == null)
                marker.gameObject.AddComponent<IndicatorPulse>();
        }

        static void BuildLockedMarker(Transform marker, int unlockCost, TMP_FontAsset font)
        {
            marker.localPosition = new Vector3(0f, -1.15f, 0f); // Dưới chân nhân vật
            var iconGo = marker.Find("Coin")?.gameObject;
            if (iconGo == null) { iconGo = new GameObject("Coin"); iconGo.transform.SetParent(marker, false); }
            iconGo.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            iconGo.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            var sr = iconGo.GetComponent<SpriteRenderer>();
            if (sr == null) sr = iconGo.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Project/Art/Sprites/Items/Coins/coin_1.png").OfType<Sprite>().FirstOrDefault();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 230;

            var labelGo = marker.Find("Price")?.gameObject;
            if (labelGo == null) { labelGo = new GameObject("Price", typeof(RectTransform)); labelGo.transform.SetParent(marker, false); }
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (tmp == null) tmp = labelGo.AddComponent<TextMeshPro>();
            labelGo.transform.localPosition = new Vector3(0.18f, 0.05f, 0f);
            if (font != null) tmp.font = font;
            tmp.text = unlockCost > 0 ? unlockCost.ToString() : "LOCKED";
            tmp.fontSize = 3.6f;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = new Color32(0xff, 0xd2, 0x7a, 0xff);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(3f, 0.8f);
            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null) { mr.sortingLayerName = "Characters"; mr.sortingOrder = 230; }
        }

        static void BuildComingSoonLabel(Transform marker, TMP_FontAsset font)
        {
            var labelGo = marker.Find("Label")?.gameObject;
            if (labelGo == null) { labelGo = new GameObject("Label", typeof(RectTransform)); labelGo.transform.SetParent(marker, false); }
            // Lưu ý: không dùng "??" với UnityEngine.Object (fake null) và không giữ Transform cũ (TMP thay bằng RectTransform).
            var tmp = labelGo.GetComponent<TextMeshPro>();
            if (tmp == null) tmp = labelGo.AddComponent<TextMeshPro>();
            labelGo.transform.localPosition = Vector3.zero;
            if (font != null) tmp.font = font;
            tmp.text = "CHƯA RA MẮT";
            tmp.fontSize = 2.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color32(0xff, 0xd2, 0x7a, 0xff);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 0.6f);
            tmp.sortingOrder = 200;

            var loc = labelGo.GetComponent<LocalizedTMPText>();
            if (loc == null) loc = labelGo.AddComponent<LocalizedTMPText>();
            var ls = new LocalizedString();
            ls.SetReference(LocalizationFontConfig.TableName, "char.coming_soon");
            typeof(LocalizedTMPText).GetField("localizedString", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(loc, ls);
            EditorUtility.SetDirty(loc);
        }

        static void SetString(Object o, string prop, string value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(prop).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
