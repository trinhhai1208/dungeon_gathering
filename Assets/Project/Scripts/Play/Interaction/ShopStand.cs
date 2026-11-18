using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Một món bày bán ở ShopStand.</summary>
    public sealed class ShopOffer
    {
        public ItemData Item;      // null = bình máu
        public int Price;
        public bool Sold;
        public bool IsPotion => Item == null;
    }

    /// <summary>Tham số mở ui/screen/shop trong run.</summary>
    public sealed class RunShopArgs
    {
        public ShopContext Context = ShopContext.Run;
        public ShopStand Stand;
        public IReadOnlyList<ShopOffer> Offers => Stand.Offers;
    }

    /// <summary>
    /// Quầy bán trong phòng Shop (ENTITIES §8): 2 món + 1 bình máu, mua bằng coin của run.
    /// TryBuy đồng bộ + idempotent (cờ Sold, trừ tiền + thêm đồ trong một hàm — R11).
    /// </summary>
    public sealed class ShopStand : MonoBehaviour, IInteractable
    {
        [SerializeField] SpriteRenderer[] slotIcons = new SpriteRenderer[3];
        [SerializeField] Sprite potionIcon;
        [SerializeField] int potionPrice = 15;
        [SerializeField] float priceMultiplier = 0.5f;
        [SerializeField] string promptKey = "hud.shop";

        readonly List<ShopOffer> offers = new();
        LevelDirector director;
        bool opening;

        public IReadOnlyList<ShopOffer> Offers => offers;

        public void Setup(LevelDirector levelDirector)
        {
            director = levelDirector;
            offers.Clear();
            var cat = director.Catalog;
            for (int i = 0; i < 2 && cat != null; i++)
            {
                var item = director.Loot?.RollShopItem();
                if (item != null) offers.Add(new ShopOffer { Item = item, Price = Mathf.Max(1, Mathf.RoundToInt(item.Price * priceMultiplier)) });
            }
            offers.Add(new ShopOffer { Item = null, Price = potionPrice });
            RefreshIcons();
        }

        void RefreshIcons()
        {
            for (int i = 0; i < slotIcons.Length; i++)
            {
                if (slotIcons[i] == null) continue;
                bool has = i < offers.Count && !offers[i].Sold;
                slotIcons[i].enabled = has;
                if (has) slotIcons[i].sprite = offers[i].IsPotion ? potionIcon : offers[i].Item.Icon;
            }
        }

        public PurchaseResult TryBuy(int index)
        {
            if (index < 0 || index >= offers.Count || director == null || !director.IsPlaying) return PurchaseResult.Invalid;
            var o = offers[index];
            if (o.Sold) return PurchaseResult.AlreadyOwned;
            PurchaseResult r;
            if (o.IsPotion)
            {
                // Bình máu dùng ngay khi mua → chặn mua khi đầy máu (không mất coin vô ích).
                if (!CanUsePotion) return PurchaseResult.Invalid;
                if (!Services.Session.TrySpendRunCoins(o.Price)) return PurchaseResult.NotEnoughCoins;
                director.Player.Heal(LootSpawner.HealthPerPotion);
                r = PurchaseResult.Ok;
            }
            else r = Economy.BuyInRun(o.Item, o.Price);
            if (r == PurchaseResult.Ok) o.Sold = true;
            RefreshIcons();
            return r;
        }

        /// <summary>Player còn sống và chưa đầy máu (bình máu có tác dụng).</summary>
        public bool CanUsePotion
        {
            get
            {
                var p = director != null ? director.Player : null;
                return p != null && !p.IsDead && p.Stats.Hp < p.Stats.MaxHp;
            }
        }

        public string GetInteractPrompt() => promptKey;
        public bool CanInteract(GameObject interactor) => !opening
            && director != null && director.IsPlaying
            && (Services.UI == null || !Services.UI.IsOpen(AddressKeys.UiScreenShop));

        public async void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            opening = true;
            try
            {
                await Services.UI.Open<UIScreen>(AddressKeys.UiScreenShop, new RunShopArgs { Stand = this });
            }
            finally { if (this != null) opening = false; }
        }
    }
}
