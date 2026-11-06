using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    public enum ShopContext { Lobby, Run }

    public enum PurchaseResult { Ok, NotEnoughCoins, AlreadyOwned, Invalid }

    /// <summary>
    /// Giao dịch idempotent (R11): kiểm tra → trừ tiền → thêm đồ trong MỘT hàm đồng bộ, không await ở giữa.
    /// </summary>
    public static class Economy
    {
        /// <summary>Shop sảnh: coin vĩnh viễn → kho.</summary>
        public static PurchaseResult BuyInLobby(ItemData item)
        {
            var save = Services.Save;
            if (item == null || save == null) return PurchaseResult.Invalid;
            var data = save.Data;
            if (data.Owns(item.id)) return PurchaseResult.AlreadyOwned;
            if (data.coins < item.shopPrice) return PurchaseResult.NotEnoughCoins;
            data.coins -= item.shopPrice;
            if (item.IsWeapon) data.ownedWeapons.Add(item.id); else data.ownedEquipment.Add(item.id);
            data.Discover(item.id);
            save.RequestSave(); // giao dịch quan trọng: ghi ngay
            EventBus<WalletChangedEvent>.Raise(new WalletChangedEvent { Coins = data.coins });
            return PurchaseResult.Ok;
        }

        /// <summary>ShopStand trong run: coin run → RunBag.</summary>
        public static PurchaseResult BuyInRun(ItemData item, int price)
        {
            var s = Services.Session;
            if (item == null || s == null) return PurchaseResult.Invalid;
            if (!s.TrySpendRunCoins(price)) return PurchaseResult.NotEnoughCoins;
            s.AddToBag(item.id);
            return PurchaseResult.Ok;
        }

        /// <summary>Mở khóa nhân vật bằng coin vĩnh viễn.</summary>
        public static PurchaseResult UnlockCharacter(CharacterData character)
        {
            var save = Services.Save;
            if (character == null || save == null || !character.released) return PurchaseResult.Invalid; // chưa ra mắt
            var data = save.Data;
            if (data.IsUnlocked(character.characterId)) return PurchaseResult.AlreadyOwned;
            if (data.coins < character.unlockPrice) return PurchaseResult.NotEnoughCoins;
            data.coins -= character.unlockPrice;
            data.unlockedCharacters.Add(character.characterId);
            if (character.startingWeapon != null)
            {
                if (!data.ownedWeapons.Contains(character.startingWeapon.id))
                    data.ownedWeapons.Add(character.startingWeapon.id);
                data.Discover(character.startingWeapon.id);
            }
            save.RequestSave();
            EventBus<WalletChangedEvent>.Raise(new WalletChangedEvent { Coins = data.coins });
            return PurchaseResult.Ok;
        }

        /// <summary>Bắt đầu run với nhân vật đã chọn: chép loadout đã lưu vào SessionData.</summary>
        public static bool StartRun(CharacterData character, ItemCatalog catalog)
        {
            var save = Services.Save;
            var session = Services.Session;
            if (character == null || save == null || session == null) return false;
            if (!character.released) return false; // chưa ra mắt
            if (!save.Data.IsUnlocked(character.characterId)) return false;
            var progress = save.Data.GetCharacter(character.characterId);
            var loadout = progress.loadout.Clone();
            if (catalog != null) DungeonGathering.Inventory.LoadoutRules.Sanitize(loadout, catalog, character);

            // Guard: Phải có ít nhất 1 vũ khí được trang bị trong loadout
            if (string.IsNullOrEmpty(loadout.GetWeapon(0)) && string.IsNullOrEmpty(loadout.GetWeapon(1)))
            {
                Debug.LogWarning("[Economy] Chưa trang bị vũ khí, không thể bắt đầu run.");
                return false;
            }

            session.StartNewRun(character.characterId, loadout, Random.Range(int.MinValue, int.MaxValue));
            save.Data.selectedCharacterId = character.characterId;
            save.RequestSave();
            return true;
        }
    }
}
