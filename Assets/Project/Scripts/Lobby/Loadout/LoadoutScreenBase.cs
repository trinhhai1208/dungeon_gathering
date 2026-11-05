using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using DungeonGathering.Localization;
using DungeonGathering.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Phần chung của màn Trang bị và màn Vũ khí (thiết kế UI-Equipment / UI-Weapon, 3 cột):
    /// trái = chi tiết món đang chọn + nút hành động; giữa = nhân vật + ô đang mặc + chỉ số (có chênh lệch +/−);
    /// phải = túi/kho có bộ lọc. Luật đặt đồ + giới hạn 2 kỹ năng kích hoạt lấy từ <see cref="LoadoutRules"/>.
    /// Mở bằng <c>IUIService.Open(address, characterId)</c>.
    /// </summary>
    public abstract class LoadoutScreenBase : UIScreen
    {
        [System.Serializable]
        public sealed class StatRow
        {
            public TMP_Text value;
            public TMP_Text delta;
        }

        [Header("Header")]
        [SerializeField] protected Button backButton;
        [SerializeField] protected TMP_Text characterChip;
        [SerializeField] protected TMP_Text activeCountText;

        [Header("Cột trái — chi tiết")]
        [SerializeField] protected GameObject detailGroup;
        [SerializeField] protected GameObject emptyHint;
        [SerializeField] protected ItemTile detailTile;
        [SerializeField] protected TMP_Text detailName;
        [SerializeField] protected TMP_Text detailMeta;
        [SerializeField] protected TMP_Text detailState;
        [SerializeField] protected TMP_Text detailStats;
        [SerializeField] protected TMP_Text detailPassive;
        [SerializeField] protected TMP_Text detailActive;
        [SerializeField] protected TMP_Text detailWarn;
        [SerializeField] protected Button actionButton;
        [SerializeField] protected Image actionFrame;
        [SerializeField] protected LocalizedTMPText actionLabel;

        [Header("Cột giữa")]
        [SerializeField] protected Image portrait;
        [SerializeField] protected ItemTile[] slotTiles;
        [SerializeField] protected StatRow[] statRows;

        [Header("Cột phải — túi")]
        [SerializeField] protected Button[] filterTabs;
        [SerializeField] protected Image[] filterFrames;
        [SerializeField] protected TMP_Text bagCountText;
        [SerializeField] protected RectTransform bagContent;
        [SerializeField] protected ItemTile bagTilePrefab;

        [Header("Sprites")]
        [SerializeField] protected Sprite frameOn;     // f-light
        [SerializeField] protected Sprite frameOff;    // f-dark
        [SerializeField] protected Sprite frameBlocked;
        [SerializeField] protected Color inkColor = new Color32(0x1b, 0x14, 0x20, 0xff);
        [SerializeField] protected Color paperColor = new Color32(0xef, 0xe6, 0xdc, 0xff);
        [SerializeField] protected Color plusColor = new Color32(0x14, 0x53, 0x2d, 0xff);
        [SerializeField] protected Color minusColor = new Color32(0x7f, 0x1d, 0x1d, 0xff);

        protected string characterId;
        protected CharacterData character;
        protected LoadoutData loadout;
        protected ItemCatalog catalog;
        protected ItemData selected;
        protected int selectedSlot = -1;     // ô đang mặc được chọn (-1 = chọn từ túi)
        protected int filter;                // 0 = tất cả

        readonly List<ItemTile> bagTiles = new();
        readonly List<ItemData> bagItems = new();

        protected abstract int SlotCount { get; }
        protected abstract IEnumerable<string> OwnedIds();
        protected abstract string SlotItemId(int slot);
        protected abstract int SlotOf(ItemData item);              // ô mặc định khi chọn từ túi
        protected abstract bool PassesFilter(ItemData item, int filterIndex);
        protected abstract void Unequip(int slot);
        protected abstract void FillStats(ItemData preview);        // ghi statRows (+ delta nếu preview != null)
        protected abstract string MetaLine(ItemData item);
        protected abstract string StatsLine(ItemData item);
        protected abstract string PassiveLine(ItemData item);

        protected virtual void Awake()
        {
            backButton?.onClick.AddListener(() => Services.UI?.Back());
            actionButton?.onClick.AddListener(OnAction);
            for (int i = 0; i < (filterTabs?.Length ?? 0); i++)
            {
                int f = i;
                filterTabs[i]?.onClick.AddListener(() => { filter = f; RebuildBag(); });
            }
            for (int i = 0; i < (slotTiles?.Length ?? 0); i++)
            {
                int s = i;
                if (slotTiles[i] != null) slotTiles[i].Clicked += _ => SelectSlot(s);
            }
        }

        public override Awaitable OnOpen(object args)
        {
            characterId = args as string;
            if (string.IsNullOrEmpty(characterId)) characterId = Services.Session?.characterId ?? "knight";
            catalog = ItemCatalog.Current;
            character = catalog != null ? catalog.GetCharacter(characterId) : null;
            loadout = Services.Save?.Data.GetCharacter(characterId).loadout ?? new LoadoutData();
            selected = null; selectedSlot = -1; filter = 0;
            if (portrait != null && character != null && character.portrait != null) portrait.sprite = character.portrait;
            if (characterChip != null && character != null)
                characterChip.text = $"{LocalizationLookup.Get(character.nameKey, character.characterId)} · {character.role.ToString().ToUpperInvariant()}";
            RefreshAll();
            return base.OnOpen(args);
        }

        protected void RefreshAll()
        {
            RefreshSlots();
            RebuildBag();
            RefreshDetail();
            FillStats(PreviewCandidate());
            if (activeCountText != null && catalog != null)
                activeCountText.text = $"{LoadoutRules.CountActive(loadout, catalog)} / {LoadoutRules.MaxActiveItems}";
        }

        void RefreshSlots()
        {
            for (int i = 0; i < SlotCount && i < slotTiles.Length; i++)
            {
                var item = Get(SlotItemId(i));
                var tile = slotTiles[i]; if (tile == null) continue;
                if (item != null) tile.SetItem(item.icon, item.rarity); else tile.SetEmpty(EmptySilhouette(i));
                tile.SetSelected(selectedSlot == i);
            }
        }

        protected virtual Sprite EmptySilhouette(int slot) => null;

        void RebuildBag()
        {
            bagItems.Clear();
            var equipped = new HashSet<string>();
            for (int i = 0; i < SlotCount; i++) { var id = SlotItemId(i); if (!string.IsNullOrEmpty(id)) equipped.Add(id); }
            foreach (var id in OwnedIds())
            {
                if (equipped.Contains(id)) continue;
                var item = Get(id);
                if (item != null && PassesFilter(item, filter)) bagItems.Add(item);
            }
            while (bagTiles.Count < bagItems.Count && bagTilePrefab != null && bagContent != null)
            {
                var t = Instantiate(bagTilePrefab, bagContent);
                int index = bagTiles.Count;
                t.Clicked += _ => SelectBag(index);
                bagTiles.Add(t);
            }
            for (int i = 0; i < bagTiles.Count; i++)
            {
                bool on = i < bagItems.Count;
                bagTiles[i].gameObject.SetActive(on);
                if (!on) continue;
                bagTiles[i].SetItem(bagItems[i].icon, bagItems[i].rarity);
                bagTiles[i].SetSelected(selectedSlot < 0 && bagItems[i] == selected);
            }
            if (bagCountText != null) bagCountText.text = bagItems.Count.ToString();
            for (int i = 0; i < (filterFrames?.Length ?? 0); i++)
                if (filterFrames[i] != null) filterFrames[i].sprite = i == filter ? frameOn : frameOff;
        }

        void SelectBag(int index)
        {
            if (index < 0 || index >= bagItems.Count) return;
            selected = bagItems[index]; selectedSlot = -1;
            RefreshAll();
        }

        void SelectSlot(int slot)
        {
            selected = Get(SlotItemId(slot)); selectedSlot = selected != null ? slot : -1;
            RefreshAll();
        }

        protected ItemData PreviewCandidate() => selected != null && selectedSlot < 0 ? selected : null;

        void RefreshDetail()
        {
            bool has = selected != null;
            if (detailGroup != null) detailGroup.SetActive(has);
            if (emptyHint != null) emptyHint.SetActive(!has);
            if (!has) return;

            if (detailTile != null) detailTile.SetItem(selected.icon, selected.rarity);
            if (detailName != null) detailName.text = LocalizationLookup.Get(selected.nameKey, selected.id);
            if (detailMeta != null) detailMeta.text = MetaLine(selected);
            if (detailStats != null) detailStats.text = StatsLine(selected);
            if (detailPassive != null) detailPassive.text = PassiveLine(selected);
            if (detailActive != null)
                detailActive.text = selected.activeSkill != null
                    ? $"{LocalizationLookup.Get(selected.activeSkill.nameKey, selected.activeSkill.skillId)} · {selected.activeSkill.cooldown:0}s"
                    : LocalizationLookup.Get("common.none", "None.");

            bool equipped = selectedSlot >= 0;
            int target = equipped ? selectedSlot : SlotOf(selected);
            bool blocked = !equipped && catalog != null && !LoadoutRules.CanPlace(loadout, catalog, selected, target);
            if (detailState != null)
            {
                var replaced = Get(SlotItemId(target));
                detailState.text = equipped ? LocalizationLookup.Get("equip.worn", "EQUIPPED")
                    : replaced != null ? LocalizationLookup.Get("equip.replaces", "Replaces ") + LocalizationLookup.Get(replaced.nameKey, replaced.id)
                    : string.Empty;
            }
            if (detailWarn != null) detailWarn.text = blocked ? LocalizationLookup.Get("equip.limit_warn") : string.Empty;
            if (actionLabel != null) actionLabel.SetKey(equipped ? "common.unequip" : blocked ? "common.max_active" : "common.equip");
            if (actionFrame != null) actionFrame.sprite = equipped ? frameOff : blocked ? (frameBlocked != null ? frameBlocked : frameOff) : frameOn;
            if (actionButton != null) actionButton.interactable = equipped || !blocked;
            OnDetailRefreshed(equipped);
        }

        /// <summary>Lớp con cập nhật nút phụ (vd "VÀO Ô 2" của màn Vũ khí).</summary>
        protected virtual void OnDetailRefreshed(bool equipped) { }

        protected bool CanPlaceSelected(int slot) => selected != null && catalog != null && LoadoutRules.CanPlace(loadout, catalog, selected, slot);
        protected bool SelectedIsEquipped => selectedSlot >= 0;

        void OnAction()
        {
            if (selected == null || catalog == null) return;
            if (selectedSlot >= 0) { Unequip(selectedSlot); Commit(); }
            else PlaceSelected(SlotOf(selected));
        }

        protected void PlaceSelected(int slot)
        {
            if (selected == null || catalog == null) return;
            if (!LoadoutRules.TryPlace(loadout, catalog, selected, slot)) return;
            Commit();
        }

        void Commit()
        {
            Services.Save?.RequestSave();
            EventBus<EquipmentChangedEvent>.Raise(new EquipmentChangedEvent { CharacterId = characterId });
            selected = null; selectedSlot = -1;
            RefreshAll();
        }

        protected ItemData Get(string id) => !string.IsNullOrEmpty(id) && catalog != null ? catalog.GetItem(id) : null;

        protected void SetStat(int row, float now, float? preview, string format, string suffix = "")
        {
            if (statRows == null || row >= statRows.Length || statRows[row] == null) return;
            var r = statRows[row];
            float shown = preview ?? now;
            if (r.value != null) r.value.text = shown.ToString(format) + suffix;
            if (r.delta == null) return;
            float d = preview.HasValue ? preview.Value - now : 0f;
            if (Mathf.Abs(d) < 0.005f) { r.delta.text = string.Empty; return; }
            r.delta.text = (d > 0 ? "+" : "") + d.ToString(format) + suffix;
            r.delta.color = d > 0 ? plusColor : minusColor;
        }
    }
}
