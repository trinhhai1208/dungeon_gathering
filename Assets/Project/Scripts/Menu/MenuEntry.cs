using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Menu
{
    /// <summary>IModuleEntry của Menu.unity: mở MenuScreen (ui/screen/menu). Scene chỉ có UI, không camera thế giới.</summary>
    public sealed class MenuEntry : MonoBehaviour, IModuleEntry
    {
        public async Awaitable Begin(ModuleContext context)
        {
            if (!AddressablesUtil.KeyExists(AddressKeys.UiScreenMenu))
            {
                Debug.LogWarning("[Menu] Chưa có address ui/screen/menu.");
                return;
            }
            await Services.UI.Open<UIScreen>(AddressKeys.UiScreenMenu);
        }
    }
}
