using System;
using System.Collections.Generic;
using System.Threading;
using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Chọn và chạy EnemyAttackSO (cooldown riêng từng đòn, chọn theo trọng số).</summary>
    [DisallowMultipleComponent]
    public sealed class EnemyAttackRunner : MonoBehaviour
    {
        readonly List<EnemyAttackSO> attacks = new();
        readonly Dictionary<EnemyAttackSO, float> readyAt = new();
        readonly List<EnemyAttackSO> candidates = new();
        Enemy enemy;
        int wallMask;
        // Mỗi lượt đánh có token riêng (liên kết LifeToken) để hủy được giữa chừng (đổi phase Boss, bị choáng).
        CancellationTokenSource attackCts;
        int runId;

        public bool IsAttacking { get; private set; }
        public EnemyAttackSO Current { get; private set; }

        void Awake()
        {
            enemy = GetComponent<Enemy>();
            wallMask = CombatLayers.Walls;
        }

        /// <summary>Đổi bộ đòn (phase Boss). Đòn mới sẵn sàng sau một nửa cooldown.</summary>
        public void SetAttacks(IReadOnlyList<EnemyAttackSO> list)
        {
            attacks.Clear();
            if (list != null) foreach (var a in list) if (a != null) attacks.Add(a);
            readyAt.Clear();
            foreach (var a in attacks) readyAt[a] = Time.time + a.cooldown * 0.5f * UnityEngine.Random.value;
            // Hủy đòn đang dở THẬT SỰ (không chỉ hạ cờ IsAttacking) → không có hai đòn chạy song song.
            Cancel();
        }

        /// <summary>Hủy đòn đang chạy (windup/charge dừng qua OperationCanceledException). Gọi được nhiều lần.</summary>
        public void Cancel()
        {
            runId++; // finally của lượt cũ không còn được đụng vào IsAttacking/Current
            if (attackCts != null && !attackCts.IsCancellationRequested) attackCts.Cancel();
            IsAttacking = false;
            Current = null;
        }

        public EnemyAttackSO Pick(ITargetable target)
        {
            if (IsAttacking || target == null || !target.IsAlive) return null;
            Vector2 from = enemy.AttackOrigin.position;
            Vector2 to = target.Transform.position;
            float dist = Vector2.Distance(from, to);
            candidates.Clear();
            float total = 0f;
            bool? los = null;
            foreach (var a in attacks)
            {
                if (Time.time < readyAt[a] || !a.CanUse(enemy, target, dist)) continue;
                if (a.requireLineOfSight)
                {
                    los ??= Physics2D.Linecast(from, to, wallMask).collider == null;
                    if (!los.Value) continue;
                }
                candidates.Add(a);
                total += a.weight;
            }
            if (candidates.Count == 0) return null;
            float r = UnityEngine.Random.value * total;
            foreach (var c in candidates) { if ((r -= c.weight) <= 0f) return c; }
            return candidates[^1];
        }

        public async void Run(EnemyAttackSO attack, ITargetable target)
        {
            if (attack == null || IsAttacking) return;
            int id = ++runId;
            IsAttacking = true;
            Current = attack;
            readyAt[attack] = Time.time + attack.cooldown;
            if (attack.stopWhileAttacking) enemy.Motor.Stop();
            var cts = CancellationTokenSource.CreateLinkedTokenSource(enemy.LifeToken);
            attackCts = cts;
            try
            {
                await attack.Execute(new EnemyAttackContext(enemy, target, cts.Token));
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e, this); }
            finally
            {
                if (attackCts == cts) attackCts = null;
                cts.Dispose();
                if (this != null && id == runId)
                {
                    IsAttacking = false;
                    Current = null;
                }
            }
        }

        void OnDestroy()
        {
            attackCts?.Cancel();
            attackCts = null;
        }
    }
}
