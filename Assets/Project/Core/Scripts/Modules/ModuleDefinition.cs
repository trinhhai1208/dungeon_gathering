using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>Một module tải được (MODULES §7.6).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Module Definition", fileName = "Module_")]
    public class ModuleDefinition : ScriptableObject
    {
        public string moduleId;                 // "lobby"
        public string sceneAddress;             // "scene/lobby" (rỗng nếu module chỉ có prefab)
        public string[] requiredLabels = { };   // tải xong mới vào
        public string[] prefetchLabels = { };   // tải nền sau khi vào
        public string[] screenAddresses = { };  // tài liệu hóa
        public bool showBackdrop;               // menu = true, lobby/play = false (UI_SYSTEM §5.1)
        public string bgmAddress;               // nhạc nền module (tùy chọn)
        public GameState gameState = GameState.Menu;
    }

    /// <summary>Thông tin truyền cho IModuleEntry.Begin.</summary>
    public readonly struct ModuleContext
    {
        public readonly ModuleScope Scope;
        public readonly ModuleDefinition Definition;
        public readonly object Args;
        public ModuleContext(ModuleScope scope, ModuleDefinition def, object args) { Scope = scope; Definition = def; Args = args; }
    }

    /// <summary>
    /// Component gốc của scene module. SceneService gọi Begin SAU khi scene cũ đã unload (bước 8 của Go — R7).
    /// Scene module không bắt đầu gameplay trong Awake/Start.
    /// </summary>
    public interface IModuleEntry
    {
        Awaitable Begin(ModuleContext context);
    }
}
