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
        readonly EquipmentSO[] slots = new EquipmentSO[LoadoutData.EquipmentSlots];
        readonly List<ItemPassiveSO> activePassives = new();
        readonly List<EquipmentSetSO> activeSets = new();
        readonly Dictionary<EquipmentSetSO, int> setCounts = new();
        int synergyCount;
        IPlayerContext ctx;
        PlayerStats stats;

        public EquipmentSO Get(EquipmentSlotType slot) => slots[(int)slot];
        public int SynergyCount => synergyCount;

        public void Apply(IPlayerContext context, PlayerStats playerStats, IEnumerable<EquipmentSO> items)
        {
            Clear();
            ctx = context;
            stats = playerStats;
            if (items == null) return;
            var role = context.Character != null ? context.Character.role : CharacterRole.Any;
            setCounts.Clear();
            foreach (var eq in items)
            {
                if (eq == null) continue;
                slots[(int)eq.slot] = eq;
                stats.Sheet.AddRange(eq, eq.baseModifiers);
                if (eq.onHitStatus != null && eq.onHitStatus.Count > 0) onHitEffects.AddRange(eq.onHitStatus);
                if (eq.SynergyActive(role))
                {
                    eq.synergyPassive.OnEquip(context);
                    activePassives.Add(eq.synergyPassive);
                    synergyCount++;
                }
                if (eq.equipmentSet != null)
                {
                    setCounts.TryGetValue(eq.equipmentSet, out int count);
                    setCounts[eq.equipmentSet] = count + 1;
                }
            }

            foreach (var kv in setCounts)
            {
                var set = kv.Key;
                int count = kv.Value;
                if (count >= 2 && set.bonus2Pieces != null && set.bonus2Pieces.Count > 0)
                {
                    stats.Sheet.AddRange(set, set.bonus2Pieces);
                    activeSets.Add(set);
                }
                if (count >= 4 && set.bonus4Passive != null)
                {
                    set.bonus4Passive.OnEquip(context);
                    activePassives.Add(set.bonus4Passive);
                }
            }
            if (onHitEffects.Count > 0) { context.Hooks.HitDealt += OnHitDealt; hooked = true; }
        }

        /// <summary>Hiệu ứng khi trúng gộp từ mọi trang bị đang mặc.</summary>
        readonly List<StatusEffectRef> onHitEffects = new();
        bool hooked;

        void OnHitDealt(HitReport r)
        {
            if (onHitEffects.Count == 0 || r.Target == null || ctx == null) return;
            new DungeonGathering.Combat.StatusPayload { effects = onHitEffects }.ApplyTo(r.Target, ctx.GameObject);
        }

        public void Clear()
        {
            onHitEffects.Clear();
            if (hooked && ctx != null) ctx.Hooks.HitDealt -= OnHitDealt;
            hooked = false;
            foreach (var p in activePassives) if (p != null && ctx != null) p.OnUnequip(ctx);
            activePassives.Clear();
            synergyCount = 0;
            foreach (var s in activeSets) if (s != null && stats != null) stats.Sheet.RemoveSource(s);
            activeSets.Clear();
            setCounts.Clear();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && stats != null) stats.Sheet.RemoveSource(slots[i]);
                slots[i] = null;
            }
        }

        void OnDestroy() => Clear();
    }
}
