using System.Collections.Generic;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Màn Vũ khí (UI-Weapon.dc.html): 2 ô vũ khí, kho kiếm lọc theo rarity (Tất cả/S/A/B/C/F),
    /// chỉ số vũ khí của ô 1 kèm chênh lệch nếu đổi sang món đang xem, 2 nút "VÀO Ô 1 / VÀO Ô 2".
    /// Address: <c>ui/screen/weapon</c>, args = characterId.
    /// </summary>
    public sealed class WeaponScreen : LoadoutScreenBase
    {
        [SerializeField] Button slot2Button;
        [SerializeField] Image slot2Frame;
        [SerializeField] LocalizedTMPText slot2Label;

        protected override void Awake()
        {
            base.Awake();
            slot2Button?.onClick.AddListener(() => PlaceSelected(1));
        }

        protected override int SlotCount => LoadoutData.WeaponSlots;
        protected override IEnumerable<string> OwnedIds() =>
            inventory != null ? inventory.GetOwnedWeapons() : Enumerable.Empty<string>();
        protected override string SlotItemId(int slot) => loadout.GetWeapon(slot);
        protected override int SlotOf(ItemData item) => 0;
        protected override bool PassesFilter(ItemData item, int f) => f == 0 || (int)item.rarity == f - 1; // Rarity: S,A,B,C,F
        protected override void Unequip(int slot)
        {
            if (slot >= 0 && slot < SlotCount)
                inventory?.UnequipWeapon(slot);
        }

        protected override string MetaLine(ItemData item) =>
            $"{LocalizationLookup.Get("weapon.type", "Sword · Melee · Rarity")} {item.rarity}";

        protected override string StatsLine(ItemData item)
        {
            if (item is not WeaponData w) return string.Empty;
            return $"{LocalizationLookup.Get("stat.dmg")} {w.damage:0}   {LocalizationLookup.Get("stat.aspd")} {w.fireRate:0.0}\n" +
                   $"{LocalizationLookup.Get("stat.range")} {w.attackRange:0.0}   DPS {w.damage * w.fireRate:0.0}";
        }

        protected override string PassiveLine(ItemData item)
        {
            var p = (item as WeaponData)?.passive;
            if (p == null) return LocalizationLookup.Get("common.none", "None.");
            return !string.IsNullOrEmpty(p.descKey) ? LocalizationLookup.Get(p.descKey) : p.description;
        }

        protected override void FillStats(ItemData preview)
        {
            var now = Get(SlotItemId(0)) as WeaponData;
            var after = preview as WeaponData;
            float[] N(WeaponData w) => w == null ? new float[6] : new[] { w.damage, w.fireRate, w.attackRange, w.damage * w.fireRate, w.critBonus * 100f, w.manaCost };
            var a = N(now); var b = after != null ? N(after) : null;
            string[] fmt = { "0", "0.0", "0.0", "0.0", "0", "0" };
            for (int i = 0; i < 6; i++) SetStat(i, a[i], b != null ? b[i] : null, fmt[i], i == 4 ? "%" : "");
        }

        protected override void OnDetailRefreshed(bool equipped)
        {
            if (slot2Button == null) return;
            bool show = !equipped && selected != null;
            slot2Button.gameObject.SetActive(show);
            if (!show) return;
            bool ok = CanPlaceSelected(1);
            slot2Button.interactable = ok;
            if (slot2Frame != null) slot2Frame.sprite = ok ? frameOn : frameOff;
            if (slot2Label != null) slot2Label.SetKey(ok ? "weapon.to_slot2" : "common.max_active");
            if (actionLabel != null && actionButton != null && actionButton.interactable) actionLabel.SetKey("weapon.to_slot1");
        }
    }
}
