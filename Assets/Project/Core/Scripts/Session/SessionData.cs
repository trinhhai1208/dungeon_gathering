using System;
using System.Collections.Generic;

namespace DungeonGathering.Core
{
    /// <summary>2 vũ khí + 6 ô trang bị (id ngắn). Dùng cả trong SaveData (theo nhân vật) và SessionData.</summary>
    [Serializable]
    public class LoadoutData
    {
        public const int WeaponSlots = 2;
        public const int EquipmentSlots = 6;

        public string[] weapons = new string[WeaponSlots];
        public string[] equipment = new string[EquipmentSlots];

        public string GetEquipment(EquipmentSlotType slot) => Get(equipment, (int)slot);
        public void SetEquipment(EquipmentSlotType slot, string id) { Normalize(); equipment[(int)slot] = id; }
        public string GetWeapon(int slot) => Get(weapons, slot);
        public void SetWeapon(int slot, string id) { Normalize(); weapons[slot] = id; }

        public IEnumerable<string> AllIds()
        {
            Normalize();
            foreach (var w in weapons) if (!string.IsNullOrEmpty(w)) yield return w;
            foreach (var e in equipment) if (!string.IsNullOrEmpty(e)) yield return e;
        }

        public void Normalize()
        {
            if (weapons == null || weapons.Length != WeaponSlots) Array.Resize(ref weapons, WeaponSlots);
            if (equipment == null || equipment.Length != EquipmentSlots) Array.Resize(ref equipment, EquipmentSlots);
        }

        public LoadoutData Clone()
        {
            Normalize();
            return new LoadoutData { weapons = (string[])weapons.Clone(), equipment = (string[])equipment.Clone() };
        }

        static string Get(string[] arr, int i) => arr != null && i >= 0 && i < arr.Length ? arr[i] : null;
    }

    /// <summary>HP/Giáp/Mana mang sang tầng kế (Tiếp tục). hasValues=false → Player khởi tạo đầy.</summary>
    [Serializable]
    public class RunStats
    {
        public bool hasValues;
        public float hp, armor, mana;
        public int activeWeaponSlot;
    }

    /// <summary>
    /// Dữ liệu của run hiện tại (ARCHITECTURE §1, §9) — không ghi ra file.
    /// Tham số giữa scene đi qua đây, không qua static rải rác.
    /// </summary>
    public sealed class SessionData
    {
        public string characterId;
        public string themeId = "dungeon";
        public int floor = 1;
        public int maxFloor = 3;
        public int seed;
        public LoadoutData loadout = new();
        public RunStats runStats = new();
        public int runCoins;
        public readonly List<string> runBag = new();
        public int kills;
        public float runTime;
        public bool runActive;
        /// <summary>Kết quả run đã chốt (LevelDirector) — Results đọc.</summary>
        public bool lastRunVictory;
        /// <summary>Đã quy đổi phần thưởng (R11). Reset khi bắt đầu run mới.</summary>
        public bool settled;

        public void StartNewRun(string character, LoadoutData fromLoadout, int newSeed)
        {
            characterId = character;
            loadout = fromLoadout != null ? fromLoadout.Clone() : new LoadoutData();
            floor = 1;
            seed = newSeed;
            runStats = new RunStats();
            runCoins = 0;
            runBag.Clear();
            kills = 0;
            runTime = 0f;
            runActive = true;
            settled = false;
            lastRunVictory = false;
        }

        /// <summary>Tiếp tục tầng kế: giữ coin, túi, chỉ số.</summary>
        public void AdvanceFloor()
        {
            floor++;
            seed = unchecked(seed * 486187739 + 1013904223);
            runActive = true;
        }

        public void AddRunCoins(int amount)
        {
            if (amount == 0) return;
            runCoins = Math.Max(0, runCoins + amount);
            EventBus<RunCoinsChangedEvent>.Raise(new RunCoinsChangedEvent { Coins = runCoins });
        }

        /// <summary>Trừ coin run (ShopStand). Trả false nếu không đủ.</summary>
        public bool TrySpendRunCoins(int amount)
        {
            if (amount < 0 || runCoins < amount) return false;
            AddRunCoins(-amount);
            return true;
        }
    }
}
