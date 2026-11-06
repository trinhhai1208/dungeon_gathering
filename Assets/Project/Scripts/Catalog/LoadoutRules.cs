using DungeonGathering.Catalog;
using DungeonGathering.Core;

namespace DungeonGathering.Inventory
{
    /// <summary>Giới hạn loadout (ENTITIES §5.5, WEAPONS §6.5).</summary>
    public static class LoadoutRules
    {
        public const int MaxActiveItems = 2;
        public const int WeaponSlotCount = LoadoutData.WeaponSlots;
        public const int EquipmentSlotCount = LoadoutData.EquipmentSlots;

        public static bool CanEquipActiveItem(int currentActiveCount) => currentActiveCount < MaxActiveItems;

        /// <summary>Số món có kỹ năng chủ động trong loadout (2 vũ khí + 6 trang bị).</summary>
        public static int CountActive(LoadoutData loadout, ItemCatalog catalog, string ignoreId = null)
            => CountActive(loadout, catalog, ignoreId, null);

        /// <summary>Như trên, bỏ qua tối đa hai id (món bị thay thế + chính món đang di chuyển).</summary>
        public static int CountActive(LoadoutData loadout, ItemCatalog catalog, string ignoreId, string ignoreId2)
        {
            if (loadout == null || catalog == null) return 0;
            int n = 0;
            foreach (var id in loadout.AllIds())
            {
                if (id == ignoreId || id == ignoreId2) continue;
                if (catalog.GetItem(id) is { HasActive: true }) n++;
            }
            return n;
        }

        /// <summary>
        /// Mặc item vào slot có vượt giới hạn kỹ năng kích hoạt không. Thay thế món đang ở slot đó được tính trừ ra;
        /// nếu chính món này đã nằm ở ô khác (di chuyển giữa 2 ô vũ khí) thì cũng không đếm nó hai lần.
        /// Trang bị luôn vào đúng ô eq.slot (giống TryPlace), không theo slotIndex.
        /// </summary>
        public static bool CanPlace(LoadoutData loadout, ItemCatalog catalog, ItemData item, int slotIndex)
        {
            if (item == null) return true;
            if (!item.HasActive) return true;
            if (loadout == null) return true;
            string replaced = item is EquipmentData eq ? loadout.GetEquipment(eq.slot)
                : item.IsWeapon ? loadout.GetWeapon(slotIndex) : loadout.GetEquipment((EquipmentSlotType)slotIndex);
            int count = CountActive(loadout, catalog, replaced, item.id);
            return count < MaxActiveItems;
        }

        /// <summary>Đặt item vào loadout nếu hợp lệ (đúng loại ô, không vượt giới hạn). Trả false nếu bị chặn.</summary>
        public static bool TryPlace(LoadoutData loadout, ItemCatalog catalog, ItemData item, int slotIndex)
        {
            if (loadout == null || item == null) return false;
            if (!CanPlace(loadout, catalog, item, slotIndex)) return false;
            if (item is WeaponData)
            {
                if (slotIndex < 0 || slotIndex >= WeaponSlotCount) return false;
                // Không cho cùng một vũ khí ở 2 ô.
                for (int i = 0; i < WeaponSlotCount; i++) if (i != slotIndex && loadout.GetWeapon(i) == item.id) loadout.SetWeapon(i, null);
                loadout.SetWeapon(slotIndex, item.id);
                return true;
            }
            if (item is EquipmentData eq)
            {
                loadout.SetEquipment(eq.slot, eq.id);
                return true;
            }
            return false;
        }

        /// <summary>Đảm bảo loadout hợp lệ khi bắt đầu run (cắt bớt món kích hoạt vượt giới hạn, khử trùng lặp vũ khí).</summary>
        public static void Sanitize(LoadoutData loadout, ItemCatalog catalog, CharacterData character)
        {
            loadout.Normalize();
            // Nếu ô 0 trống nhưng ô 1 có vũ khí thì đẩy sang ô 0
            if (string.IsNullOrEmpty(loadout.weapons[0]) && !string.IsNullOrEmpty(loadout.weapons[1]))
            {
                loadout.weapons[0] = loadout.weapons[1];
                loadout.weapons[1] = null;
            }
            if (loadout.weapons[1] == loadout.weapons[0]) loadout.weapons[1] = null; // không cùng vũ khí ở 2 ô
            int actives = 0;
            for (int i = 0; i < WeaponSlotCount; i++)
            {
                var w = catalog.GetItem(loadout.weapons[i]);
                if (w == null) { loadout.weapons[i] = null; continue; }
                if (w.HasActive && ++actives > MaxActiveItems) loadout.weapons[i] = null;
            }
            for (int i = 0; i < EquipmentSlotCount; i++)
            {
                var e = catalog.GetItem(loadout.equipment[i]);
                if (e == null) { loadout.equipment[i] = null; continue; }
                if (e.HasActive && ++actives > MaxActiveItems) loadout.equipment[i] = null;
            }
        }
    }
}
