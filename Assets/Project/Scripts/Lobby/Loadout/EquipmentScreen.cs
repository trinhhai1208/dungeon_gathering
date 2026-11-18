using System.Collections.Generic;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using TMPro;
using UnityEngine;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Màn Trang bị (UI-Equipment.dc.html): 6 ô chia 3-3 quanh nhân vật, chỉ số tổng có chênh lệch khi xem món trong túi,
    /// bộ lọc 7 tab (Tất cả + 6 loại ô), Role Synergy. Address: <c>ui/screen/equipment</c>, args = characterId.
    /// </summary>
    public sealed class EquipmentScreen : LoadoutScreenBase
    {
        static readonly StatType[] Rows = { StatType.MaxHp, StatType.MaxArmor, StatType.MaxMana, StatType.AttackDamage, StatType.AttackSpeed, StatType.MoveSpeed, StatType.CritChance };
        static readonly string[] SlotKeys = { "slot.helmet", "stat.armor", "slot.gloves", "slot.boots", "slot.neck", "slot.acc" };

        [SerializeField] TMP_Text synergyCountText;
        [SerializeField] Sprite[] slotSilhouettes = new Sprite[6];

        protected override int SlotCount => LoadoutData.EquipmentSlots;
        protected override IEnumerable<string> OwnedIds() =>
            inventory != null ? inventory.GetOwnedEquipment() : Enumerable.Empty<string>();
        protected override string SlotItemId(int slot) => loadout.GetEquipment((EquipmentSlotType)slot);
        protected override int SlotOf(ItemData item) => item is EquipmentSO eq ? (int)eq.slot : 0;
        protected override bool PassesFilter(ItemData item, int f) => f == 0 || (item is EquipmentSO eq && (int)eq.slot == f - 1);
        protected override void Unequip(int slot) => inventory?.UnequipItem((EquipmentSlotType)slot);
        protected override Sprite EmptySilhouette(int slot) => slot < slotSilhouettes.Length ? slotSilhouettes[slot] : null;

        protected override string MetaLine(ItemData item)
        {
            var eq = item as EquipmentSO;
            string slot = eq != null ? LocalizationLookup.Get(SlotKeys[(int)eq.slot]) : "";
            return $"{slot} · Rarity {item.Rank}";
        }

        protected override string StatsLine(ItemData item)
        {
            if (item is not EquipmentSO eq || eq.baseModifiers == null) return string.Empty;
            return string.Join("\n", eq.baseModifiers.Select(m =>
            {
                string name = LocalizationLookup.Get(StatKey(m.Stat));
                return m.PercentValue != 0 ? $"+{m.PercentValue * 100:0}% {name}" : $"+{m.FlatValue:0} {name}";
            }));
        }

        protected override string PassiveLine(ItemData item)
        {
            if (item is not EquipmentSO eq || eq.synergyPassive == null) return LocalizationLookup.Get("eq.no_role", "No Role effect.");
            string text = !string.IsNullOrEmpty(eq.synergyPassive.descKey) ? LocalizationLookup.Get(eq.synergyPassive.descKey) : eq.synergyPassive.description;
            bool on = character != null && eq.SynergyActive(character.role);
            string badge = on ? LocalizationLookup.Get("equip.badge_on", "ACTIVE") : LocalizationLookup.Get("equip.badge_need", "NEEDS ") + eq.synergyRole.ToString().ToUpperInvariant();
            return on ? $"<b>{badge}</b>\n{text}" : $"<b>{badge}</b>\n<i><color=#C9CDD8>{text}</color></i>";
        }

        protected override void FillStats(ItemData preview)
        {
            var now = Totals(null);
            var after = preview is EquipmentSO eq ? Totals(eq) : null;
            for (int i = 0; i < Rows.Length; i++)
            {
                bool pct = Rows[i] is StatType.AttackDamage or StatType.CritChance;
                float k = pct ? 100f : 1f;
                SetStat(i, now[i] * k, after != null ? after[i] * k : null, Rows[i] is StatType.AttackSpeed or StatType.MoveSpeed ? "0.0" : "0", pct ? "%" : "");
            }
            if (synergyCountText != null && character != null)
            {
                int n = 0;
                for (int s = 0; s < SlotCount; s++) if (Get(SlotItemId(s)) is EquipmentSO e && e.SynergyActive(character.role)) n++;
                synergyCountText.text = $"{n} / {SlotCount}";
            }
        }

        float[] Totals(EquipmentSO swapIn)
        {
            var b = character != null ? character.ResolveStats() : BaseStats.Default;
            var flat = new float[Rows.Length]; var pct = new float[Rows.Length];
            var setCounts = new Dictionary<EquipmentSetSO, int>();
            void Accumulate(IReadOnlyList<StatModifier> mods)
            {
                if (mods == null) return;
                foreach (var m in mods)
                {
                    int r = System.Array.IndexOf(Rows, m.Stat); if (r < 0) continue;
                    flat[r] += m.FlatValue; pct[r] += m.PercentValue;
                }
            }
            for (int s = 0; s < SlotCount; s++)
            {
                var item = Get(SlotItemId(s)) as EquipmentSO;
                if (swapIn != null && s == (int)swapIn.slot) item = swapIn;
                if (item == null) continue;
                Accumulate(item.baseModifiers);
                // Giống PlayerEquipment.Apply: passive synergy (đúng Role) và passive set 4 món cũng cộng chỉ số
                if (character != null && item.SynergyActive(character.role)) Accumulate(item.synergyPassive.StatModifiers);
                if (item.equipmentSet != null)
                    setCounts[item.equipmentSet] = setCounts.TryGetValue(item.equipmentSet, out var count) ? count + 1 : 1;
            }
            foreach (var kv in setCounts)
            {
                if (kv.Value >= 2) Accumulate(kv.Key.bonus2Pieces);
                if (kv.Value >= 4 && kv.Key.bonus4Passive != null) Accumulate(kv.Key.bonus4Passive.StatModifiers);
            }
            var result = new float[Rows.Length];
            for (int i = 0; i < Rows.Length; i++) result[i] = (b.Get(Rows[i]) + flat[i]) * (1f + pct[i]);
            return result;
        }

        static string StatKey(StatType t) => t switch
        {
            StatType.MaxHp => "stat.hp", StatType.MaxArmor => "stat.armor", StatType.MaxMana => "stat.mana",
            StatType.AttackDamage => "stat.dmg", StatType.AttackSpeed => "stat.aspd", StatType.MoveSpeed => "stat.spd", _ => "stat.crit"
        };
    }
}
