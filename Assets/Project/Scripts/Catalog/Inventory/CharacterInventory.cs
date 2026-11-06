using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;

namespace DungeonGathering.Inventory
{
    /// <summary>
    /// Quản lý trang bị, vũ khí và kho đồ ở tầng Sảnh & Dữ liệu (SYS-01-INV).
    /// Single Source of Truth cho CharacterPanel, WeaponScreen, EquipmentScreen.
    /// </summary>
    public class CharacterInventory
    {
        readonly string characterId;
        readonly SaveData saveData;
        readonly ItemCatalog catalog;
        readonly CharacterData character;
        readonly CharacterProgress progress;
        readonly LoadoutData loadout;

        public string CharacterId => characterId;
        public CharacterData Character => character;
        public LoadoutData Loadout => loadout;
        public SaveData SaveData => saveData;
        public ItemCatalog Catalog => catalog;

        public WeaponData Weapon1 => catalog != null ? catalog.Get<WeaponData>(loadout.GetWeapon(0)) : null;
        public WeaponData Weapon2 => catalog != null ? catalog.Get<WeaponData>(loadout.GetWeapon(1)) : null;
        public bool HasAnyWeapon => !string.IsNullOrEmpty(loadout.GetWeapon(0)) || !string.IsNullOrEmpty(loadout.GetWeapon(1));

        public EquipmentData GetEquipment(EquipmentSlotType slot) =>
            catalog != null ? catalog.Get<EquipmentData>(loadout.GetEquipment(slot)) : null;

        public int ActiveCount => LoadoutRules.CountActive(loadout, catalog);
        public int MaxActive => LoadoutRules.MaxActiveItems;

        public CharacterInventory(string characterId, SaveData saveData = null, ItemCatalog catalog = null)
        {
            this.characterId = string.IsNullOrEmpty(characterId) ? SaveData.DefaultCharacterId : characterId;
            this.saveData = saveData ?? Services.Save?.Data;
            this.catalog = catalog ?? ItemCatalog.Current;
            this.character = this.catalog != null ? this.catalog.GetCharacter(this.characterId) : null;
            bool isUnlocked = this.saveData == null || this.saveData.IsUnlocked(this.characterId);
            this.progress = isUnlocked ? this.saveData?.GetCharacter(this.characterId, true) : null;
            this.loadout = this.progress?.loadout ?? new LoadoutData();
            if (isUnlocked)
            {
                EnsureDefaultLoadout();
            }
        }

        public void EnsureDefaultLoadout()
        {
            loadout.Normalize();
            if (saveData != null && !saveData.IsUnlocked(characterId))
                return;

            // Đảm bảo startingWeapon nằm trong kho đồ sở hữu (túi đồ sảnh) để người chơi có thể tự trang bị
            if (character != null && character.startingWeapon != null && saveData != null)
            {
                if (!saveData.ownedWeapons.Contains(character.startingWeapon.id))
                {
                    saveData.ownedWeapons.Add(character.startingWeapon.id);
                    saveData.Discover(character.startingWeapon.id);
                    Services.Save?.RequestSave();
                }
            }
            if (loadout.GetWeapon(1) == loadout.GetWeapon(0))
            {
                loadout.SetWeapon(1, null);
            }
        }

        public bool CanEquipWeapon(WeaponData weapon, int slot, out string reasonKey)
        {
            reasonKey = string.Empty;
            if (weapon == null || catalog == null) return false;
            if (!LoadoutRules.CanPlace(loadout, catalog, weapon, slot))
            {
                reasonKey = "equip.limit_warn";
                return false;
            }
            return true;
        }

        public bool EquipWeapon(int slot, string weaponId)
        {
            if (catalog == null) return false;
            var weapon = catalog.Get<WeaponData>(weaponId);
            if (weapon == null) return false;
            if (!LoadoutRules.TryPlace(loadout, catalog, weapon, slot)) return false;

            Services.Save?.RequestSave();
            EventBus<EquipmentChangedEvent>.Raise(new EquipmentChangedEvent { CharacterId = characterId });
            return true;
        }

        public bool UnequipWeapon(int slot)
        {
            if (slot < 0 || slot >= LoadoutData.WeaponSlots) return false;
            loadout.SetWeapon(slot, null);
            Services.Save?.RequestSave();
            EventBus<EquipmentChangedEvent>.Raise(new EquipmentChangedEvent { CharacterId = characterId });
            return true;
        }

        public bool CanEquipItem(EquipmentData item, out string reasonKey)
        {
            reasonKey = string.Empty;
            if (item == null || catalog == null) return false;
            if (!LoadoutRules.CanPlace(loadout, catalog, item, (int)item.slot))
            {
                reasonKey = "equip.limit_warn";
                return false;
            }
            return true;
        }

        public bool EquipItem(string equipId)
        {
            if (catalog == null) return false;
            var item = catalog.Get<EquipmentData>(equipId);
            if (item == null) return false;
            if (!LoadoutRules.TryPlace(loadout, catalog, item, (int)item.slot)) return false;

            Services.Save?.RequestSave();
            EventBus<EquipmentChangedEvent>.Raise(new EquipmentChangedEvent { CharacterId = characterId, Slot = item.slot });
            return true;
        }

        public bool UnequipItem(EquipmentSlotType slot)
        {
            loadout.SetEquipment(slot, null);
            Services.Save?.RequestSave();
            EventBus<EquipmentChangedEvent>.Raise(new EquipmentChangedEvent { CharacterId = characterId, Slot = slot });
            return true;
        }

        public IEnumerable<string> GetOwnedWeapons() =>
            saveData != null ? saveData.ownedWeapons : Array.Empty<string>();

        public IEnumerable<string> GetOwnedEquipment() =>
            saveData != null ? saveData.ownedEquipment : Array.Empty<string>();
    }
}
