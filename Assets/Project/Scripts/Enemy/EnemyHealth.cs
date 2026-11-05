using System;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// IDamageable của quái. Quy tắc R12: kiểm IsDead đầu tiên; HP về 0 → đặt isDead TRƯỚC rồi HandleDeath đúng một lần.
    /// IsDead reset trong ResetHealth (OnSpawn/Init).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] Color hurtFlash = new(1f, 0.45f, 0.45f);

        Enemy enemy;
        float hp, maxHp;
        bool isDead;

        public bool IsDead => isDead;
        public Team Team => Team.Enemy;
        public float Current => hp;
        public float Max => maxHp;
        /// <summary>Bất tử tạm (đang spawn, chuyển phase boss).</summary>
        public bool Invulnerable { get; set; }

        /// <summary>(hiện tại, tối đa)</summary>
        public event Action<float, float> Changed;

        void Awake() => enemy = GetComponent<Enemy>();

        public void ResetHealth(float max)
        {
            maxHp = Mathf.Max(1f, max);
            hp = maxHp;
            isDead = false;
            Invulnerable = false;
            Changed?.Invoke(hp, maxHp);
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (isDead || Invulnerable) return false;
            if (info.Team == Team.Enemy) return false; // cùng phe
            float amount = Mathf.Max(0f, info.Amount);
            if (amount <= 0f) return false;

            hp -= amount;
            CombatFx.DamageNumber(transform.position, amount, info.IsCrit);
            enemy.Anim?.Flash(hurtFlash);
            Sfx.PlayAt(info.IsCrit ? SfxId.CritHit : SfxId.EnemyHurt, transform.position);

            if (info.Knockback > 0f && info.Direction != Vector2.zero && enemy.Data != null)
                enemy.Motor.Knockback(info.Direction.normalized * info.Knockback * (1f - enemy.Data.knockbackResist));

            if (hp <= 0f)
            {
                hp = 0f;
                isDead = true;           // đặt TRƯỚC mọi thứ khác
                Changed?.Invoke(hp, maxHp);
                Sfx.PlayAt(SfxId.EnemyDeath, transform.position);
                enemy.HandleDeath();     // một lần duy nhất
                return true;
            }
            Changed?.Invoke(hp, maxHp);
            if (!enemy.Attacks.IsAttacking) enemy.Anim?.Play(AnimKey.Hurt);
            return true;
        }

        /// <summary>Giết ngay (boss chết → dọn quái con; vẫn qua luồng chết chuẩn).</summary>
        public void Kill()
        {
            if (isDead) return;
            Invulnerable = false;
            TakeDamage(new DamageInfo(hp + 1f, 0f, Vector2.zero, 0f, false, null, Team.Neutral));
        }
    }
}
