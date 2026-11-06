using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Các bước chuyển run dùng chung cho Results/Pause/Lobby (không module nào phải tham chiếu module khác).
    /// Quy đổi phần thưởng đúng một lần (SessionData.settled — R11).
    /// </summary>
    public static class RunFlow
    {
        /// <summary>Tỉ lệ quy đổi coin run → coin vĩnh viễn khi thắng / thua. Nguồn DUY NHẤT (Settle + màn kết quả).</summary>
        public static float VictoryCoinRate = 1f;
        public static float DefeatCoinRate = 0.5f;
        /// <summary>Thưởng mỗi tầng đã tới (× floor) và thưởng hạ boss (chỉ khi thắng).</summary>
        public static int FloorBonusPerFloor = 20;
        public static int BossBonus = 100;
        /// <summary>Thua có giữ đồ trong RunBag không (tài liệu: chưa chốt) — mặc định không giữ.</summary>
        public static bool KeepBagOnDefeat = true;

        /// <summary>Phần thưởng quy đổi của run (Results hiển thị, Settle cộng — cùng một công thức).</summary>
        public readonly struct RunReward
        {
            public readonly int RunCoins;
            public readonly int FloorBonus;
            public readonly int BossBonus;
            public int Total => RunCoins + FloorBonus + BossBonus;

            public RunReward(int runCoins, int floorBonus, int bossBonus)
            {
                RunCoins = runCoins;
                FloorBonus = floorBonus;
                BossBonus = bossBonus;
            }
        }

        public static RunReward ComputeReward(SessionData s, bool victory)
        {
            if (s == null) return default;
            float rate = victory ? VictoryCoinRate : DefeatCoinRate;
            return new RunReward(
                Mathf.FloorToInt(Mathf.Max(0, s.runCoins) * rate),
                Mathf.Max(0, s.floor) * FloorBonusPerFloor,
                victory ? BossBonus : 0);
        }

        /// <summary>Chốt phần thưởng run vào SaveData. Trả false nếu đã chốt rồi / không có run đang chạy.</summary>
        public static bool Settle()
        {
            var s = Services.Session;
            var save = Services.Save;
            if (s == null || save == null || s.settled || !s.runActive) return false;
            s.settled = true; // đặt TRƯỚC khi cộng

            var data = save.Data;
            bool victory = s.lastRunVictory;
            data.coins += ComputeReward(s, victory).Total;
            if (victory || KeepBagOnDefeat)
            {
                foreach (var id in s.runBag)
                {
                    // Phân loại vũ khí/trang bị do Catalog quyết định; ở Core lưu theo tiền tố đã đăng ký.
                    if (ItemIdClassifier.IsWeapon(id)) { if (!data.ownedWeapons.Contains(id)) data.ownedWeapons.Add(id); }
                    else if (!data.ownedEquipment.Contains(id)) data.ownedEquipment.Add(id);
                    data.Discover(id);
                }
            }
            data.stats.totalRuns++;
            data.stats.totalKills += s.kills;
            if (victory) data.stats.victories++;
            if (s.floor > data.stats.highestLevelReached) data.stats.highestLevelReached = s.floor;
            s.runActive = false;
            save.RequestSave();
            EventBus<WalletChangedEvent>.Raise(new WalletChangedEvent { Coins = data.coins });
            return true;
        }

        /// <summary>Chốt run (nếu còn) rồi về sảnh. Trả false nếu đang chuyển scene (không làm gì).</summary>
        /// <summary>Bỏ cuộc giữa chừng: KHÔNG giữ túi đồ (runBag), chốt run và lưu (SYS-04-COR).</summary>
        public static bool Abandon()
        {
            var s = Services.Session;
            if (s == null || s.settled || !s.runActive) return false;
            s.lastRunVictory = false;
            s.ClearBag(); // Luật chơi: Thoát giữa chừng mất túi đồ
            return Settle();
        }

        /// <summary>Bỏ cuộc giữa run: xóa túi đồ rồi chốt run về sảnh (SYS-01-INV).</summary>
        public static bool AbandonToLobby()
        {
            if (Services.Scenes == null || Services.Scenes.IsTransitioning) return false;
            Abandon();
            _ = Services.Scenes.Go(AddressKeys.SceneLobby);
            return true;
        }

        public static bool ReturnToLobby()
        {
            if (Services.Scenes == null || Services.Scenes.IsTransitioning) return false;
            Settle();
            _ = Services.Scenes.Go(AddressKeys.SceneLobby);
            return true;
        }

        /// <summary>Thắng → sang tầng kế, giữ coin + túi + chỉ số.</summary>
        public static void ContinueNextFloor()
        {
            var s = Services.Session;
            if (Services.Scenes.IsTransitioning || s == null || s.settled) return;
            if (s.floor >= s.maxFloor) { ReturnToLobby(); return; }
            s.AdvanceFloor();
            _ = Services.Scenes.Go(AddressKeys.ScenePlay);
        }

        /// <summary>Thua → chơi lại từ tầng 1 với cùng nhân vật + loadout.</summary>
        public static void Retry()
        {
            var s = Services.Session;
            if (Services.Scenes.IsTransitioning || s == null) return;
            Settle();
            s.StartNewRun(s.characterId, s.loadout, Random.Range(int.MinValue, int.MaxValue));
            _ = Services.Scenes.Go(AddressKeys.ScenePlay);
        }
    }

    /// <summary>Catalog đăng ký hàm phân loại id vũ khí khi tải (Core không biết WeaponData).</summary>
    public static class ItemIdClassifier
    {
        public static System.Func<string, bool> WeaponPredicate;
        public static bool IsWeapon(string id) => WeaponPredicate != null && WeaponPredicate(id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => WeaponPredicate = null;
    }
}
