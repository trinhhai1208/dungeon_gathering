using System.IO;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using DungeonGathering.Play;
using UnityEditor;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Builder tự động sinh và chuẩn hóa các Prefab thực thể (ENTITIES_PREFABS.md):
    /// Tileset, Player, Enemy_Base + 5 Enemy Variants + 3 Boss Prefabs, Traps,
    /// Combat FX (SpawnWarningMarker, DamagePopup), Interaction (Chest, ShopStand, Portal, RoomDoor),
    /// RoomTemplate + 5 Room Variants (Start, Combat, Treasure, Shop, Boss), và PlayPrefabSet.
    /// (Đã tách phân hệ bằng partial class: PlayerCombat, Enemies, Rooms - SYS-04-COR).
    /// </summary>
    public static partial class EntityPrefabBuilder
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
                "Assets/Project/Prefabs/Rooms/Props",
                "Assets/Project/Prefabs/Traps",
                "Assets/Project/Art/Tiles",
                "Assets/Project/Data"
            };
            foreach (var d in dirs)
                if (!Directory.Exists(d)) Directory.CreateDirectory(d);
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
