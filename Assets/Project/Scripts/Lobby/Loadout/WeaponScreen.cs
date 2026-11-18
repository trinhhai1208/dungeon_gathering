using System.Collections.Generic;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using TMPro;
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
        protected override bool PassesFilter(ItemData item, int f) => f == 0 || (int)item.Rank == f - 1; // Rarity: S,A,B,C,F
        protected override void Unequip(int slot)
        {
            if (slot >= 0 && slot < SlotCount)
                inventory?.UnequipWeapon(slot);
        }

        /// <summary>"Cận chiến · Bậc A" / "Tầm xa · Bậc S" theo loại vũ khí thật (trước đây ghi cứng "Sword · Melee").</summary>
        protected override string MetaLine(ItemData item)
        {
            bool ranged = item is WeaponData w && w.Spec.weaponType == WeaponType.Ranged;
            string kind = ranged ? LocalizationLookup.Get("weapon.kind_ranged", "Ranged") : LocalizationLookup.Get("weapon.kind_melee", "Melee");
            return $"{kind} · {LocalizationLookup.Get("common.rarity", "Rarity")} {item.Rank}";
        }

        protected override string StatsLine(ItemData item)
        {
            if (item is not WeaponData w) return string.Empty;
            return $"{LocalizationLookup.Get("stat.dmg")} {w.Spec.damage:0}   {LocalizationLookup.Get("stat.aspd")} {w.Spec.fireRate:0.0}\n" +
                   $"{LocalizationLookup.Get("stat.range")} {w.Spec.attackRange:0.0}   DPS {w.Spec.damage * w.Spec.fireRate:0.0}";
        }

        protected override string PassiveLine(ItemData item)
        {
            var p = (item as WeaponData)?.Passive;
            if (p == null) return LocalizationLookup.Get("common.none", "None.");
            return !string.IsNullOrEmpty(p.descKey) ? LocalizationLookup.Get(p.descKey) : p.description;
        }

        protected override void FillStats(ItemData preview)
        {
            var now = Get(SlotItemId(0)) as WeaponData;
            var after = preview as WeaponData;
            float[] N(WeaponData w) => w == null ? new float[6] : new[] { w.Spec.damage, w.Spec.fireRate, w.Spec.attackRange, w.Spec.damage * w.Spec.fireRate, w.Spec.critBonus * 100f, w.Spec.manaCost };
            var a = N(now); var b = after != null ? N(after) : null;
            string[] fmt = { "0", "0.0", "0.0", "0.0", "0", "0" };
            for (int i = 0; i < 6; i++) SetStat(i, a[i], b != null ? b[i] : null, fmt[i], i == 4 ? "%" : "", higherIsBetter: i != 5);
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
            if (slot2Label != null)
            {
                slot2Label.SetKey(ok ? "weapon.to_slot2" : "common.max_active");
                var slot2Tmp = slot2Label.GetComponent<TMP_Text>();
                if (slot2Tmp != null) slot2Tmp.color = ok ? inkColor : paperColor;
            }
            if (actionLabel != null && actionButton != null && actionButton.interactable) actionLabel.SetKey("weapon.to_slot1");
        }
    }
}
