using System.Linq;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using DungeonGathering.Play;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    public static partial class EntityPrefabBuilder
    {
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

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Art/Fonts/Pixelta SDF.asset");
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
    }
}
