using System;
using System.Collections.Generic;

namespace DungeonGathering.Core
{
    [Serializable]
    public class SettingsData
    {
        public int bgmVolume = 7;     // 0..10 nấc
        public int sfxVolume = 8;
        public bool autoAim = true;
        public bool vibration = true;
        public string language = "";  // rỗng = theo máy
    }

    [Serializable]
    public class CharacterProgress
    {
        public string characterId;
        public int level;
        public LoadoutData loadout = new();
    }

    [Serializable]
    public class StatsData
    {
        public int totalRuns;
        public int highestLevelReached;
        public int totalKills;
        public int victories;
    }

    /// <summary>POCO lưu JSON (ARCHITECTURE §9). Chỉ lưu id ngắn, không lưu address/GUID.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;
        /// <summary>Nhân vật mặc định luôn được mở khóa.</summary>
        public const string DefaultCharacterId = "knight";

        public int version = CurrentVersion;
        public int coins;
        public int gems;
        public string selectedCharacterId = "knight";
        public List<CharacterProgress> characters = new();
        public List<string> unlockedCharacters = new() { "knight" };
        public List<string> ownedWeapons = new() { "iron" };
        public List<string> ownedEquipment = new();
        public List<string> discovered = new();
        public SettingsData settings = new();
        public StatsData stats = new();

        public bool IsUnlocked(string characterId) => unlockedCharacters.Contains(characterId);

        public CharacterProgress GetCharacter(string characterId, bool create = true)
        {
            foreach (var c in characters)
                if (c.characterId == characterId) { c.loadout ??= new LoadoutData(); c.loadout.Normalize(); return c; }
            if (!create) return null;
            var cp = new CharacterProgress { characterId = characterId };
            characters.Add(cp);
            return cp;
        }

        public bool Owns(string itemId) => ownedWeapons.Contains(itemId) || ownedEquipment.Contains(itemId);

        public void Discover(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId) && !discovered.Contains(itemId)) discovered.Add(itemId);
        }

        /// <summary>Sửa dữ liệu thiếu sau khi đọc file cũ.</summary>
        public void Upgrade()
        {
            characters ??= new();
            unlockedCharacters ??= new();
            unlockedCharacters.RemoveAll(string.IsNullOrEmpty);
            if (!unlockedCharacters.Contains(DefaultCharacterId)) unlockedCharacters.Insert(0, DefaultCharacterId);
            if (string.IsNullOrEmpty(selectedCharacterId) || !unlockedCharacters.Contains(selectedCharacterId))
                selectedCharacterId = DefaultCharacterId;
            ownedWeapons ??= new();
            if (ownedWeapons.Count == 0) ownedWeapons.Add("iron");
            ownedEquipment ??= new();
            discovered ??= new();
            settings ??= new();
            stats ??= new();
            foreach (var c in characters)
            {
                c.loadout ??= new LoadoutData();
                c.loadout.Normalize();
                if (!unlockedCharacters.Contains(c.characterId))
                {
                    c.loadout.weapons[0] = null;
                    c.loadout.weapons[1] = null;
                }
                else if (c.loadout.weapons != null && c.loadout.weapons.Length >= 2 && c.loadout.weapons[1] == c.loadout.weapons[0])
                {
                    c.loadout.weapons[1] = null;
                }
            }
            version = CurrentVersion;
        }
    }
}
