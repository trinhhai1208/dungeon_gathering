using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Trang bị đang mặc trong run: baseModifiers cho mọi Role; đúng Role → kích hoạt synergyPassive (ENTITIES §5.2).
    /// Không đổi đồ trong dungeon — chỉ Apply một lần khi spawn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        readonly EquipmentData[] slots = new EquipmentData[LoadoutData.EquipmentSlots];
        readonly List<EquipmentPassiveSO> activePassives = new();
        IPlayerContext ctx;
        PlayerStats stats;

        public EquipmentData Get(EquipmentSlotType slot) => slots[(int)slot];
        public int SynergyCount => activePassives.Count;

        public void Apply(IPlayerContext context, PlayerStats playerStats, IEnumerable<EquipmentData> items)
        {
            Clear();
            ctx = context;
            stats = playerStats;
            if (items == null) return;
            var role = context.Character != null ? context.Character.role : CharacterRole.Any;
            foreach (var eq in items)
            {
                if (eq == null) continue;
                slots[(int)eq.slot] = eq;
                stats.Sheet.AddRange(eq, eq.baseModifiers);
                if (eq.SynergyActive(role))
                {
                    eq.synergyPassive.OnEquip(context);
                    activePassives.Add(eq.synergyPassive);
                }
            }
        }

        public void Clear()
        {
            foreach (var p in activePassives) if (p != null && ctx != null) p.OnUnequip(ctx);
            activePassives.Clear();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && stats != null) stats.Sheet.RemoveSource(slots[i]);
                slots[i] = null;
            }
        }

        void OnDestroy() => Clear();
    }
}
