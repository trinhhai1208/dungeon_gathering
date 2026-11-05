using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using DungeonGathering.Play;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Builder tự động sinh và chuẩn hóa các Prefab thực thể (ENTITIES_PREFABS.md):
    /// Tileset, Player, Enemy_Base + 5 Enemy Variants + 3 Boss Prefabs, Traps,
    /// Combat FX (SpawnWarningMarker, DamagePopup), Interaction (Chest, ShopStand, Portal, RoomDoor),
    /// RoomTemplate + 5 Room Variants (Start, Combat, Treasure, Shop, Boss), và PlayPrefabSet.
    /// </summary>
    public static class EntityPrefabBuilder
    {
        const string PlayerPrefabPath = "Assets/Project/Prefabs/Player/Player.prefab";
        const string EnemyBasePath = "Assets/Project/Prefabs/Enemies/Enemy_Base.prefab";
        const string SpawnMarkerPath = "Assets/Project/Prefabs/Combat/SpawnWarningMarker.prefab";
        const string DamagePopupPath = "Assets/Project/Prefabs/Combat/DamagePopup.prefab";
        const string ChestPrefabPath = "Assets/Project/Prefabs/Rooms/Chest.prefab";
        const string ShopStandPrefabPath = "Assets/Project/Prefabs/Rooms/ShopStand.prefab";
        const string PortalPrefabPath = "Assets/Project/Prefabs/Rooms/Portal.prefab";
        const string RoomDoorPrefabPath = "Assets/Project/Prefabs/Rooms/RoomDoor.prefab";
        const string RoomTemplatePath = "Assets/Project/Prefabs/Rooms/RoomTemplate.prefab";
        const string PlayPrefabSetPath = "Assets/Project/Data/PlayPrefabSet.asset";
        const string TilesDir = "Assets/Project/Art/Tiles";
        const string TrapsDir = "Assets/Project/Prefabs/Traps";
        const string TilesetTexturePath = "Assets/Project/Art/Sprites/Environment/Tilesets/Dungeon_Tileset.png";
        const string StructureTexturePath = "Assets/Project/Art/Sprites/Environment/Tilesets/Structure.png";

        [MenuItem("Tools/Dungeon Gathering/Build Entity Prefabs")]
        public static void BuildAll()
        {
            EnsureDirectories();
            var tiles = BuildDungeonTiles();
            BuildPlayerPrefab();
            BuildCombatPrefabs();
            BuildTrapPrefabs();
            BuildInteractionPrefabs();
            BuildEnemyBaseAndVariants();
            BuildRoomTemplates(tiles);
            BuildPlayPrefabSetAndAssign();
            RegisterAddressables();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EntityPrefabBuilder] Hoàn tất dựng và chuẩn hóa toàn bộ entity, boss, trap, tile & room prefabs!");
        }

        static void EnsureDirectories()
        {
            string[] dirs =
            {
                "Assets/Project/Prefabs/Player",
                "Assets/Project/Prefabs/Enemies",
                "Assets/Project/Prefabs/Enemies/Animations",
                "Assets/Project/Prefabs/Combat",
                "Assets/Project/Prefabs/Rooms",
                "Assets/Project/Prefabs/Traps",
                "Assets/Project/Art/Tiles",
                "Assets/Project/Data"
            };
            foreach (var d in dirs)
            {
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
            }
        }

        // ==========================================
        // 0. DUNGEON TILESET ASSETS
        // ==========================================
        static Sprite GetTilesetSprite(int cx, int cy)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetTexturePath);
            if (tex == null) return null;
            var p = new Vector2(cx * 16 + 8, tex.height - (cy * 16 + 8));
            return AssetDatabase.LoadAllAssetsAtPath(TilesetTexturePath).OfType<Sprite>().FirstOrDefault(s => s.rect.Contains(p));
        }

        static Tile GetOrCreateTile(string fileName, int cx, int cy, Tile.ColliderType colliderType)
        {
            string path = $"{TilesDir}/{fileName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = GetTilesetSprite(cx, cy);
            tile.color = Color.white;
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static Dictionary<(int cx, int cy), Tile> BuildDungeonTiles()
        {
            var map = new Dictionary<(int cx, int cy), Tile>();
            // Tạo các tile sàn (cx: 6..9, cy: 0..2)
            for (int cy = 0; cy <= 2; cy++)
                for (int cx = 6; cx <= 9; cx++)
                    map[(cx, cy)] = GetOrCreateTile($"Tile_Floor_{cx}_{cy}", cx, cy, Tile.ColliderType.None);

            // Tạo các tile tường viền & góc (cx: 0..5, cy: 0..5)
            for (int cy = 0; cy <= 5; cy++)
                for (int cx = 0; cx <= 5; cx++)
                    map[(cx, cy)] = GetOrCreateTile($"Tile_Wall_{cx}_{cy}", cx, cy, Tile.ColliderType.Grid);

            // Tạo các tile trang trí trên sàn/tường
            (int cx, int cy, string name)[] decors =
            {
                (8, 3, "Crate"), (8, 6, "Bones"), (7, 7, "SkullBone"), (4, 7, "Banner"),
                (9, 4, "Rubble1"), (9, 5, "Rubble2"), (2, 9, "Brazier"), (5, 7, "Chalice"),
                (4, 6, "WebL"), (5, 6, "WebR"), (6, 6, "GateL"), (7, 6, "GateR")
            };
            foreach (var (cx, cy, name) in decors)
                map[(cx, cy)] = GetOrCreateTile($"Tile_Decor_{name}", cx, cy, Tile.ColliderType.None);

            // Tạo 3 tile chuẩn cho DungeonConfig (corridorFloorTile, corridorWallTile, sealWallTile)
            GetOrCreateTile("Dungeon_Floor", 6, 0, Tile.ColliderType.None);
            GetOrCreateTile("Dungeon_Wall", 1, 0, Tile.ColliderType.Grid);
            GetOrCreateTile("Dungeon_SealWall", 1, 5, Tile.ColliderType.Grid);

            return map;
        }

        // ==========================================
        // 1. PLAYER PREFAB (ENTITIES §2.1 & §3.5.1)
        // ==========================================
        public static void BuildPlayerPrefab()
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.layer = LayerMask.NameToLayer("Player");

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = root.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 0.8f);
            col.offset = Vector2.zero;
            col.direction = CapsuleDirection2D.Vertical;

            root.AddComponent<PlayerStats>();
            root.AddComponent<PlayerMovement>();
            root.AddComponent<PlayerInputReader>();
            root.AddComponent<StatusEffectController>();
            var skillCtrl = root.AddComponent<PlayerSkillController>();
            var equipment = root.AddComponent<PlayerEquipment>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var playerCtrl = root.AddComponent<PlayerController>();

            // Visual
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(root.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 0;
            var defaultCharSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Characters/Knight/Hero_Knight_Blue_Portrait.png");
            if (defaultCharSprite == null)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Project/Art/Sprites/Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png");
                foreach (var obj in sprites) if (obj is Sprite s) { defaultCharSprite = s; break; }
            }
            if (defaultCharSprite != null) sr.sprite = defaultCharSprite;

            var anim = visualGo.AddComponent<Animator>();
            var defaultController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Project/Prefabs/Player/Knight/Animations/Hero_Knight_Blue.controller");
            if (defaultController != null) anim.runtimeAnimatorController = defaultController;

            var animDriver = visualGo.AddComponent<SpriteAnimDriver>();
            var soAnim = new SerializedObject(animDriver);
            soAnim.FindProperty("animator").objectReferenceValue = anim;
            soAnim.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soAnim.FindProperty("facesRight").boolValue = true;
            soAnim.ApplyModifiedPropertiesWithoutUndo();

            // Shadow
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sortingLayerName = "Shadows";
            shadowSr.sortingOrder = 0;
            shadowSr.color = new Color(0f, 0f, 0f, 0.4f);

            // WeaponPivot & WeaponHolder
            var weaponPivotGo = new GameObject("WeaponPivot");
            weaponPivotGo.transform.SetParent(root.transform, false);
            weaponPivotGo.transform.localPosition = new Vector3(0f, -0.10f, 0f);
            var weaponHolder = weaponPivotGo.AddComponent<WeaponHolder>();

            var socketPrimaryGo = new GameObject("Socket_Primary");
            socketPrimaryGo.transform.SetParent(weaponPivotGo.transform, false);
            socketPrimaryGo.transform.localPosition = new Vector3(0.22f, 0f, 0f);

            var socketSecondaryGo = new GameObject("Socket_Secondary");
            socketSecondaryGo.transform.SetParent(weaponPivotGo.transform, false);
            socketSecondaryGo.transform.localPosition = new Vector3(0.22f, 0f, 0f);

            var soWeapons = new SerializedObject(weaponHolder);
            soWeapons.FindProperty("weaponPivot").objectReferenceValue = weaponPivotGo.transform;
            soWeapons.FindProperty("weaponSocket").objectReferenceValue = socketPrimaryGo.transform;
            soWeapons.FindProperty("socketPrimary").objectReferenceValue = socketPrimaryGo.transform;
            soWeapons.FindProperty("socketSecondary").objectReferenceValue = socketSecondaryGo.transform;
            soWeapons.FindProperty("aimRange").floatValue = 8f;
            soWeapons.FindProperty("weaponSortingOrder").intValue = 10;
            soWeapons.ApplyModifiedPropertiesWithoutUndo();

            // Hurtbox
            var hurtboxGo = new GameObject("Hurtbox");
            hurtboxGo.transform.SetParent(root.transform, false);
            hurtboxGo.layer = LayerMask.NameToLayer("PlayerHurtbox");
            var hurtCol = hurtboxGo.AddComponent<CapsuleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.size = new Vector2(0.5f, 0.8f);
            hurtCol.offset = Vector2.zero;

            // InteractRange
            var interactGo = new GameObject("InteractRange");
            interactGo.transform.SetParent(root.transform, false);
            var interactCol = interactGo.AddComponent<CircleCollider2D>();
            interactCol.isTrigger = true;
            interactCol.radius = 1.2f;

            // FxSockets
            var fxGo = new GameObject("FxSockets");
            fxGo.transform.SetParent(root.transform, false);
            var feetGo = new GameObject("Feet");
            feetGo.transform.SetParent(fxGo.transform, false);
            feetGo.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(fxGo.transform, false);
            bodyGo.transform.localPosition = Vector3.zero;

            // Link references into PlayerController
            var soPlayer = new SerializedObject(playerCtrl);
            soPlayer.FindProperty("weapons").objectReferenceValue = weaponHolder;
            soPlayer.FindProperty("skills").objectReferenceValue = skillCtrl;
            soPlayer.FindProperty("equipment").objectReferenceValue = equipment;
            soPlayer.FindProperty("interactor").objectReferenceValue = interactor;
            soPlayer.FindProperty("anim").objectReferenceValue = animDriver;
            soPlayer.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[EntityPrefabBuilder] Tạo thành công Player.prefab");
        }

        // ==========================================
        // 2. COMBAT PREFABS (ENTITIES §3.1 & §3.5.4)
        // ==========================================
        public static void BuildCombatPrefabs()
        {
            // 2.1 SpawnWarningMarker
            var markerGo = new GameObject("SpawnWarningMarker");
            markerGo.AddComponent<PooledObject>();
            var markerSr = markerGo.AddComponent<SpriteRenderer>();
            markerSr.sortingLayerName = "Default";
            markerSr.sortingOrder = 1;
            markerSr.color = new Color(1f, 0.2f, 0.2f, 0.7f);

            var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/joy_base.png");
            if (circleSprite != null) markerSr.sprite = circleSprite;

            var markerComp = markerGo.AddComponent<SpawnWarningMarker>();
            var soMarker = new SerializedObject(markerComp);
            soMarker.FindProperty("ring").objectReferenceValue = markerSr;
            soMarker.FindProperty("pulseScale").floatValue = 1.25f;
            soMarker.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(markerGo, SpawnMarkerPath);
            Object.DestroyImmediate(markerGo);

            // 2.2 DamagePopup
            var popupGo = new GameObject("DamagePopup");
            popupGo.AddComponent<PooledObject>();
            var tmp = popupGo.AddComponent<TextMeshPro>();
            tmp.text = "0";
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingLayerID = SortingLayer.NameToID("Characters");
            tmp.sortingOrder = 100;

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Art/Fonts/PixelifySans-Regular SDF.asset")
                         ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Art/Fonts/PixelFont SDF.asset");
            if (fontAsset != null) tmp.font = fontAsset;

            var popupComp = popupGo.AddComponent<DamagePopup>();
            var soPopup = new SerializedObject(popupComp);
            soPopup.FindProperty("label").objectReferenceValue = tmp;
            soPopup.FindProperty("normalColor").colorValue = Color.white;
            soPopup.FindProperty("critColor").colorValue = new Color(1f, 0.75f, 0.2f);
            soPopup.FindProperty("hurtColor").colorValue = new Color(1f, 0.3f, 0.3f);
            soPopup.FindProperty("rise").floatValue = 0.8f;
            soPopup.FindProperty("lifetime").floatValue = 0.6f;
            soPopup.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(popupGo, DamagePopupPath);
            Object.DestroyImmediate(popupGo);
        }

        // ==========================================
        // 2.5. TRAP PREFABS (ARCHITECTURE §7)
        // ==========================================
        public static void BuildTrapPrefabs()
        {
            var p1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Traps/SpikeTrap/peaks_1.png");
            var p2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Traps/SpikeTrap/peaks_2.png");
            var p3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Traps/SpikeTrap/peaks_3.png");
            var p4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Traps/SpikeTrap/peaks_4.png");
            var f1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Traps/Flamethrower/flamethrower_1_1.png");
            var f4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Traps/Flamethrower/flamethrower_1_4.png");
            var burnVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/VFX/Status/VFX_Status_BurningAura.prefab");

            // 1. Timed Spike Trap
            {
                var go = new GameObject("Trap_SpikeTimed");
                go.layer = LayerMask.NameToLayer("Trap");
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.85f, 0.85f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 2;
                sr.sprite = p1;
                var trap = go.AddComponent<TimedTrap>();
                var so = new SerializedObject(trap);
                so.FindProperty("visual").objectReferenceValue = sr;
                so.FindProperty("inactiveSprite").objectReferenceValue = p1;
                so.FindProperty("warningSprite").objectReferenceValue = p2;
                so.FindProperty("activeSprite").objectReferenceValue = p4;
                so.FindProperty("damage").floatValue = 1f;
                so.FindProperty("knockback").floatValue = 2f;
                so.FindProperty("inactiveTime").floatValue = 1.8f;
                so.FindProperty("warningTime").floatValue = 0.5f;
                so.FindProperty("activeTime").floatValue = 0.7f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, $"{TrapsDir}/Trap_SpikeTimed.prefab");
                Object.DestroyImmediate(go);
            }

            // 2. Trigger Spike Trap
            {
                var go = new GameObject("Trap_SpikeTrigger");
                go.layer = LayerMask.NameToLayer("Trap");
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.85f, 0.85f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 2;
                sr.sprite = p1;
                var trap = go.AddComponent<TriggerTrap>();
                var so = new SerializedObject(trap);
                so.FindProperty("visual").objectReferenceValue = sr;
                so.FindProperty("inactiveSprite").objectReferenceValue = p1;
                so.FindProperty("warningSprite").objectReferenceValue = p3;
                so.FindProperty("activeSprite").objectReferenceValue = p4;
                so.FindProperty("damage").floatValue = 1f;
                so.FindProperty("warningTime").floatValue = 0.35f;
                so.FindProperty("activeTime").floatValue = 0.55f;
                so.FindProperty("cooldown").floatValue = 1.2f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, $"{TrapsDir}/Trap_SpikeTrigger.prefab");
                Object.DestroyImmediate(go);
            }

            // 3. Flame Hazard Zone
            {
                var go = new GameObject("Trap_FlameHazard");
                go.layer = LayerMask.NameToLayer("Trap");
                var col = go.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.9f, 0.9f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 2;
                sr.sprite = f4 != null ? f4 : f1;
                var trap = go.AddComponent<HazardZone>();
                var so = new SerializedObject(trap);
                so.FindProperty("visual").objectReferenceValue = sr;
                so.FindProperty("inactiveSprite").objectReferenceValue = f1;
                so.FindProperty("activeSprite").objectReferenceValue = f4 != null ? f4 : f1;
                so.FindProperty("dotId").stringValue = "hazard_fire";
                so.FindProperty("dotDps").floatValue = 1f;
                so.FindProperty("lingerDuration").floatValue = 1.5f;
                so.FindProperty("slowPercent").floatValue = 0.25f;
                if (burnVfx != null) so.FindProperty("loopVfx").objectReferenceValue = burnVfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, $"{TrapsDir}/Trap_FlameHazard.prefab");
                Object.DestroyImmediate(go);
            }
        }

        // ==========================================
        // 3. INTERACTION & ROOM PROPS (ENTITIES §1.2 & §3.5.3)
        // ==========================================
        public static void BuildInteractionPrefabs()
        {
            // 3.1 Chest
            var chestGo = new GameObject("Chest");
            var chestCol = chestGo.AddComponent<BoxCollider2D>();
            chestCol.isTrigger = true;
            chestCol.size = new Vector2(1f, 0.8f);

            var chestVisGo = new GameObject("Visual");
            chestVisGo.transform.SetParent(chestGo.transform, false);
            var chestSr = chestVisGo.AddComponent<SpriteRenderer>();
            chestSr.sortingLayerName = "Characters";

            var closedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Items/Chest/chest_1.png");
            var openSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Items/Chest/chest_open_1.png");
            if (closedSprite != null) chestSr.sprite = closedSprite;

            var chestComp = chestGo.AddComponent<Chest>();
            var soChest = new SerializedObject(chestComp);
            soChest.FindProperty("visual").objectReferenceValue = chestSr;
            soChest.FindProperty("closedSprite").objectReferenceValue = closedSprite;
            soChest.FindProperty("openSprite").objectReferenceValue = openSprite;
            soChest.FindProperty("goldAmount").intValue = 15;
            soChest.FindProperty("promptKey").stringValue = "hud.open_chest";
            soChest.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(chestGo, ChestPrefabPath);
            Object.DestroyImmediate(chestGo);

            // 3.2 ShopStand
            var standGo = new GameObject("ShopStand");
            var standCol = standGo.AddComponent<BoxCollider2D>();
            standCol.isTrigger = true;
            standCol.size = new Vector2(2f, 1f);

            var crateSprite = GetTilesetSprite(8, 3);
            var slotRenderers = new SpriteRenderer[3];
            float[] slotOffsets = { -0.8f, 0f, 0.8f };
            for (int i = 0; i < 3; i++)
            {
                var tableGo = new GameObject($"Table_{i}");
                tableGo.transform.SetParent(standGo.transform, false);
                tableGo.transform.localPosition = new Vector3(slotOffsets[i], -0.1f, 0f);
                var tableSr = tableGo.AddComponent<SpriteRenderer>();
                tableSr.sortingLayerName = "Characters";
                tableSr.sortingOrder = 0;
                tableSr.sprite = crateSprite;

                var slotGo = new GameObject($"Slot_{i}");
                slotGo.transform.SetParent(standGo.transform, false);
                slotGo.transform.localPosition = new Vector3(slotOffsets[i], 0.45f, 0f);
                var srSlot = slotGo.AddComponent<SpriteRenderer>();
                srSlot.sortingLayerName = "Characters";
                srSlot.sortingOrder = 2;
                slotRenderers[i] = srSlot;
            }

            var potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Items/Potions/flasks_4_1.png");
            if (potionSprite == null)
            {
                var itemSprites = AssetDatabase.FindAssets("t:Sprite flasks", new[] { "Assets/Project/Art/Sprites/Items" });
                if (itemSprites.Length > 0)
                    potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(itemSprites[0]));
            }

            var standComp = standGo.AddComponent<ShopStand>();
            var soStand = new SerializedObject(standComp);
            var slotsProp = soStand.FindProperty("slotIcons");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; i++) slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotRenderers[i];
            if (potionSprite != null) soStand.FindProperty("potionIcon").objectReferenceValue = potionSprite;
            soStand.FindProperty("potionPrice").intValue = 15;
            soStand.FindProperty("promptKey").stringValue = "hud.shop";
            soStand.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(standGo, ShopStandPrefabPath);
            Object.DestroyImmediate(standGo);

            // 3.3 Portal
            var portalGo = new GameObject("Portal");
            var portalCol = portalGo.AddComponent<CircleCollider2D>();
            portalCol.isTrigger = true;
            portalCol.radius = 0.8f;

            var portalVisGo = new GameObject("Visual");
            portalVisGo.transform.SetParent(portalGo.transform, false);
            var portalSr = portalVisGo.AddComponent<SpriteRenderer>();
            portalSr.sortingLayerName = "Characters";
            var portalAnimPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/VFX/Arcane/VFX_Arcane_PortalSummon.prefab");
            if (portalAnimPrefab != null)
            {
                var visSr = portalAnimPrefab.GetComponentInChildren<SpriteRenderer>();
                if (visSr != null) portalSr.sprite = visSr.sprite;
                var visAnim = portalAnimPrefab.GetComponentInChildren<Animator>();
                if (visAnim != null)
                {
                    var animComp = portalVisGo.AddComponent<Animator>();
                    animComp.runtimeAnimatorController = visAnim.runtimeAnimatorController;
                }
            }

            var portalComp = portalGo.AddComponent<Portal>();
            var soPortal = new SerializedObject(portalComp);
            soPortal.FindProperty("promptKey").stringValue = "hud.portal";
            soPortal.FindProperty("enterOnTouch").boolValue = true;
            soPortal.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(portalGo, PortalPrefabPath);
            Object.DestroyImmediate(portalGo);

            // 3.4 RoomDoor (rộng 3 ô hành lang, cụm 3 cột đá Structure)
            BuildRoomDoorPrefab();
        }

        [MenuItem("Tools/Dungeon Gathering/Build Room Door Prefab")]
        public static void BuildRoomDoorPrefab()
        {
            var doorGo = new GameObject("RoomDoor");
            doorGo.layer = LayerMask.NameToLayer("Wall");

            var doorBlocker = doorGo.AddComponent<BoxCollider2D>();
            doorBlocker.size = new Vector2(3f, 1f);
            doorBlocker.isTrigger = false;

            var pillarSprite = AssetDatabase.LoadAllAssetsAtPath(StructureTexturePath).OfType<Sprite>().FirstOrDefault();

            var pillars = new Transform[3];
            var srs = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                float offset = i - 1; // -1, 0, 1
                var p = new GameObject($"Pillar_{i}");
                p.transform.SetParent(doorGo.transform, false);
                p.transform.localPosition = new Vector3(offset, 0.5f, 0f);
                var sr = p.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = "Characters";
                sr.sortingOrder = 10;
                sr.sprite = pillarSprite;

                pillars[i] = p.transform;
                srs[i] = sr;
            }

            var doorComp = doorGo.AddComponent<RoomDoor>();
            var soDoor = new SerializedObject(doorComp);
            soDoor.FindProperty("blocker").objectReferenceValue = doorBlocker;
            var arrPillars = soDoor.FindProperty("pillars");
            arrPillars.arraySize = 3;
            var arrRenderers = soDoor.FindProperty("renderers");
            arrRenderers.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                arrPillars.GetArrayElementAtIndex(i).objectReferenceValue = pillars[i];
                arrRenderers.GetArrayElementAtIndex(i).objectReferenceValue = srs[i];
            }
            soDoor.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(doorGo, RoomDoorPrefabPath);
            Object.DestroyImmediate(doorGo);
            Debug.Log("[EntityPrefabBuilder] Đã dựng lại RoomDoor.prefab với 3 cột đá Structure.");
        }

        // ==========================================
        // 4. ENEMIES, SKULL & BOSS VARIANTS (ENTITIES §3.1 & §3.5.2)
        // ==========================================
        public static void BuildEnemyBaseAndVariants()
        {
            // 4.1 Build Enemy_Base
            var root = new GameObject("Enemy_Base");
            root.tag = "Enemy";
            root.layer = LayerMask.NameToLayer("Enemy");

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            var col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            root.AddComponent<EnemyHealth>();
            // NavMeshAgent (AI Navigation): chỉ tìm đường + né nhau, vị trí do Rigidbody2D — EnemyMotor cấu hình & bật lúc spawn.
            var navAgent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;
            navAgent.updateUpAxis = false;
            navAgent.radius = EnemyNavigation.AgentRadius;
            navAgent.height = EnemyNavigation.AgentHeight;
            navAgent.enabled = false;
            root.AddComponent<EnemyMotor>();
            root.AddComponent<EnemyBrain>();
            root.AddComponent<EnemyAttackRunner>();
            root.AddComponent<StatusEffectController>();
            root.AddComponent<PooledObject>();
            var enemy = root.AddComponent<Enemy>();

            // Visual
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(root.transform, false);
            var sr = visualGo.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Characters";
            sr.sortingOrder = 0;
            var anim = visualGo.AddComponent<Animator>();
            var animDriver = visualGo.AddComponent<SpriteAnimDriver>();
            var soDriver = new SerializedObject(animDriver);
            soDriver.FindProperty("animator").objectReferenceValue = anim;
            soDriver.FindProperty("spriteRenderer").objectReferenceValue = sr;
            soDriver.FindProperty("facesRight").boolValue = true;
            soDriver.ApplyModifiedPropertiesWithoutUndo();

            // Shadow
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(root.transform, false);
            shadowGo.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sortingLayerName = "Shadows";
            shadowSr.color = new Color(0f, 0f, 0f, 0.4f);

            // Hurtbox
            var hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(root.transform, false);
            hurtGo.layer = LayerMask.NameToLayer("EnemyHurtbox");
            var hurtCol = hurtGo.AddComponent<CircleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.radius = 0.45f;

            // AttackOrigin & StatusFx
            var attackOriginGo = new GameObject("AttackOrigin");
            attackOriginGo.transform.SetParent(root.transform, false);

            var statusFxGo = new GameObject("StatusFx");
            statusFxGo.transform.SetParent(root.transform, false);
            statusFxGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            // Link Enemy
            var soEnemy = new SerializedObject(enemy);
            soEnemy.FindProperty("attackOrigin").objectReferenceValue = attackOriginGo.transform;
            soEnemy.FindProperty("anim").objectReferenceValue = animDriver;
            var disableArray = soEnemy.FindProperty("disableOnDeath");
            disableArray.arraySize = 2;
            disableArray.GetArrayElementAtIndex(0).objectReferenceValue = col;
            disableArray.GetArrayElementAtIndex(1).objectReferenceValue = hurtCol;
            soEnemy.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, EnemyBasePath);
            Object.DestroyImmediate(root);

            // 4.2 Tạo Animator Controller cho Skull (từ skull_v1_1..4 và skull_v2_1..4)
            string skullCtrlPath = BuildSkullAnimatorController();

            // 4.3 Cập nhật 5 Enemy Prefabs thường
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Skeleton_Spear.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-skeleton1.controller", "skeleton-spear", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Skeleton_Warrior.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-skeleton2.controller", "skeleton-warrior", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Vampire.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-vampire.controller", "vampire", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Zombie.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/1Zombie.controller", "zombie", 1f, false);
            ConfigureEnemyVariant("Assets/Project/Prefabs/Enemies/Enemy_Skull.prefab",
                skullCtrlPath, "skull", 1f, false);

            // 4.4 Tạo 3 Boss Prefabs (Boss_EliteWarrior, Boss_EliteVampire, Boss_VampireLord)
            ConfigureBossVariant("Assets/Project/Prefabs/Enemies/Boss_EliteWarrior.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-skeleton2.controller",
                "elite-warrior", 1.65f, new Color(1f, 0.85f, 0.75f, 1f),
                "Assets/Project/Prefabs/VFX/Explosions/VFX_Explosions_FireNova.prefab");

            ConfigureBossVariant("Assets/Project/Prefabs/Enemies/Boss_EliteVampire.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-vampire.controller",
                "elite-vampire", 1.65f, new Color(0.9f, 0.75f, 1f, 1f),
                "Assets/Project/Prefabs/VFX/Shadow/VFX_Shadow_VoidImplosion.prefab");

            ConfigureBossVariant("Assets/Project/Prefabs/Enemies/Boss_VampireLord.prefab",
                "Assets/Project/Prefabs/Enemies/Animations/enemies-vampire.controller",
                "vampire-lord", 2.1f, new Color(1f, 0.65f, 0.7f, 1f),
                "Assets/Project/Prefabs/VFX/Explosions/VFX_Explosions_ArcaneBurst.prefab");
        }

        static string BuildSkullAnimatorController()
        {
            const string dir = "Assets/Project/Prefabs/Enemies/Animations/";
            const string ctrlPath = dir + "enemies-skull.controller";
            var v1 = new List<Sprite>();
            var v2 = new List<Sprite>();
            for (int i = 1; i <= 4; i++)
            {
                var s1 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Art/Sprites/Enemies/DungeonMonsters/skull/v1/skull_v1_{i}.png");
                var s2 = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Project/Art/Sprites/Enemies/DungeonMonsters/skull/v2/skull_v2_{i}.png");
                if (s1 != null) v1.Add(s1);
                if (s2 != null) v2.Add(s2);
            }
            if (v1.Count == 0) return ctrlPath;
            if (v2.Count == 0) v2.AddRange(v1);

            AnimationClip Make(string name, List<Sprite> frames, float fps, bool loop)
            {
                string p = dir + name + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
                bool isNew = clip == null;
                if (isNew) clip = new AnimationClip { name = name };
                clip.frameRate = fps;
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var keys = new ObjectReferenceKeyframe[frames.Count + 1];
                for (int i = 0; i < frames.Count; i++)
                    keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
                keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / fps, value = frames[^1] };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                if (isNew) AssetDatabase.CreateAsset(clip, p);
                else EditorUtility.SetDirty(clip);
                return clip;
            }

            var idle = Make("enemies-skull_idle", v1, 8f, true);
            var move = Make("enemies-skull_movement", v1, 10f, true);
            var atk = Make("enemies-skull_attack", v2, 12f, false);
            var hurt = Make("enemies-skull_take_damage", v2.Take(2).ToList(), 10f, false);
            var death = Make("enemies-skull_death", v2, 8f, false);

            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
                var sm = ctrl.layers[0].stateMachine;
                foreach (var c in new[] { idle, move, atk, hurt, death })
                {
                    var st = sm.AddState(c.name);
                    st.motion = c;
                    if (c == idle) sm.defaultState = st;
                }
            }
            return ctrlPath;
        }

        static void ConfigureEnemyVariant(string prefabPath, string controllerPath, string enemyId, float scale, bool isBoss)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePath);
            if (basePrefab == null) return;

            var go = Object.Instantiate(basePrefab);
            go.name = Path.GetFileNameWithoutExtension(prefabPath);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var anim = go.GetComponentInChildren<Animator>();
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (anim != null && ctrl != null) anim.runtimeAnimatorController = ctrl;

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (ctrl != null && ctrl.animationClips.Length > 0 && sr != null)
            {
                var clip = ctrl.animationClips[0];
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                if (bindings.Length > 0)
                {
                    var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                    if (keyframes != null && keyframes.Length > 0 && keyframes[0].value is Sprite s)
                        sr.sprite = s;
                }
            }

            // Gán EnemyData nếu đã có trong Assets/Project/Data/Enemies/
            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/Enemy_{enemyId}.asset");
            var enemyComp = go.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                var so = new SerializedObject(enemyComp);
                so.FindProperty("isBoss").boolValue = isBoss;
                if (enemyData != null) so.FindProperty("data").objectReferenceValue = enemyData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static void ConfigureBossVariant(string prefabPath, string controllerPath, string bossId, float scale, Color tint, string vfxPath)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyBasePath);
            if (basePrefab == null) return;

            var go = Object.Instantiate(basePrefab);
            go.name = Path.GetFileNameWithoutExtension(prefabPath);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            // Boss không qua PoolManager (ENTITIES §3.1)
            var pooled = go.GetComponent<PooledObject>();
            if (pooled != null) Object.DestroyImmediate(pooled);

            var anim = go.GetComponentInChildren<Animator>();
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            if (anim != null && ctrl != null) anim.runtimeAnimatorController = ctrl;

            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = tint;
                if (ctrl != null && ctrl.animationClips.Length > 0)
                {
                    var clip = ctrl.animationClips[0];
                    var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                    if (bindings.Length > 0)
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                        if (keyframes != null && keyframes.Length > 0 && keyframes[0].value is Sprite s)
                            sr.sprite = s;
                    }
                }
            }

            var enemyData = AssetDatabase.LoadAssetAtPath<EnemyData>($"Assets/Project/Data/Enemies/Enemy_{bossId}.asset");
            var enemyComp = go.GetComponent<Enemy>();
            if (enemyComp != null)
            {
                var so = new SerializedObject(enemyComp);
                so.FindProperty("isBoss").boolValue = true;
                if (enemyData != null) so.FindProperty("data").objectReferenceValue = enemyData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var phaseCtrl = go.AddComponent<BossPhaseController>();
            var transVfx = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPath);
            var soPhase = new SerializedObject(phaseCtrl);
            soPhase.FindProperty("bossId").stringValue = bossId;
            var phasesProp = soPhase.FindProperty("phases");
            phasesProp.arraySize = 2;

            var p0 = phasesProp.GetArrayElementAtIndex(0);
            p0.FindPropertyRelative("hpThreshold").floatValue = 0.6f;
            p0.FindPropertyRelative("speedMultiplier").floatValue = 1.25f;
            p0.FindPropertyRelative("transitionInvulnerable").floatValue = 0.8f;
            if (transVfx != null) p0.FindPropertyRelative("transitionVfx").objectReferenceValue = transVfx;

            var p1 = phasesProp.GetArrayElementAtIndex(1);
            p1.FindPropertyRelative("hpThreshold").floatValue = 0.3f;
            p1.FindPropertyRelative("speedMultiplier").floatValue = 1.5f;
            p1.FindPropertyRelative("transitionInvulnerable").floatValue = 1.0f;
            if (transVfx != null) p1.FindPropertyRelative("transitionVfx").objectReferenceValue = transVfx;

            // Nạp đòn tấn công nâng cao cho phase 2 của Vampire Lord nếu đã có
            if (bossId == "vampire-lord")
            {
                var spiral = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-spiral.asset");
                var summon = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-summon.asset");
                var fan = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-fan.asset");
                var charge = AssetDatabase.LoadAssetAtPath<EnemyAttackSO>("Assets/Project/Data/EnemyAttacks/Atk_lord-charge.asset");
                if (fan != null && charge != null && summon != null)
                {
                    var a0 = p0.FindPropertyRelative("attacks");
                    a0.arraySize = 3;
                    a0.GetArrayElementAtIndex(0).objectReferenceValue = fan;
                    a0.GetArrayElementAtIndex(1).objectReferenceValue = charge;
                    a0.GetArrayElementAtIndex(2).objectReferenceValue = summon;
                }
                if (spiral != null && fan != null && charge != null && summon != null)
                {
                    var a1 = p1.FindPropertyRelative("attacks");
                    a1.arraySize = 4;
                    a1.GetArrayElementAtIndex(0).objectReferenceValue = spiral;
                    a1.GetArrayElementAtIndex(1).objectReferenceValue = fan;
                    a1.GetArrayElementAtIndex(2).objectReferenceValue = charge;
                    a1.GetArrayElementAtIndex(3).objectReferenceValue = summon;
                }
            }

            soPhase.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        // ==========================================
        // 5. ROOM TEMPLATES (ENTITIES §3.5.3)
        // ==========================================
        public static void BuildRoomTemplates(Dictionary<(int cx, int cy), Tile> tiles)
        {
            BuildSingleRoom(RoomTemplatePath, "RoomTemplate", RoomType.Combat, new Vector2Int(20, 14), tiles, true, true);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Start.prefab", "Room_Start", RoomType.Start, new Vector2Int(20, 14), tiles, false, false);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Combat.prefab", "Room_Combat", RoomType.Combat, new Vector2Int(20, 14), tiles, true, true);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Treasure.prefab", "Room_Treasure", RoomType.Treasure, new Vector2Int(20, 14), tiles, false, false);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Shop.prefab", "Room_Shop", RoomType.Shop, new Vector2Int(20, 14), tiles, false, false);
            BuildSingleRoom("Assets/Project/Prefabs/Rooms/Room_Boss.prefab", "Room_Boss", RoomType.Boss, new Vector2Int(24, 18), tiles, true, true);
        }

        static void BuildSingleRoom(string path, string name, RoomType type, Vector2Int size,
            Dictionary<(int cx, int cy), Tile> tiles, bool addObstacles, bool addTraps)
        {
            var root = new GameObject(name);
            var template = root.AddComponent<RoomTemplate>();
            template.roomType = type;
            template.size = size;

            // Grid
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root.transform, false);
            gridGo.AddComponent<Grid>();

            // Ground
            var groundGo = new GameObject("Ground");
            groundGo.transform.SetParent(gridGo.transform, false);
            template.ground = groundGo.AddComponent<Tilemap>();
            var groundRend = groundGo.AddComponent<TilemapRenderer>();
            groundRend.sortingLayerName = "Default";
            groundRend.sortingOrder = -10;

            // Wall
            var wallGo = new GameObject("Wall");
            wallGo.transform.SetParent(gridGo.transform, false);
            wallGo.layer = LayerMask.NameToLayer("Wall");
            template.wall = wallGo.AddComponent<Tilemap>();
            var wallRend = wallGo.AddComponent<TilemapRenderer>();
            wallRend.sortingLayerName = "Default";
            wallRend.sortingOrder = -5;
            wallGo.AddComponent<TilemapCollider2D>();

            // Decor
            var decorGo = new GameObject("Decor");
            decorGo.transform.SetParent(gridGo.transform, false);
            template.decor = decorGo.AddComponent<Tilemap>();
            var decorRend = decorGo.AddComponent<TilemapRenderer>();
            decorRend.sortingLayerName = "Default";
            decorRend.sortingOrder = -2;

            // Traps
            var trapsGo = new GameObject("Traps");
            trapsGo.transform.SetParent(gridGo.transform, false);
            trapsGo.layer = LayerMask.NameToLayer("Trap");
            template.traps = trapsGo.AddComponent<Tilemap>();
            var trapsRend = trapsGo.AddComponent<TilemapRenderer>();
            trapsRend.sortingLayerName = "Default";
            trapsRend.sortingOrder = -1;

            int hx = size.x / 2;
            int hy = size.y / 2;

            // Vẽ sàn & tường (chừa 4 khe cửa rộng 3 ô [-1..1] ở chính giữa mỗi cạnh để khớp hành lang và Seal)
            var rng = new System.Random(name.GetHashCode());
            (int cx, int cy)[] floorCoords =
            {
                (6, 0), (7, 0), (8, 0), (9, 0),
                (6, 1), (7, 1), (8, 1), (9, 1),
                (6, 2), (7, 2), (8, 2), (9, 2)
            };

            for (int y = -hy; y <= hy; y++)
            {
                for (int x = -hx; x <= hx; x++)
                {
                    var fCoord = rng.NextDouble() < 0.6 ? (6, 0) : floorCoords[rng.Next(floorCoords.Length)];
                    if (tiles.TryGetValue(fCoord, out var ft))
                        template.ground.SetTile(new Vector3Int(x, y, 0), ft);

                    bool isTop = y == hy;
                    bool isBot = y == -hy;
                    bool isLeft = x == -hx;
                    bool isRight = x == hx;
                    if (!isTop && !isBot && !isLeft && !isRight) continue;

                    (int cx, int cy) wCoord;
                    if (isTop && isLeft) wCoord = (0, 0);
                    else if (isTop && isRight) wCoord = (5, 0);
                    else if (isBot && isLeft) wCoord = (0, 4);
                    else if (isBot && isRight) wCoord = (5, 4);
                    else if (isTop) wCoord = (1 + Mathf.Abs(x) % 4, 0);
                    else if (isBot) wCoord = (1 + Mathf.Abs(x) % 4, 4);
                    else if (isLeft) wCoord = (0, 1 + Mathf.Abs(y) % 3);
                    else wCoord = (5, 1 + Mathf.Abs(y) % 3);

                    if (tiles.TryGetValue(wCoord, out var wt))
                        template.wall.SetTile(new Vector3Int(x, y, 0), wt);
                }
            }

            // Vật cản nội bộ (tile tường → bị trừ khỏi NavMesh của quái, quái đi vòng)
            if (addObstacles && tiles.TryGetValue((1, 5), out var blockTile))
            {
                int ox = hx - 5;
                int oy = hy - 4;
                Vector2Int[] pillars = { new(-ox, oy), new(ox - 1, oy), new(-ox, -oy), new(ox - 1, -oy) };
                foreach (var p in pillars)
                {
                    template.wall.SetTile(new Vector3Int(p.x, p.y, 0), blockTile);
                    template.wall.SetTile(new Vector3Int(p.x + 1, p.y, 0), blockTile);
                }
            }

            // Trang trí (Decor tile + Prop Đuốc/Bình)
            if (tiles.TryGetValue((8, 6), out var bones))
                template.decor.SetTile(new Vector3Int(-hx + 3, -hy + 2, 0), bones);
            if (tiles.TryGetValue((9, 4), out var rubble))
                template.decor.SetTile(new Vector3Int(hx - 3, hy - 3, 0), rubble);

            var torchWall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Rooms/Props/Prop_Torch_Wall.prefab");
            var vaseProp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Rooms/Props/Prop_Vase_Shining.prefab");
            if (torchWall != null)
            {
                foreach (var tx in new[] { -4f, 4f })
                {
                    var tGo = (GameObject)PrefabUtility.InstantiatePrefab(torchWall, decorGo.transform);
                    tGo.transform.localPosition = new Vector3(tx, hy - 1.2f, 0f);
                }
            }
            if (vaseProp != null && type is RoomType.Start or RoomType.Treasure or RoomType.Shop)
            {
                foreach (var vx in new[] { -hx + 2.5f, hx - 2.5f })
                {
                    var vGo = (GameObject)PrefabUtility.InstantiatePrefab(vaseProp, decorGo.transform);
                    vGo.transform.localPosition = new Vector3(vx, hy - 2.2f, 0f);
                }
            }

            // Đặt Bẫy sàn trong phòng Combat / Boss
            if (addTraps)
            {
                var spikeTimed = AssetDatabase.LoadAssetAtPath<GameObject>($"{TrapsDir}/Trap_SpikeTimed.prefab");
                var spikeTrigger = AssetDatabase.LoadAssetAtPath<GameObject>($"{TrapsDir}/Trap_SpikeTrigger.prefab");
                if (spikeTimed != null)
                {
                    foreach (var pos in new[] { new Vector3(-2.5f, 0f, 0f), new Vector3(2.5f, 0f, 0f) })
                    {
                        var tr = (GameObject)PrefabUtility.InstantiatePrefab(spikeTimed, trapsGo.transform);
                        tr.transform.localPosition = pos;
                    }
                }
                if (spikeTrigger != null && type == RoomType.Boss)
                {
                    foreach (var pos in new[] { new Vector3(0f, -3.5f, 0f), new Vector3(0f, 3.5f, 0f) })
                    {
                        var tr = (GameObject)PrefabUtility.InstantiatePrefab(spikeTrigger, trapsGo.transform);
                        tr.transform.localPosition = pos;
                    }
                }
            }

            // DoorSockets chính giữa các cạnh (x=0 với N/S, y=0 với E/W)
            var doorSocketsGo = new GameObject("DoorSockets");
            doorSocketsGo.transform.SetParent(root.transform, false);

            var dn = new GameObject("DoorNorth");
            dn.transform.SetParent(doorSocketsGo.transform, false);
            dn.transform.localPosition = new Vector3(0.5f, hy, 0f);
            template.doorNorth = dn.transform;

            var de = new GameObject("DoorEast");
            de.transform.SetParent(doorSocketsGo.transform, false);
            de.transform.localPosition = new Vector3(hx, 0.5f, 0f);
            template.doorEast = de.transform;

            var ds = new GameObject("DoorSouth");
            ds.transform.SetParent(doorSocketsGo.transform, false);
            ds.transform.localPosition = new Vector3(0.5f, -hy, 0f);
            template.doorSouth = ds.transform;

            var dw = new GameObject("DoorWest");
            dw.transform.SetParent(doorSocketsGo.transform, false);
            dw.transform.localPosition = new Vector3(-hx, 0.5f, 0f);
            template.doorWest = dw.transform;

            // Points
            var pointsGo = new GameObject("Points");
            pointsGo.transform.SetParent(root.transform, false);

            var pSpawn = new GameObject("PlayerSpawn");
            pSpawn.transform.SetParent(pointsGo.transform, false);
            template.playerSpawn = pSpawn.transform;

            var cPoint = new GameObject("ChestPoint");
            cPoint.transform.SetParent(pointsGo.transform, false);
            cPoint.transform.localPosition = type == RoomType.Treasure ? Vector3.zero : new Vector3(0f, 2f, 0f);
            template.chestPoint = cPoint.transform;

            var sPoint = new GameObject("ShopPoint");
            sPoint.transform.SetParent(pointsGo.transform, false);
            template.shopPoint = sPoint.transform;

            var bPoint = new GameObject("BossPoint");
            bPoint.transform.SetParent(pointsGo.transform, false);
            bPoint.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            template.bossPoint = bPoint.transform;

            var portalPoint = new GameObject("PortalPoint");
            portalPoint.transform.SetParent(pointsGo.transform, false);
            portalPoint.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            template.portalPoint = portalPoint.transform;

            // SpawnPoints (4 điểm góc trong phòng)
            Vector2[] spawnOffsets = { new(-hx + 4f, hy - 3.5f), new(hx - 4f, hy - 3.5f), new(-hx + 4f, -hy + 3.5f), new(hx - 4f, -hy + 3.5f) };
            for (int i = 0; i < spawnOffsets.Length; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(pointsGo.transform, false);
                sp.transform.localPosition = (Vector3)spawnOffsets[i];
                template.spawnPoints.Add(sp.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ==========================================
        // 6. PLAY PREFAB SET & LEVEL DIRECTOR LINK
        // ==========================================
        public static void BuildPlayPrefabSetAndAssign()
        {
            var set = AssetDatabase.LoadAssetAtPath<PlayPrefabSet>(PlayPrefabSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<PlayPrefabSet>();
                AssetDatabase.CreateAsset(set, PlayPrefabSetPath);
            }

            var playerGo = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerGo != null) set.player = playerGo.GetComponent<PlayerController>();

            var popupGo = AssetDatabase.LoadAssetAtPath<GameObject>(DamagePopupPath);
            if (popupGo != null) set.damagePopup = popupGo.GetComponent<DamagePopup>();

            var hitVfx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/VFX/Impacts/VFX_Impacts_HitSpark.prefab");
            set.defaultHitVfx = hitVfx;

            var markerGo = AssetDatabase.LoadAssetAtPath<GameObject>(SpawnMarkerPath);
            if (markerGo != null) set.spawnMarker = markerGo.GetComponent<SpawnWarningMarker>();

            var goldGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Loot/Loot_Coin_Gold.prefab");
            if (goldGo != null) set.goldCoin = goldGo.GetComponent<LootDrop>();

            var blueGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Loot/Loot_Coin_Blue.prefab");
            if (blueGo != null) set.blueCoin = blueGo.GetComponent<LootDrop>();

            var manaGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Loot/Loot_ManaOrb.prefab");
            if (manaGo != null) set.manaOrb = manaGo.GetComponent<LootDrop>();

            var hpGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Loot/Loot_HealthPotion.prefab");
            if (hpGo != null) set.healthPotion = hpGo.GetComponent<LootDrop>();

            var itemPickupGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/Prefabs/Loot/ItemPickup.prefab");
            if (itemPickupGo != null) set.itemPickup = itemPickupGo.GetComponent<ItemPickup>();

            var doorGo = AssetDatabase.LoadAssetAtPath<GameObject>(RoomDoorPrefabPath);
            if (doorGo != null) set.door = doorGo.GetComponent<RoomDoor>();

            var chestGo = AssetDatabase.LoadAssetAtPath<GameObject>(ChestPrefabPath);
            if (chestGo != null) set.chest = chestGo.GetComponent<Chest>();

            var standGo = AssetDatabase.LoadAssetAtPath<GameObject>(ShopStandPrefabPath);
            if (standGo != null) set.shopStand = standGo.GetComponent<ShopStand>();

            var portalGo = AssetDatabase.LoadAssetAtPath<GameObject>(PortalPrefabPath);
            if (portalGo != null) set.portal = portalGo.GetComponent<Portal>();

            EditorUtility.SetDirty(set);
            AssignToLevelDirector(set);
        }

        static void AssignToLevelDirector(PlayPrefabSet set)
        {
            var directors = Object.FindObjectsByType<LevelDirector>(FindObjectsSortMode.None);
            if (directors != null && directors.Length > 0)
            {
                foreach (var d in directors)
                {
                    var so = new SerializedObject(d);
                    so.FindProperty("prefabs").objectReferenceValue = set;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(d);
                }
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }
        }

        static void RegisterAddressables()
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            var group = settings.DefaultGroup;

            var playerGuid = AssetDatabase.AssetPathToGUID(PlayerPrefabPath);
            if (!string.IsNullOrEmpty(playerGuid))
            {
                var entry = settings.CreateOrMoveEntry(playerGuid, group);
                if (entry != null) entry.address = AddressKeys.PlayerShell;
            }

            var setGuid = AssetDatabase.AssetPathToGUID(PlayPrefabSetPath);
            if (!string.IsNullOrEmpty(setGuid))
            {
                var entry = settings.CreateOrMoveEntry(setGuid, group);
                if (entry != null) entry.address = AddressKeys.PlayPrefabSet;
            }
        }
    }
}
