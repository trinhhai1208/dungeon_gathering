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

        DungeonConfig config;
        ModuleScope scope;
        Enemy bossPrefab;
        Enemy bossInstance;
        bool bossLoading;
        bool bossKillPending;
        string bossError;

        public Enemy BossPrefab => bossPrefab;
        public Enemy BossInstance => bossInstance;
        public bool IsLoading => bossLoading;

        public bool HasBossAddress => config != null && !string.IsNullOrEmpty(config.bossAddress) && AddressablesUtil.KeyExists(config.bossAddress);

        public BossEncounterDirector(LevelDirector director)
        {
            this.director = director;
        }

        public void Preload(DungeonConfig config, ModuleScope scope)
        {
            this.config = config;
            this.scope = scope;
            _ = LoadBoss();
        }

        /// <summary>Tải prefab boss (address hoặc tham chiếu trực tiếp). Gọi lại được (thử lại một lần khi lỗi).</summary>
        public async Awaitable LoadBoss()
        {
            if (config == null) return;
            if (!HasBossAddress)
            {
                bossPrefab = config.bossPrefab;
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
        /// Phòng boss kích hoạt: chờ boss tải nền (có timeout) rồi spawn. Lỗi/hết giờ → thử tải lại MỘT lần;
        /// vẫn lỗi → chốt thắng (mở Portal) thay vì kẹt run.
        /// </summary>
        public async void SpawnBoss(RoomController room)
        {
            float timeout = config != null ? config.bossLoadTimeout : 20f;
            if (!await WaitBossLoaded(timeout)) return;
            if (!director.IsPlaying || room == null) return;

            if (bossPrefab == null && !bossLoading && HasBossAddress)
            {
                Debug.LogWarning($"[Play] Tải boss lỗi ({bossError ?? "không rõ"}) — thử lại một lần.");
                _ = LoadBoss();
                if (!await WaitBossLoaded(timeout)) return;
                if (!director.IsPlaying || room == null) return;
            }

            if (bossPrefab == null)
            {
                Debug.LogError($"[Play] Không spawn được boss: {(bossLoading ? "hết thời gian chờ tải" : bossError ?? "chưa cấu hình")} — chốt thắng để không kẹt run.");
                room.WavesRemaining = false;
                director.Finish(true);
                return;
            }

            var t = room.Template;
            Vector3 at = t.bossPoint != null ? t.bossPoint.position : t.transform.position;
            var boss = UnityEngine.Object.Instantiate(bossPrefab, at, Quaternion.identity, room.transform);
            boss.OnSpawn();
            boss.Init(room.CreateSpawnContext());
            room.Register(boss);
            bossInstance = boss;
            // Dự phòng khi prefab boss không có BossPhaseController (không phát BossDefeatedEvent).
            // Died gọi trước EnemyDiedEvent nên KillAll ở đây không gây Raise lồng EnemyDiedEvent.
            boss.Died += OnBossInstanceDied;
            room.WavesRemaining = false;
            var phases = boss.GetComponent<BossPhaseController>();
            if (phases != null) phases.Publish();
        }

        void OnBossInstanceDied(Enemy e) => HandleBossKilled();

        /// <summary>Boss chết (BossDefeatedEvent / Enemy.Died / EnemyDiedEvent.IsBoss) → dọn quái con + chốt thắng. Idempotent.</summary>
        public void HandleBossKilled()
        {
            if (!director.IsPlaying) return;
            if (director.Builder != null && director.Builder.BossRoom != null)
                director.Builder.BossRoom.KillAll();
            director.Finish(true);
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
