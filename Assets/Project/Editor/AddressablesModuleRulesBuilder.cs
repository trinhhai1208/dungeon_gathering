using System.Collections.Generic;
using System.IO;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// M2 Tool (docs/MODULES_ADDRESSABLES_WEBGL.md §6, §9.3, §11 M2):
    /// Tạo/cập nhật ModuleRegistry, ModuleDefinition, LocalizationFontConfig (Core/Data),
    /// tạo các Addressables Group chuẩn (Local LZ4, AppendHash) và gán đầy đủ address + label từ AddressKeys.
    /// </summary>
    public static class AddressablesModuleRulesBuilder
    {
        const string CoreDataDir = "Assets/Project/Core/Data";
        const string ModulesDir = CoreDataDir + "/Modules";
        const string RegistryPath = CoreDataDir + "/ModuleRegistry.asset";
        const string CoreFontConfigPath = CoreDataDir + "/LocalizationFontConfig.asset";
        const string CoreSfxLibraryPath = CoreDataDir + "/SfxLibrary.asset";
        const string SystemsPrefabPath = "Assets/Project/Core/Prefabs/Systems.prefab";

        [MenuItem("Tools/Dungeon Gathering/Addressables/Apply Module Rules")]
        public static void ApplyRules()
        {
            EnsureFolder(CoreDataDir);
            EnsureFolder(ModulesDir);

            var registry = BuildModuleRegistry();
            LinkSystemsPrefab(registry);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[Addressables] Không khởi tạo được AddressableAssetSettings.");
                return;
            }

            // Đảm bảo danh sách nhãn chuẩn
            string[] allLabels =
            {
                AddressKeys.LabelCore, AddressKeys.LabelSharedUi,
                "menu", "lobby", "loadout", "shop", "catalog", "character",
                "play", "results", "theme-dungeon", "enemies-dungeon", "boss-vampire-lord",
                "vfx-melee", "vfx-elemental",
                "bgm-menu", "bgm-lobby", "bgm-play", "bgm-boss"
            };
            foreach (var lb in allLabels) settings.AddLabel(lb);

            // Tạo/cập nhật các group
            var gCore      = EnsureGroup(settings, "Core_Local",       BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gSharedUi  = EnsureGroup(settings, "Shared_UI",        BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gMenu      = EnsureGroup(settings, "Mod_Menu",         BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gLobby     = EnsureGroup(settings, "Mod_Lobby",        BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gLoadout   = EnsureGroup(settings, "Mod_Loadout",      BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gShop      = EnsureGroup(settings, "Mod_Shop",         BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gPlay      = EnsureGroup(settings, "Mod_Play",         BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gResults   = EnsureGroup(settings, "Mod_Results",      BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gCatalog   = EnsureGroup(settings, "Catalog_Items",    BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gTheme     = EnsureGroup(settings, "Theme_Dungeon",    BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            var gEnemies   = EnsureGroup(settings, "Enemies_Dungeon",  BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            var gBoss      = EnsureGroup(settings, "Boss_VampireLord", BundledAssetGroupSchema.BundlePackingMode.PackSeparately);
            var gVfx       = EnsureGroup(settings, "VFX_Combat",       BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
            var gBgm       = EnsureGroup(settings, "Audio_BGM",        BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

            int registered = 0;
            void Reg(AddressableAssetGroup grp, string assetPath, string address, params string[] labels)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"[Addressables] Bỏ qua file chưa tồn tại: {assetPath}");
                    return;
                }
                var entry = settings.CreateOrMoveEntry(guid, grp, false, false);
                entry.address = address;
                foreach (var lb in labels)
                    if (!string.IsNullOrEmpty(lb)) entry.SetLabel(lb, true, true, false);
                registered++;
            }

            // 1. Core_Local
            Reg(gCore, RegistryPath, AddressKeys.CoreModuleRegistry, AddressKeys.LabelCore);
            Reg(gCore, CoreFontConfigPath, AddressKeys.CoreFontConfig, AddressKeys.LabelCore);
            Reg(gCore, CoreSfxLibraryPath, AddressKeys.CoreSfxLibrary, AddressKeys.LabelCore);
            Reg(gCore, "Assets/Project/Art/Fonts/Pixelta SDF.asset", "font/pixelta", AddressKeys.LabelCore);
            Reg(gCore, "Assets/Project/Art/Fonts/dearpix SDF.asset", "font/dearpix", AddressKeys.LabelCore);

            // 3. Shared_UI
            Reg(gSharedUi, "Assets/Project/Prefabs/UI/ItemTile.prefab", AddressKeys.UiSharedItemTile, AddressKeys.LabelSharedUi);
            Reg(gSharedUi, "Assets/Project/Prefabs/UI/SettingsPopup.prefab", AddressKeys.UiPopupSettings, AddressKeys.LabelSharedUi);
            Reg(gSharedUi, "Assets/Project/Prefabs/UI/VirtualJoystick.prefab", "ui/shared/virtual-joystick", AddressKeys.LabelSharedUi);

            // 4. Mod_Menu
            EnsureOnlyBootstrapInBuildSettings();
            Reg(gMenu, "Assets/Project/Scenes/Menu.unity", AddressKeys.SceneMenu, "menu");
            Reg(gMenu, "Assets/Project/Prefabs/UI/MenuScreen.prefab", AddressKeys.UiScreenMenu, "menu");

            // 5. Mod_Lobby
            Reg(gLobby, "Assets/Project/Scenes/Lobby.unity", AddressKeys.SceneLobby, "lobby");
            Reg(gLobby, "Assets/Project/Prefabs/UI/LobbyHud.prefab", AddressKeys.UiHudLobby, "lobby");
            Reg(gLobby, "Assets/Project/Prefabs/UI/CharacterPanel.prefab", AddressKeys.UiPanelCharacter, "lobby");

            // 6. Mod_Loadout
            Reg(gLoadout, "Assets/Project/Prefabs/UI/EquipmentScreen.prefab", AddressKeys.UiScreenEquipment, "loadout");
            Reg(gLoadout, "Assets/Project/Prefabs/UI/WeaponScreen.prefab", AddressKeys.UiScreenWeapon, "loadout");

            // 7. Mod_Shop
            Reg(gShop, "Assets/Project/Prefabs/UI/ShopScreen.prefab", AddressKeys.UiScreenShop, "shop");

            // 8. Mod_Play
            Reg(gPlay, "Assets/Project/Scenes/Play.unity", AddressKeys.ScenePlay, "play");
            Reg(gPlay, "Assets/Project/Prefabs/UI/PlayHud.prefab", AddressKeys.UiHudPlay, "play");
            Reg(gPlay, "Assets/Project/Prefabs/UI/PauseScreen.prefab", AddressKeys.UiScreenPause, "play");
            Reg(gPlay, "Assets/Project/Prefabs/Player/Player.prefab", AddressKeys.PlayerShell, "play");
            Reg(gPlay, "Assets/Project/Data/PlayPrefabSet.asset", AddressKeys.PlayPrefabSet, "play");

            // 9. Mod_Results
            Reg(gResults, "Assets/Project/Prefabs/UI/VictoryScreen.prefab", AddressKeys.UiScreenVictory, "results");
            Reg(gResults, "Assets/Project/Prefabs/UI/DefeatScreen.prefab", AddressKeys.UiScreenDefeat, "results");

            // 10. Catalog_Items & Characters
            Reg(gCatalog, "Assets/Project/Data/Catalog/ItemCatalog.asset", AddressKeys.DataCatalog, "catalog");
            foreach (var guid in AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Project/Data" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(p);
                if (item != null) Reg(gCatalog, p, $"data/item/{item.id}", "catalog");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:HeroDefinitionSO", new[] { "Assets/Project/Data" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var ch = AssetDatabase.LoadAssetAtPath<HeroDefinitionSO>(p);
                if (ch != null) Reg(gCatalog, p, $"data/char/{ch.characterId}", "catalog");
            }

            // 11. Theme_Dungeon
            // Mọi LevelConfig (không cố định 3 tầng) — key theo theme + số level, khớp CampaignSO.levelAddresses
            foreach (var guid in AssetDatabase.FindAssets("t:LevelConfigSO", new[] { "Assets/Project/Data" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var lv = AssetDatabase.LoadAssetAtPath<DungeonGathering.Play.LevelConfigSO>(p);
                if (lv != null) Reg(gTheme, p, AddressKeys.DungeonConfig(lv.themeId, lv.floor), "theme-dungeon");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project/Prefabs/Rooms", "Assets/Project/Prefabs/Traps" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                Reg(gTheme, p, $"theme/dungeon/{name}", "theme-dungeon");
            }

            // 12. Enemies_Dungeon & Boss_VampireLord — chỉ prefab boss (có BossPhaseController).
            //     Quái thường không còn prefab riêng: Enemy_Base (PlayPrefabSet, group Mod_Play) + MonsterDefinitionSO
            //     (tham chiếu từ LevelConfig, đi theo bundle Theme_Dungeon).
            (string file, string addr)[] dungeonEnemies =
            {
                ("Boss_EliteWarrior",      AddressKeys.Boss("elite-warrior")),
                ("Boss_EliteVampire",      AddressKeys.Boss("elite-vampire")),
                ("Boss_Crimson_Warlord",   AddressKeys.Boss("crimson-warlord")),
                ("Boss_Dread_Executioner", AddressKeys.Boss("dread-executioner")),
                ("Boss_Inferno_King",      AddressKeys.Boss("inferno-king")),
                ("Boss_Frost_Lich_King",   AddressKeys.Boss("frost-lich-king")),
            };
            foreach (var (file, addr) in dungeonEnemies)
                Reg(gEnemies, $"Assets/Project/Prefabs/Enemies/{file}.prefab", addr, "enemies-dungeon");

            Reg(gBoss, "Assets/Project/Prefabs/Enemies/Boss_VampireLord.prefab", AddressKeys.Boss("vampire-lord"), "boss-vampire-lord");

            // 13. VFX_Combat
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project/Prefabs/VFX" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                Reg(gVfx, p, $"vfx/{name}", "vfx-melee");
            }

            // 14. Audio_BGM — mỗi bài 1 bundle, tải theo label bgm-<id> khi vào module (MODULES §… Audio_BGM)
            foreach (var id in BgmIds)
            {
                string p = FindBgmFile(id);
                if (p == null) { Debug.LogWarning($"[Addressables] Chưa có nhạc '{id}' trong {BgmDir} (bgm_{id}.*)."); continue; }
                Reg(gBgm, p, AddressKeys.Bgm(id), "bgm-" + id);
            }

            RemoveGroupIfEmpty(settings, "Shared_Fonts");
            RemoveGroupIfEmpty(settings, "Characters");
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Addressables] Đã áp dụng quy tắc M2: {registered} entry trên 14 group + ModuleRegistry.");
        }

        /// <summary>
        /// Chỉ áp phần nhạc nền (không đụng group/module khác): đăng ký Audio/BGM/bgm_&lt;id&gt;.* vào group Audio_BGM,
        /// gán ModuleDefinition.bgmAddress và thêm label bgm-&lt;id&gt; vào required (bgm-boss vào prefetch của play).
        /// </summary>
        [MenuItem("Tools/Dungeon Gathering/Addressables/Apply BGM Rules")]
        public static void ApplyBgmRules()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null) { Debug.LogError("[Addressables] Không khởi tạo được AddressableAssetSettings."); return; }
            var group = EnsureGroup(settings, "Audio_BGM", BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

            int registered = 0;
            foreach (var id in BgmIds)
            {
                settings.AddLabel("bgm-" + id);
                string p = FindBgmFile(id);
                if (p == null) { Debug.LogWarning($"[Addressables] Chưa có nhạc '{id}' trong {BgmDir} (bgm_{id}.*)."); continue; }
                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(p), group, false, false);
                entry.address = AddressKeys.Bgm(id);
                entry.SetLabel("bgm-" + id, true, true, false);
                registered++;
            }

            foreach (var id in new[] { "menu", "lobby", "play" })
            {
                var m = AssetDatabase.LoadAssetAtPath<ModuleDefinition>($"{ModulesDir}/Module_{id}.asset");
                if (m == null) { Debug.LogWarning($"[Addressables] Thiếu Module_{id}.asset — chạy Apply Module Rules trước."); continue; }
                m.bgmAddress = BgmAddressIfPresent(id);
                m.requiredLabels = WithBgm((m.requiredLabels ?? new string[0]).Where(l => l != "bgm-" + id).ToArray(), id);
                if (id == "play")
                    m.prefetchLabels = WithBgm((m.prefetchLabels ?? new string[0]).Where(l => l != "bgm-boss").ToArray(), "boss");
                EditorUtility.SetDirty(m);
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Addressables] BGM: {registered}/{BgmIds.Length} bài trong group Audio_BGM, đã gán bgmAddress cho module.");
        }

        const string BgmDir = "Assets/Project/Audio/BGM";
        static readonly string[] BgmIds = { "menu", "lobby", "play", "boss" };

        /// <summary>Audio/BGM/bgm_&lt;id&gt;.(ogg|mp3|wav) → đường dẫn, null nếu chưa có.</summary>
        static string FindBgmFile(string id)
        {
            foreach (var ext in new[] { ".ogg", ".mp3", ".wav" })
            {
                string p = $"{BgmDir}/bgm_{id}{ext}";
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(p))) return p;
            }
            return null;
        }

        /// <summary>Address nhạc của module nếu file tồn tại, ngược lại rỗng (SceneService bỏ qua).</summary>
        static string BgmAddressIfPresent(string id) => FindBgmFile(id) != null ? AddressKeys.Bgm(id) : "";

        static string[] WithBgm(string[] labels, string id) =>
            FindBgmFile(id) != null ? labels.Concat(new[] { "bgm-" + id }).ToArray() : labels;

        static void RemoveGroupIfEmpty(AddressableAssetSettings settings, string groupName)
        {
            var grp = settings.FindGroup(groupName);
            if (grp != null && grp.entries.Count == 0)
                settings.RemoveGroup(grp);
        }

        static ModuleRegistry BuildModuleRegistry()
        {
            var menu = SaveModule("menu", AddressKeys.SceneMenu, GameState.Menu, true,
                WithBgm(new[] { "menu" }, "menu"),
                new[] { "lobby", "catalog" },
                new[] { AddressKeys.UiScreenMenu },
                BgmAddressIfPresent("menu"));

            var lobby = SaveModule("lobby", AddressKeys.SceneLobby, GameState.Lobby, false,
                WithBgm(new[] { "lobby", "catalog" }, "lobby"),
                new[] { "loadout", "shop", "play" },
                new[] { AddressKeys.UiHudLobby, AddressKeys.UiPanelCharacter, AddressKeys.UiScreenEquipment, AddressKeys.UiScreenWeapon },
                BgmAddressIfPresent("lobby"));

            var play = SaveModule("play", AddressKeys.ScenePlay, GameState.Playing, false,
                WithBgm(new[] { "play", "theme-dungeon", "enemies-dungeon", "vfx-melee" }, "play"),
                WithBgm(new[] { "boss-vampire-lord", "results", "shop" }, "boss"),
                new[] { AddressKeys.UiHudPlay, AddressKeys.UiScreenPause, AddressKeys.UiScreenVictory, AddressKeys.UiScreenDefeat, AddressKeys.UiScreenShop },
                BgmAddressIfPresent("play"));

            var reg = AssetDatabase.LoadAssetAtPath<ModuleRegistry>(RegistryPath);
            if (reg == null)
            {
                reg = ScriptableObject.CreateInstance<ModuleRegistry>();
                AssetDatabase.CreateAsset(reg, RegistryPath);
            }
            var so = new SerializedObject(reg);
            var list = so.FindProperty("modules");
            list.ClearArray();
            var defs = new[] { menu, lobby, play };
            for (int i = 0; i < defs.Length; i++)
            {
                list.InsertArrayElementAtIndex(i);
                list.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(reg);
            return reg;
        }

        static ModuleDefinition SaveModule(string id, string sceneAddr, GameState state, bool backdrop,
            string[] req, string[] prefetch, string[] screens, string bgmAddress)
        {
            string path = $"{ModulesDir}/Module_{id}.asset";
            var m = AssetDatabase.LoadAssetAtPath<ModuleDefinition>(path);
            if (m == null)
            {
                m = ScriptableObject.CreateInstance<ModuleDefinition>();
                AssetDatabase.CreateAsset(m, path);
            }
            m.moduleId = id;
            m.sceneAddress = sceneAddr;
            m.gameState = state;
            m.showBackdrop = backdrop;
            m.requiredLabels = req;
            m.prefetchLabels = prefetch;
            m.screenAddresses = screens;
            m.bgmAddress = bgmAddress;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void LinkSystemsPrefab(ModuleRegistry registry)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(SystemsPrefabPath);
            if (go == null) return;
            var sys = go.GetComponent<Systems>();
            if (sys == null) return;
            var so = new SerializedObject(sys);
            so.FindProperty("moduleRegistry").objectReferenceValue = registry;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SavePrefabAsset(go);
        }

        static AddressableAssetGroup EnsureGroup(AddressableAssetSettings settings, string groupName,
            BundledAssetGroupSchema.BundlePackingMode packingMode)
        {
            var grp = settings.FindGroup(groupName);
            if (grp == null)
            {
                grp = settings.CreateGroup(groupName, false, false, false, null,
                    typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            }
            var schema = grp.GetSchema<BundledAssetGroupSchema>() ?? grp.AddSchema<BundledAssetGroupSchema>();
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.BundleMode = packingMode;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            schema.UseAssetBundleCrcForCachedBundles = false;
            schema.UseUnityWebRequestForLocalBundles = true;
            schema.IncludeInBuild = true;
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            EditorUtility.SetDirty(grp);
            return grp;
        }

        static void EnsureOnlyBootstrapInBuildSettings()
        {
            const string bootstrapPath = "Assets/Project/Scenes/Bootstrap.unity";
            if (!File.Exists(bootstrapPath)) return;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(bootstrapPath, true) };
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
