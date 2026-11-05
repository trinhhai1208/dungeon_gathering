using System;
using System.Collections.Generic;
using System.Threading;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Sinh quái theo đợt cho một phòng (ENTITIES §3.2): Fixed Waves hoặc Budget Waves, mỗi quái có vòng cảnh báo 1s.
    /// Lúc đặt marker: pendingSpawns++; lúc quái xuất hiện: pendingSpawns-- + alive.Add (cùng một hàm).
    /// Phòng/run kết thúc trong lúc chờ → hủy marker, không spawn.
    /// </summary>
    public sealed class EnemySpawner
    {
        readonly RoomController room;
        readonly DungeonConfig config;
        readonly System.Random rng;
        readonly PlayPrefabSet prefabs;
        readonly ModuleScope scope;
        readonly CancellationTokenSource cts = new();

        public EnemySpawner(RoomController room, DungeonConfig config, System.Random rng, PlayPrefabSet prefabs, ModuleScope scope)
        {
            this.room = room;
            this.config = config;
            this.rng = rng;
            this.prefabs = prefabs;
            this.scope = scope;
        }

        public void Stop()
        {
            if (!cts.IsCancellationRequested) cts.Cancel();
        }

        public async void Run()
        {
            var waves = BuildWaves();
            room.WavesRemaining = waves.Count > 0;
            if (waves.Count == 0) { room.CheckCleared(); return; }
            var token = cts.Token;
            try
            {
                for (int w = 0; w < waves.Count; w++)
                {
                    foreach (var (prefab, pos) in waves[w]) SpawnOne(prefab, pos, token); // pending++ đồng bộ
                    if (w == waves.Count - 1) room.WavesRemaining = false;
                    // Chờ đợt này dọn xong rồi mới sang đợt kế.
                    while (room.AliveCount > 0 || room.PendingSpawns > 0)
                        await Awaitable.NextFrameAsync(token);
                    if (w < waves.Count - 1) await Awaitable.WaitForSecondsAsync(0.6f, token);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                if (room != null) room.CheckCleared();
            }
        }

        async void SpawnOne(Enemy prefab, Vector2 position, CancellationToken token)
        {
            room.BeginPendingSpawn();
            GameObject marker = prefabs != null && prefabs.spawnMarker != null
                ? Services.Pool.Get(prefabs.spawnMarker.gameObject, scope, position, Quaternion.identity)
                : null;
            try
            {
                await Awaitable.WaitForSecondsAsync(config != null ? config.spawnWarningTime : 1f, token);
            }
            catch (OperationCanceledException)
            {
                if (marker != null) Services.Pool?.Return(marker);
                if (room != null) room.CancelPendingSpawn();
                return;
            }
            if (marker != null) Services.Pool.Return(marker);
            if (room == null || scope.IsDisposed || room.Director == null || !room.Director.IsPlaying)
            {
                room?.CancelPendingSpawn();
                return;
            }
            var go = Services.Pool.Get(prefab.gameObject, scope, position, Quaternion.identity);
            var enemy = go != null ? go.GetComponent<Enemy>() : null;
            if (enemy != null) enemy.Init(room.CreateSpawnContext());
            room.CompletePendingSpawn(enemy);
        }

        List<List<(Enemy, Vector2)>> BuildWaves()
        {
            var waves = new List<List<(Enemy, Vector2)>>();
            var t = room.Template;
            if (!t.useSpawnBudget && t.fixedWaves.Count > 0)
            {
                foreach (var wd in t.fixedWaves)
                {
                    var wave = new List<(Enemy, Vector2)>();
                    foreach (var s in wd.spawns)
                        if (s.prefab != null) wave.Add((s.prefab, PointAt(s.spawnPointIndex)));
                    if (wave.Count > 0) waves.Add(wave);
                }
                return waves;
            }

            if (config == null || config.enemyTable.Count == 0) return waves;
            int count = rng.Next(config.wavesPerRoom.x, Mathf.Max(config.wavesPerRoom.x, config.wavesPerRoom.y) + 1);
            for (int w = 0; w < count; w++)
            {
                int budget = config.spawnBudget + config.budgetGrowth * w;
                var wave = new List<(Enemy, Vector2)>();
                int guard = 64;
                while (budget > 0 && guard-- > 0)
                {
                    var pick = PickEntry(budget);
                    if (pick == null) break;
                    budget -= pick.Cost;
                    wave.Add((pick.prefab, PointAt(-1)));
                }
                if (wave.Count > 0) waves.Add(wave);
            }
            return waves;
        }

        EnemySpawnEntry PickEntry(int budget)
        {
            int total = 0;
            foreach (var e in config.enemyTable) if (e.prefab != null && e.Cost <= budget) total += e.weight;
            if (total <= 0) return null;
            int r = rng.Next(total);
            foreach (var e in config.enemyTable)
            {
                if (e.prefab == null || e.Cost > budget) continue;
                if ((r -= e.weight) < 0) return e;
            }
            return null;
        }

        Vector2 PointAt(int index)
        {
            var pts = room.Template.spawnPoints;
            if (pts.Count > 0)
            {
                if (index < 0 || index >= pts.Count) index = rng.Next(pts.Count);
                if (pts[index] != null) return pts[index].position + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.3f);
            }
            // Không có SpawnPoint: điểm ngẫu nhiên đi được trong phòng.
            var b = room.Bounds;
            for (int i = 0; i < 20; i++)
            {
                var p = new Vector2(Mathf.Lerp(b.min.x + 2, b.max.x - 2, (float)rng.NextDouble()), Mathf.Lerp(b.min.y + 2, b.max.y - 2, (float)rng.NextDouble()));
                if (room.IsWalkable(p)) return p;
            }
            return b.center;
        }
    }
}
