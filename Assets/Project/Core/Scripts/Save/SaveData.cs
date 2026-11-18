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
        /// <summary>v2: hệ dữ liệu mới (thư viện vũ khí, bậc D–S, định nghĩa hero / quái). Save v1 bị reset tiến trình.</summary>
        public const int CurrentVersion = 2;
        /// <summary>Vũ khí khởi đầu của hero mặc định (Knight).</summary>
        public const string DefaultWeaponId = "sword-iron";
        /// <summary>Nhân vật mặc định luôn được mở khóa.</summary>
        public const string DefaultCharacterId = "knight";

        public int version = CurrentVersion;
        public int coins;
        public int gems;
        public string selectedCharacterId = "knight";
        public List<CharacterProgress> characters = new();
        public List<string> unlockedCharacters = new() { "knight" };
        public List<string> ownedWeapons = new() { DefaultWeaponId };
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

        /// <summary>
        /// true khi file save do bản game MỚI hơn ghi (version &gt; CurrentVersion): SaveSystem không được ghi đè,
        /// tránh mất dữ liệu bản mới không hiểu. Không serialize (auto-property).
        /// </summary>
        public bool FromNewerVersion { get; private set; }

        public bool Owns(string itemId) => ownedWeapons.Contains(itemId) || ownedEquipment.Contains(itemId);

        public void Discover(string itemId)
        {
            if (!string.IsNullOrEmpty(itemId) && !discovered.Contains(itemId)) discovered.Add(itemId);
        }

        /// <summary>
        /// Save bản cũ (trước v2) không tương thích hệ dữ liệu mới → xóa tiến trình (coin, nhân vật, đồ) về mặc định,
        /// GIỮ cài đặt (âm lượng, ngôn ngữ, auto-aim…). Đã thống nhất: reset thay vì chuyển đổi.
        /// </summary>
        void ResetProgressForNewVersion()
        {
            UnityEngine.Debug.Log($"[Save] Save v{version} → v{CurrentVersion}: reset tiến trình, giữ cài đặt.");
            var keepSettings = settings ?? new SettingsData();
            coins = 0;
            gems = 0;
            selectedCharacterId = DefaultCharacterId;
            characters = new List<CharacterProgress>();
            unlockedCharacters = new List<string> { DefaultCharacterId };
            ownedWeapons = new List<string> { DefaultWeaponId };
            ownedEquipment = new List<string>();
            discovered = new List<string>();
            stats = new StatsData();
            settings = keepSettings;
        }

        /// <summary>Sửa dữ liệu thiếu sau khi đọc file cũ.</summary>
        public void Upgrade()
        {
            FromNewerVersion = version > CurrentVersion;
            if (!FromNewerVersion && version < CurrentVersion) ResetProgressForNewVersion();
            if (FromNewerVersion)
                UnityEngine.Debug.LogWarning($"[Save] File save version {version} mới hơn bản game (v{CurrentVersion}) — chỉ đọc, sẽ không ghi đè.");
            characters ??= new();
            unlockedCharacters ??= new();
            unlockedCharacters.RemoveAll(string.IsNullOrEmpty);
            if (!unlockedCharacters.Contains(DefaultCharacterId)) unlockedCharacters.Insert(0, DefaultCharacterId);
            if (string.IsNullOrEmpty(selectedCharacterId) || !unlockedCharacters.Contains(selectedCharacterId))
                selectedCharacterId = DefaultCharacterId;
            ownedWeapons ??= new();
            ownedWeapons.RemoveAll(id => id == "iron");   // id cũ không tồn tại
            if (ownedWeapons.Count == 0) ownedWeapons.Add(DefaultWeaponId);
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
            if (!FromNewerVersion) version = CurrentVersion;   // không hạ version của save bản mới
        }
    }
}
