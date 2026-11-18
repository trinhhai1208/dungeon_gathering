using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;

namespace DungeonGathering.Inventory
{
    /// <summary>
    /// Kho đồ + loadout của một hero ở sảnh (thay CharacterInventory). Nguồn duy nhất cho CharacterPanel, màn Vũ khí,
    /// màn Trang bị.
    /// - Chỉ xem thì KHÔNG ghi save: hero chưa có tiến trình dùng loadout tạm trong bộ nhớ, vũ khí khởi đầu được coi như
    ///   đã sở hữu để hiển thị; chỉ khi người chơi thật sự trang bị / tháo mới tạo tiến trình và lưu.
    /// - Mọi thay đổi đi qua <see cref="LoadoutRules"/>; phát <see cref="Changed"/> + EquipmentChangedEvent.
    /// </summary>
    public sealed class InventoryModel
    {
        readonly string characterId;
        readonly SaveData saveData;
        readonly ItemCatalog catalog;
        readonly HeroDefinitionSO character;
        readonly bool unlocked;
        CharacterProgress progress;
        LoadoutData loadout;

        /// <summary>Loadout / kho đổi (trang bị, tháo).</summary>
        public event Action Changed;

        public string CharacterId => characterId;
        public HeroDefinitionSO Character => character;
        public LoadoutData Loadout => loadout;
        public SaveData SaveData => saveData;
        public ItemCatalog Catalog => catalog;
        public bool IsUnlocked => unlocked;

        public WeaponData Weapon1 => catalog != null ? catalog.Get<WeaponData>(loadout.GetWeapon(0)) : null;
        public WeaponData Weapon2 => catalog != null ? catalog.Get<WeaponData>(loadout.GetWeapon(1)) : null;
        public bool HasAnyWeapon => !string.IsNullOrEmpty(loadout.GetWeapon(0)) || !string.IsNullOrEmpty(loadout.GetWeapon(1));
        public EquipmentSO GetEquipment(EquipmentSlotType slot) => catalog != null ? catalog.Get<EquipmentSO>(loadout.GetEquipment(slot)) : null;
        public int ActiveCount => LoadoutRules.CountActive(loadout, catalog);
        public int MaxActive => LoadoutRules.MaxActiveItems;

        public InventoryModel(string characterId, SaveData saveData = null, ItemCatalog catalog = null)
        {
            this.characterId = string.IsNullOrEmpty(characterId) ? SaveData.DefaultCharacterId : characterId;
            this.saveData = saveData ?? Services.Save?.Data;
            this.catalog = catalog ?? ItemCatalog.Current;
            character = this.catalog != null ? this.catalog.GetCharacter(this.characterId) : null;
            unlocked = this.saveData == null || this.saveData.IsUnlocked(this.characterId);
            progress = unlocked ? this.saveData?.GetCharacter(this.characterId, create: false) : null;
            loadout = progress?.loadout?.Clone() ?? DefaultLoadout();
            if (progress != null) loadout = progress.loadout; // đã có tiến trình → sửa trực tiếp
            loadout.Normalize();
            if (!string.IsNullOrEmpty(loadout.GetWeapon(0)) && loadout.GetWeapon(1) == loadout.GetWeapon(0)) loadout.SetWeapon(1, null);
        }

        /// <summary>Loadout tạm khi hero chưa có tiến trình: cầm vũ khí khởi đầu (chưa lưu).</summary>
        LoadoutData DefaultLoadout()
        {
            var l = new LoadoutData();
            if (character != null && character.startingWeapon != null) l.SetWeapon(0, character.startingWeapon.id);
            return l;
        }

        /// <summary>Lần đầu người chơi thay đổi: tạo tiến trình trong save, gắn loadout tạm vào, đảm bảo sở hữu vũ khí khởi đầu.</summary>
        bool EnsureWritable()
        {
            if (!unlocked || saveData == null) return false;
            if (progress == null)
            {
                progress = saveData.GetCharacter(characterId, create: true);
                progress.loadout = loadout;
            }
            if (character != null && character.startingWeapon != null && !saveData.ownedWeapons.Contains(character.startingWeapon.id))
            {
                saveData.ownedWeapons.Add(character.startingWeapon.id);
                saveData.Discover(character.startingWeapon.id);
            }
            return true;
        }

        void Commit(EquipmentSlotType? slot = null)
        {
            Services.Save?.RequestSave();
            Changed?.Invoke();
            EventBus<EquipmentChangedEvent>.Raise(new EquipmentChangedEvent { CharacterId = characterId, Slot = slot ?? default });
        }

        public bool CanEquipWeapon(WeaponData weapon, int slot, out string reasonKey)
        {
            reasonKey = string.Empty;
            if (weapon == null || catalog == null || !unlocked) return false;
            if (!LoadoutRules.CanPlace(loadout, catalog, weapon, slot)) { reasonKey = "equip.limit_warn"; return false; }
            return true;
        }

        public bool EquipWeapon(int slot, string weaponId)
        {
            if (catalog == null || !Owns(weaponId)) return false;
            var weapon = catalog.Get<WeaponData>(weaponId);
            if (weapon == null || !EnsureWritable()) return false;
            if (!LoadoutRules.TryPlace(loadout, catalog, weapon, slot)) return false;
            Commit();
            return true;
        }

        public bool UnequipWeapon(int slot)
        {
            if (slot < 0 || slot >= LoadoutData.WeaponSlots || !EnsureWritable()) return false;
            loadout.SetWeapon(slot, null);
            Commit();
            return true;
        }

        public bool CanEquipItem(EquipmentSO item, out string reasonKey)
        {
            reasonKey = string.Empty;
            if (item == null || catalog == null || !unlocked) return false;
            if (!LoadoutRules.CanPlace(loadout, catalog, item, (int)item.slot)) { reasonKey = "equip.limit_warn"; return false; }
            return true;
        }

        public bool EquipItem(string equipId)
        {
            if (catalog == null || !Owns(equipId)) return false;
            var item = catalog.Get<EquipmentSO>(equipId);
            if (item == null || !EnsureWritable()) return false;
            if (!LoadoutRules.TryPlace(loadout, catalog, item, (int)item.slot)) return false;
            Commit(item.slot);
            return true;
        }

        public bool UnequipItem(EquipmentSlotType slot)
        {
            if (!EnsureWritable()) return false;
            loadout.SetEquipment(slot, null);
            Commit(slot);
            return true;
        }

        /// <summary>Có sở hữu không (vũ khí khởi đầu của hero luôn tính là có).</summary>
        public bool Owns(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            if (character != null && character.startingWeapon != null && character.startingWeapon.id == itemId) return true;
            return saveData == null || saveData.Owns(itemId);
        }

        public IEnumerable<string> GetOwnedWeapons()
        {
            if (saveData == null) yield break;
            foreach (var id in saveData.ownedWeapons) yield return id;
            var start = character != null && character.startingWeapon != null ? character.startingWeapon.id : null;
            if (!string.IsNullOrEmpty(start) && !saveData.ownedWeapons.Contains(start)) yield return start;
        }

        public IEnumerable<string> GetOwnedEquipment() =>
            saveData != null ? saveData.ownedEquipment : (IEnumerable<string>)Array.Empty<string>();
    }
}
