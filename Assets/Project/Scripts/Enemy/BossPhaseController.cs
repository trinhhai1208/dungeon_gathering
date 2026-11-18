using System;
using System.Collections.Generic;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// Boss = Enemy + phase theo ngưỡng máu (ARCHITECTURE §6). Một đòn vượt nhiều ngưỡng → nhảy thẳng sang phase
    /// ứng với máu hiện tại; mỗi phase vào tối đa một lần; không đổi phase khi IsDead.
    /// Bắn BossHealthChangedEvent cho HUD, BossDefeatedEvent khi chết.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class BossPhaseController : MonoBehaviour
    {
        [Serializable]
        public class Phase
        {
            [Range(0, 1)] [Tooltip("Vào phase khi máu ≤ ngưỡng này (tỉ lệ)")] public float hpThreshold = 0.6f;
            [Tooltip("Hệ số nhân tốc chạy của boss trong phase này (1 = giữ nguyên).")]
            public float speedMultiplier = 1.2f;
            [Tooltip("Skill / đòn của phase (chọn trong SkillLibrary)")]
            public List<Catalog.SkillId> skills = new();

            public List<EnemyAttackSO> ResolveAttacks() => Catalog.SkillRegistry.GetAll<EnemyAttackSO>(skills);
            [Tooltip("VFX phát tại vị trí boss khi vào phase. Trống = không có.")]
            public GameObject transitionVfx;
            [Tooltip("Bất tử trong lúc chuyển phase")] public float transitionInvulnerable = 0.8f;
        }

        [SerializeField] string bossId = "vampire-lord";
        [SerializeField] List<Phase> phases = new();

        Enemy enemy;
        int phaseIndex = -1;
        int transitionId; // phase mới vào khi phase trước còn đang chờ → chỉ lần chuyển MỚI NHẤT được tắt bất tử

        public string BossId => bossId;
        public int PhaseIndex => phaseIndex;
        IReadOnlyList<Phase> ActivePhases =>
            enemy != null && enemy.Data != null && enemy.Data.phases != null && enemy.Data.phases.Count > 0
                ? enemy.Data.phases
                : phases;

        void Awake() => enemy = GetComponent<Enemy>();

        // Lấy trực tiếp: OnEnable có thể chạy trước Enemy.Awake.
        void OnEnable()
        {
            GetComponent<EnemyHealth>().Changed += OnHealthChanged;
            phaseIndex = -1;
            transitionId++; // vô hiệu chờ bất tử của đời trước (pool)
        }

        void OnDisable()
        {
            GetComponent<EnemyHealth>().Changed -= OnHealthChanged;
        }

        void Start() => Publish();

        /// <summary>LevelDirector gọi sau Init để HUD hiện thanh máu ngay.</summary>
        public void Publish()
        {
            var h = enemy.Health;
            EventBus<BossHealthChangedEvent>.Raise(new BossHealthChangedEvent
            {
                BossName = enemy.Data != null ? enemy.Data.nameKey : bossId,
                Current = Mathf.CeilToInt(h.Current),
                Max = Mathf.CeilToInt(h.Max),
            });
        }

        void OnHealthChanged(float cur, float max)
        {
            Publish();
            if (enemy.Health.IsDead)
            {
                EventBus<BossDefeatedEvent>.Raise(new BossDefeatedEvent { BossId = bossId });
                return;
            }
            float frac = max > 0f ? cur / max : 0f;
            var list = ActivePhases;
            int target = phaseIndex;
            for (int i = phaseIndex + 1; i < list.Count; i++)
                if (frac <= list[i].hpThreshold) target = i;
            if (target != phaseIndex) EnterPhase(target);
        }

        async void EnterPhase(int index)
        {
            phaseIndex = index;
            var p = ActivePhases[index];
            enemy.Motor.SpeedMultiplier = p.speedMultiplier;
            var phaseAttacks = p.ResolveAttacks();
            if (phaseAttacks.Count > 0) enemy.Attacks.SetAttacks(phaseAttacks);
            CombatFx.Play(p.transitionVfx, transform.position, Quaternion.identity);
            if (p.transitionInvulnerable <= 0f) return;
            int id = ++transitionId;
            enemy.Health.Invulnerable = true;
            try { await Awaitable.WaitForSecondsAsync(p.transitionInvulnerable, enemy.LifeToken); }
            catch (OperationCanceledException) { }
            if (this != null && id == transitionId && !enemy.IsDead) enemy.Health.Invulnerable = false;
        }
    }
}
