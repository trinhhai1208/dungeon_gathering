using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>Danh sách module; SceneService.Go tra bảng này theo sceneAddress.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Module Registry", fileName = "ModuleRegistry")]
    public class ModuleRegistry : ScriptableObject
    {
        [SerializeField] List<ModuleDefinition> modules = new();

        public IReadOnlyList<ModuleDefinition> All => modules;

        public ModuleDefinition FindByScene(string sceneAddress)
        {
            foreach (var m in modules)
                if (m != null && m.sceneAddress == sceneAddress) return m;
            return null;
        }

        public ModuleDefinition FindById(string moduleId)
        {
            foreach (var m in modules)
                if (m != null && m.moduleId == moduleId) return m;
            return null;
        }

        static readonly Dictionary<string, ModuleDefinition> fallbackCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => fallbackCache.Clear();

        /// <summary>Định nghĩa mặc định khi registry chưa có module (giai đoạn dựng dự án). Cache theo address.</summary>
        public static ModuleDefinition CreateFallback(string sceneAddress)
        {
            string cacheKey = sceneAddress ?? string.Empty;
            if (fallbackCache.TryGetValue(cacheKey, out var cached) && cached != null) return cached;
            var def = CreateInstance<ModuleDefinition>();
            fallbackCache[cacheKey] = def;
            def.sceneAddress = sceneAddress;
            def.moduleId = string.IsNullOrEmpty(sceneAddress) ? "unknown" : sceneAddress.Substring(sceneAddress.LastIndexOf('/') + 1);
            def.showBackdrop = def.moduleId == "menu";
            def.gameState = def.moduleId switch { "lobby" => GameState.Lobby, "play" => GameState.Playing, _ => GameState.Menu };
            def.hideFlags = HideFlags.DontSave;
            return def;
        }
    }
}
