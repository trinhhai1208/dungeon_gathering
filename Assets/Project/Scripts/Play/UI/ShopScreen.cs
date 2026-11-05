using System.Collections.Generic;
using System.Linq;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using DungeonGathering.Localization;
using DungeonGathering.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Màn Thương nhân (UI-Shop.dc.html). Hai ngữ cảnh:
    /// - Sảnh: <c>Open("ui/screen/shop")</c> không args → hàng từ <see cref="ItemCatalog"/> (chưa sở hữu, có giá), trả bằng coin vĩnh viễn (<see cref="Economy.BuyInLobby"/>).
    /// - Trong run: args = <see cref="RunShopArgs"/> → hàng của <see cref="ShopStand"/>, trả bằng coin của run (<see cref="ShopStand.TryBuy"/>).
    /// Giao dịch đồng bộ + idempotent (R11): nút MUA khóa đến khi màn vẽ lại.
    /// </summary>
    public sealed class ShopScreen : UIScreen
    {
        [System.Serializable]
        public sealed class Card
        {
            public Button button;
            public Image frame;
            public ItemTile tile;
            public TMP_Text name;
            public TMP_Text kind;
            public TMP_Text price;
        }

        [SerializeField] Button closeButton;
        [SerializeField] LocalizedTMPText contextChip;
        [SerializeField] TMP_Text coinText;
        [SerializeField] Card[] cards = new Card[6];

        [Header("Chi tiết")]
        [SerializeField] ItemTile detailTile;
        [SerializeField] TMP_Text detailName;
        [SerializeField] TMP_Text detailKind;
        [SerializeField] TMP_Text detailStats;
        [SerializeField] TMP_Text detailDesc;
        [SerializeField] LocalizedTMPText message;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text buyPrice;

        [Header("Sprites")]
        [SerializeField] Sprite cardOn;
        [SerializeField] Sprite cardOff;
        [SerializeField] Sprite potionIcon;
        [SerializeField] Color inkColor = new Color32(0x1b, 0x14, 0x20, 0xff);

        RunShopArgs run;
        readonly List<(ItemData item, int price, bool sold)> offers = new();
        int selected;
        string messageKey;

        void Awake()
        {
            closeButton?.onClick.AddListener(() => Services.UI?.Back());
            buyButton?.onClick.AddListener(Buy);
            for (int i = 0; i < cards.Length; i++)
            {
                int k = i;
                var card = cards[i];
                if (card != null)
                {
                    if (card.button != null)
                    {
                        card.button.navigation = new Navigation { mode = Navigation.Mode.None };
                        card.button.onClick.AddListener(() => SelectCard(k));
                    }
                    if (card.tile != null)
                    {
                        card.tile.Clicked += _ => SelectCard(k);
                    }
                }
            }
        }

        void SelectCard(int index)
        {
            selected = index;
            messageKey = null;
            Redraw();
        }

        public override Awaitable OnOpen(object args)
        {
            run = args as RunShopArgs;
            selected = 0; messageKey = null;
            SetPlayerCombat(false);
            LoadOffers();
            if (contextChip != null) contextChip.SetKey(run != null ? "shop.where_run" : "shop.where_lobby");
            Redraw();
            return base.OnOpen(args);
        }

        public override void OnClose()
        {
            SetPlayerCombat(true);
            base.OnClose();
        }

        void OnDestroy() => SetPlayerCombat(true);

        bool combatLocked;

        /// <summary>Shop trong run: khóa tấn công/kỹ năng của Player khi màn đang mở (vẫn đi lại được); mở lại khi đóng nếu run còn chạy.</summary>
        void SetPlayerCombat(bool on)
        {
            if (!on && run == null) return;
            if (on && !combatLocked) return;
            var dir = LevelDirector.Current;
            var player = dir != null ? dir.Player : null;
            if (!on)
            {
                combatLocked = true;
                if (player != null) player.SetCombatEnabled(false);
                return;
            }
            combatLocked = false;
            if (player != null && dir.IsPlaying && !player.IsDead) player.SetCombatEnabled(true);
        }

        bool PotionBlocked(ItemData item) => run != null && item == null && run.Stand != null && !run.Stand.CanUsePotion;

        void LoadOffers()
        {
            offers.Clear();
            if (run != null)
            {
                foreach (var o in run.Offers) offers.Add((o.Item, o.Price, o.Sold));
                return;
            }
            var catalog = ItemCatalog.Current;
            var save = Services.Save?.Data;
            if (catalog == null) return;
            foreach (var item in catalog.All<ItemData>().Where(i => i.shopPrice > 0 && (save == null || !save.Owns(i.id))).Take(cards.Length))
                offers.Add((item, item.shopPrice, false));
        }

        int Wallet => run != null ? (Services.Session?.runCoins ?? 0) : (Services.Save?.Data.coins ?? 0);

        void Redraw()
        {
            if (coinText != null) coinText.text = Wallet.ToString("N0");
            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i]; if (c == null) continue;
                bool has = i < offers.Count;
                if (c.button != null) c.button.gameObject.SetActive(has);
                if (!has) continue;
                var (item, price, sold) = offers[i];
                if (c.frame != null) c.frame.sprite = i == selected ? cardOn : cardOff;
                if (c.tile != null) { if (item != null) c.tile.SetItem(item.icon, item.rarity); else c.tile.SetItem(potionIcon, Rarity.F); }
                if (c.name != null) { c.name.text = Name(item); c.name.color = inkColor; }
                if (c.kind != null) c.kind.text = Kind(item);
                if (c.price != null) c.price.text = sold ? "—" : price.ToString();
            }
            DrawDetail();
        }

        void DrawDetail()
        {
            if (selected >= offers.Count)
            {
                // không có hàng (chưa có dữ liệu / đã mua hết): cột chi tiết trống
                if (buyButton != null) buyButton.interactable = false;
                if (detailTile != null) detailTile.SetEmpty();
                foreach (var t in new[] { detailName, detailKind, detailStats, detailDesc, buyPrice }) if (t != null) t.text = string.Empty;
                if (detailName != null) detailName.text = LocalizationLookup.Get("common.empty", "Empty");
                message?.SetRaw(string.Empty);
                return;
            }
            var (item, price, sold) = offers[selected];
            if (detailTile != null) { if (item != null) detailTile.SetItem(item.icon, item.rarity); else detailTile.SetItem(potionIcon, Rarity.F); }
            if (detailName != null) detailName.text = Name(item);
            if (detailKind != null) detailKind.text = Kind(item);
            if (detailStats != null) detailStats.text = Stats(item);
            if (detailDesc != null) detailDesc.text = item == null ? LocalizationLookup.Get("shop.potion_desc") : LocalizationLookup.Get(item.descKey, string.Empty);
            if (buyPrice != null) buyPrice.text = price.ToString();
            bool hpFull = !sold && PotionBlocked(item);
            bool canBuy = !sold && !hpFull && Wallet >= price;
            if (buyButton != null) buyButton.interactable = canBuy;
            if (hpFull && messageKey == null)
            {
                message?.SetRaw(LocalizationLookup.Get("shop.hp_full", "HP is full"));
                return;
            }
            string key = messageKey ?? (sold ? null : Wallet < price ? "shop.no_coin" : run != null ? "shop.hint_run" : "shop.hint_lobby");
            if (message != null) { if (key != null) message.SetKey(key); else message.SetRaw(string.Empty); }
        }

        void Buy()
        {
            if (selected >= offers.Count) return;
            if (buyButton != null) buyButton.interactable = false;   // khóa ngay (R11)
            var (item, price, _) = offers[selected];
            PurchaseResult r = run != null ? run.Stand.TryBuy(selected) : Economy.BuyInLobby(item);
            Sfx.Play(r == PurchaseResult.Ok ? SfxId.UiPurchase : SfxId.UiError);
            messageKey = r switch
            {
                PurchaseResult.Ok => item == null ? "shop.used" : item.IsWeapon ? "shop.bought_weapon" : run != null ? "shop.bought_run" : "shop.bought_lobby",
                PurchaseResult.NotEnoughCoins => "shop.no_coin",
                _ => null
            };
            if (r == PurchaseResult.Ok)
            {
                if (run != null) LoadOffers();
                else offers[selected] = (item, price, true);
            }
            Redraw();
        }

        static string Name(ItemData item) => item == null ? LocalizationLookup.Get("item.potion", "Health Potion") : LocalizationLookup.Get(item.nameKey, item.id);

        static string Kind(ItemData item)
        {
            if (item == null) return LocalizationLookup.Get("shop.consumable", "Consumable");
            return item is WeaponData ? $"{LocalizationLookup.Get("shop.weapon_prefix", "Weapon")} · {item.rarity}" : $"{item.GetType().Name.Replace("Data", "")} · {item.rarity}";
        }

        static string Stats(ItemData item) => item switch
        {
            // Giá trị thật của bình máu (LootSpawner.HealthPerPotion) — không dùng chuỗi cố định "shop.hp30".
            null => $"+{LootSpawner.HealthPerPotion} {LocalizationLookup.Get("stat.hp", "HP")}",
            WeaponData w => $"{w.damage:0} {LocalizationLookup.Get("stat.dmg")}\n{w.fireRate:0.0} {LocalizationLookup.Get("stat.aspd")}",
            EquipmentData e when e.baseModifiers != null => string.Join("\n", e.baseModifiers.Select(m => m.PercentValue != 0 ? $"+{m.PercentValue * 100:0}% {m.Stat}" : $"+{m.FlatValue:0} {m.Stat}")),
            _ => string.Empty
        };
    }
}
