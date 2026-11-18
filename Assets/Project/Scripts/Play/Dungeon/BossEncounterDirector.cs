using System;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Điều phối toàn bộ vòng đời của trận chạm trán Boss (SYS-03-ENM):
    /// - Tải trước và quản lý prefab Boss qua Addressables hoặc tham chiếu trực tiếp.
    /// - Sinh Boss tại bossPoint khi người chơi bước vào phòng Boss.
    /// - Xử lý chuyển phase, dọn dẹp phòng và chốt kết quả thắng cuộc an toàn.
    /// </summary>
    public sealed class BossEncounterDirector
    {
        readonly LevelDirector director;

        LevelConfigSO config;
        ModuleScope scope;
        Enemy bossPrefab;
        Enemy bossInstance;
        bool bossLoading;
        bool bossKillPending;
        bool bossDefeatedHandled;
        string bossError;

        public Enemy BossPrefab => bossPrefab;
        public Enemy BossInstance => bossInstance;
        public bool IsLoading => bossLoading;

        public bool HasBossAddress => config != null && !string.IsNullOrEmpty(config.bossAddress) && AddressablesUtil.KeyExists(config.bossAddress);

        public BossEncounterDirector(LevelDirector director)
        {
            this.director = director;
        }

        public void Preload(LevelConfigSO config, ModuleScope scope)
        {
            this.config = config;
            this.scope = scope;
            _ = LoadBoss();
        }

        /// <summary>Tải prefab boss qua Addressables. Gọi lại được (thử lại một lần khi lỗi).</summary>
        public async Awaitable LoadBoss()
        {
            if (config == null || bossLoading || bossPrefab != null) return;
            if (!HasBossAddress)
            {
                bossError = "chưa cấu hình bossAddress hoặc chưa có trong Addressables";
                return;
            }
            bossLoading = true;
            bossError = null;
            try
            {
                var go = await scope.Load<GameObject>(config.bossAddress);
                bossPrefab = go != null ? go.GetComponent<Enemy>() : null;
                if (bossPrefab == null) bossError = "prefab boss thiếu Enemy";
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { bossError = e.Message; }
            finally { bossLoading = false; }
        }

        public async Awaitable<bool> WaitBossLoaded(float timeout)
        {
            var token = director.destroyCancellationToken;
            float deadline = Time.time + timeout;
            try
            {
                while (bossLoading && Time.time < deadline) await Awaitable.NextFrameAsync(token);
            }
            catch (OperationCanceledException) { return false; }
            return true;
        }

        /// <summary>
        /// Chờ lần tải boss KẾT THÚC (thành công hoặc lỗi). Quá timeout mà vẫn đang tải (mạng chậm) → cảnh báo và chờ tiếp,
        /// giữ cửa phòng boss đóng; không coi "chậm" là "lỗi". False nếu director bị hủy.
        /// </summary>
        async Awaitable<bool> WaitBossLoadFinished(float timeout)
        {
            if (!await WaitBossLoaded(timeout)) return false;
            if (!bossLoading) return true;
            Debug.LogWarning($"[Play] Boss tải chậm (>{timeout:0}s) — tiếp tục chờ.");
            return await WaitBossLoaded(float.PositiveInfinity);
        }

        /// <summary>
        /// Phòng boss kích hoạt: chờ boss tải nền (có timeout) rồi spawn. Lỗi/hết giờ → thử tải lại MỘT lần;
        /// vẫn lỗi → chốt thắng (mở Portal) thay vì kẹt run.
        /// </summary>
        public async void SpawnBoss(RoomController room)
        {
            float timeout = config != null ? config.bossLoadTimeout : 20f;
            if (!await WaitBossLoadFinished(timeout)) return;
            if (!director.IsPlaying || room == null) return;

            if (bossPrefab == null && HasBossAddress)
            {
                Debug.LogWarning($"[Play] Tải boss lỗi ({bossError ?? "không rõ"}) — thử lại một lần.");
                _ = LoadBoss();
                if (!await WaitBossLoadFinished(timeout)) return;
                if (!director.IsPlaying || room == null) return;
            }

            // Chỉ chốt thắng khi tải LỖI thật sự (không phải chỉ chậm) — tránh được thưởng thắng mà không đánh boss.
            if (bossPrefab == null)
            {
                Debug.LogError($"[Play] Không spawn được boss: {bossError ?? "chưa cấu hình"} — mở cửa/Portal để không kẹt run.");
                room.ClearOnBossDefeated();
                return;
            }

            var t = room.Template;
            Vector3 at = t.bossPoint != null ? t.bossPoint.position : t.transform.position;
            var boss = UnityEngine.Object.Instantiate(bossPrefab, at, Quaternion.identity, room.transform);
            boss.OnSpawn();
            boss.Init(room.CreateSpawnContext());
            room.Register(boss);
            bossInstance = boss;
            if (Services.Session != null && boss.Data != null) Services.Session.currentBossNameKey = boss.Data.nameKey;
            // Dự phòng khi prefab boss không có BossPhaseController (không phát BossDefeatedEvent).
            // Died gọi trước EnemyDiedEvent nên KillAll ở đây không gây Raise lồng EnemyDiedEvent.
            boss.Died += OnBossInstanceDied;
            room.WavesRemaining = false;
            var phases = boss.GetComponent<BossPhaseController>();
            if (phases != null) phases.Publish();
        }

        void OnBossInstanceDied(Enemy e) => HandleBossKilled();

        /// <summary>Boss chết (BossDefeatedEvent / Enemy.Died / EnemyDiedEvent.IsBoss) → dọn quái con + mở phòng Boss + dựng Portal. Idempotent.</summary>
        public void HandleBossKilled()
        {
            if (!director.IsPlaying || bossDefeatedHandled) return;
            bossDefeatedHandled = true;
            bossKillPending = false;
            var bossRoom = director.Builder != null ? director.Builder.BossRoom : null;
            if (bossRoom != null)
            {
                bossRoom.KillAll();
                bossRoom.ClearOnBossDefeated();
            }
            else
            {
                director.Finish(true);
            }
        }

        public void NotifyEnemyDied(EnemyDiedEvent e)
        {
            // Dự phòng: quái IsBoss chết trong phòng Boss. Không KillAll ngay (sẽ Raise lồng EnemyDiedEvent) → xử lý ở frame sau.
            if (director.IsPlaying && e.IsBoss && director.Builder != null && director.Builder.BossRoom != null
                && (e.Enemy == bossInstance || RoomController.FindAt(e.Position) == director.Builder.BossRoom))
            {
                bossKillPending = true;
            }
        }

        public void NotifyBossDefeated() => HandleBossKilled();

        public void Tick()
        {
            if (bossKillPending)
            {
                bossKillPending = false;
                HandleBossKilled();
            }
        }
    }
}
