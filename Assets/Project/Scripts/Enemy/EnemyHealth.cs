using System;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// IDamageable của quái. Tài nguyên + chỉ số nằm ở <see cref="ActorVitals"/> (dùng chung với Player).
    /// Quy tắc R12: kiểm IsDead đầu tiên; HP về 0 → IsDead đặt TRƯỚC rồi HandleDeath đúng một lần.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable, IStatSheetProvider
    {
        [SerializeField] Color hurtFlash = new(1f, 0.45f, 0.45f);

        static readonly object TierSource = new();

        readonly ActorVitals vitals = new(new BaseStats { maxHp = 1f, attackDamage = 1f, attackSpeed = 1f, critDamage = 2f });
        Enemy enemy;

        public ActorVitals Vitals => vitals;
        public StatSheet Stats => vitals.Sheet;
        public bool IsDead => vitals.IsDead;
        public Team Team => Team.Enemy;
        public float Current => vitals.Hp;
        public float Max => vitals.MaxHp;
        /// <summary>Bất tử tạm (đang spawn, chuyển phase boss).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>(hiện tại, tối đa)</summary>
        public event Action<float, float> Changed;

        void Awake() => enemy = GetComponent<Enemy>();

        /// <summary>
        /// Dựng lại chỉ số cho lượt sống mới: chỉ số gốc + hệ số bậc (MonsterTierConfig) + hệ số độ khó của level.
        /// </summary>
        public void ResetStats(BaseStats baseStats, MonsterTier tier, float levelHealthMultiplier)
        {
            // Hệ số HP của level nhân thẳng vào HP gốc để NHÂN với hệ số bậc (Elite ×2.5 trên level ×2 = ×5),
            // thay vì cộng hai modifier % (= ×3.5).
            baseStats.maxHp *= Mathf.Max(0.1f, levelHealthMultiplier);
            vitals.Reset(baseStats);
            vitals.BeginBatch();
            var cfg = MonsterTierConfigSO.Current;
            if (cfg != null) vitals.Sheet.AddRange(TierSource, MonsterTierConfigSO.Modifiers(cfg.Get(tier)));
            vitals.EndBatch();
            vitals.FillAll();
            Invulnerable = false;
            Changed?.Invoke(Current, Max);
        }

        /// <summary>Tương thích cũ: chỉ đặt Max HP.</summary>
        public void ResetHealth(float max) =>
            ResetStats(new BaseStats { maxHp = Mathf.Max(1f, max), attackDamage = 1f, attackSpeed = 1f, critDamage = 2f }, MonsterTier.Normal, 1f);

        public bool TakeDamage(in DamageInfo info)
        {
            if (IsDead || Invulnerable) return false;
            if (info.Team == Team.Enemy) return false; // cùng phe
            float amount = Mathf.Max(0f, info.Amount);
            if (amount <= 0f) return false;

            var hit = vitals.ApplyDamage(amount, info.IgnoresArmor);
            if (!hit.Applied) return false;
            CombatFx.DamageNumber(transform.position, amount, info.IsCrit);
            enemy.Anim?.Flash(hurtFlash);
            Sfx.PlayAt(info.IsCrit ? SfxId.CritHit : SfxId.EnemyHurt, transform.position);

            if (info.Knockback > 0f && info.Direction != Vector2.zero)
                enemy.Motor.Knockback(info.Direction.normalized * info.Knockback * (1f - Stats.Get(StatType.KnockbackResist)));

            Changed?.Invoke(Current, Max);
            if (hit.Fatal)
            {
                Sfx.PlayAt(SfxId.EnemyDeath, transform.position);
                enemy.HandleDeath();     // một lần duy nhất (IsDead đã đặt trong ApplyDamage)
                return true;
            }
            if (!info.IsDoT && !enemy.Attacks.IsAttacking) enemy.Anim?.Play(AnimKey.Hurt);
            return true;
        }

        /// <summary>Giết ngay (boss chết → dọn quái con; vẫn qua luồng chết chuẩn).</summary>
        public void Kill()
        {
            if (IsDead) return;
            Invulnerable = false;
            TakeDamage(new DamageInfo(Current + 1f, 0f, Vector2.zero, 0f, false, null, Team.Neutral, DamageFlags.IgnoreArmor));
        }
    }
}
