using DG.Tweening;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Vũ khí cầm tay (một view cho mỗi WeaponData). WeaponHolder gọi Bind rồi TryAttack theo hướng ngắm.
    /// Sát thương = spec.damage × AttackDamage; tốc đánh = fireRate × AttackSpeed; chí mạng = critBonus + CritChance.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        public const float CritMultiplier = 2f;

        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Transform muzzlePoint;

        protected WeaponData data;      // định danh vật phẩm (hook passive, HitReport)
        protected WeaponSpec spec;      // thông số đã resolve (thư viện hoặc dữ liệu cũ)
        protected IPlayerContext owner;
        protected float readyAt;
        /// <summary>Vị trí nghỉ của view (lưu một lần khi Bind) — giật nòng / đâm luôn quay về đây.</summary>
        protected Vector3 restLocalPos;

        public WeaponData Data => data;
        public WeaponSpec Spec => spec;
        public virtual bool IsBusy => false;
        public SpriteRenderer Sprite => spriteRenderer;
        public Transform MuzzlePoint => muzzlePoint;

        public virtual void Bind(WeaponData weapon, IPlayerContext ctx)
        {
            data = weapon;
            spec = weapon.Spec;
            owner = ctx;
            readyAt = 0f;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && spec.weaponSprite != null) spriteRenderer.sprite = spec.weaponSprite;
            transform.localPosition = spec.weaponPrefab != null ? (Vector3)spec.holdOffset : Vector3.zero;
            restLocalPos = transform.localPosition;
            if (muzzlePoint == null)
            {
                muzzlePoint = new GameObject("Muzzle").transform;
                muzzlePoint.SetParent(transform, false);
            }
            muzzlePoint.localPosition = spec.muzzleOffset;
        }

        /// <summary>
        /// Lật vũ khí khi ngắm sang trái: lật trục Y của chính view (khung đã căn theo hướng ngắm),
        /// không dùng SpriteRenderer.flipY vì sprite gốc vẽ chéo → flipY làm lưỡi lệch 90°.
        /// </summary>
        public void SetFacingLeft(bool left)
        {
            var s = transform.localScale;
            float y = Mathf.Abs(s.y) * (left ? -1f : 1f);
            if (!Mathf.Approximately(s.y, y)) transform.localScale = new Vector3(s.x, y, s.z);
            if (spriteRenderer != null && spriteRenderer.flipY) spriteRenderer.flipY = false;
        }

        protected bool CanAttack() => spec != null && owner != null && !owner.IsDead && Time.time >= readyAt && !IsBusy;

        protected virtual void ResetAttackCooldown()
        {
            float rate = Mathf.Max(0.05f, spec.fireRate * Mathf.Max(0.1f, owner.GetStat(StatType.AttackSpeed)));
            readyAt = Time.time + 1f / rate;
        }

        /// <summary>Kiểm tra + trừ mana. Gọi một lần mỗi đòn.</summary>
        protected bool PayMana() => spec.manaCost <= 0 || owner.TrySpendMana(spec.manaCost);

        /// <summary>Sát thương của một đòn (đã nhân chỉ số + cường hóa + chí mạng).</summary>
        protected float RollDamage(out bool crit, float empowerMultiplier = 1f)
        {
            float dmg = spec.damage * Mathf.Max(0f, owner.GetStat(StatType.AttackDamage)) * empowerMultiplier;
            float chance = spec.critBonus + owner.GetStat(StatType.CritChance);
            float critMult = owner != null ? owner.GetStat(StatType.CritDamage) : CritMultiplier;
            if (critMult <= 0f) critMult = CritMultiplier;
            return CombatUtil.RollDamage(dmg, chance, critMult, out crit);
        }

        protected void ReportHit(IDamageable target, Vector2 point, Vector2 dir, float damage, bool crit)
        {
            owner.Hooks.RaiseHit(new HitReport
            {
                Target = (target as Component)?.gameObject,
                Point = point,
                Direction = dir,
                Damage = damage,
                Crit = crit,
                Killed = target.IsDead,
                Weapon = data,
            });
        }

        /// <summary>Hiệu ứng khi trúng của vũ khí (SO gốc + entry + đạn), dùng cho đạn qua ProjectileSpec.Status.</summary>
        protected StatusPayload OnHitPayload => new() { effects = spec != null ? spec.onHitStatus : null };

        protected void ApplyOnHitStatus(IDamageable target)
        {
            if (spec == null || spec.onHitStatus.Count == 0 || target == null || target.IsDead) return;
            OnHitPayload.ApplyTo(target, owner != null ? owner.GameObject : gameObject);
        }

        protected Vector3 MuzzlePosition => muzzlePoint != null ? muzzlePoint.position : transform.position;

        /// <summary>Tấn công theo hướng. Trả true nếu đã tung đòn.</summary>
        public abstract bool TryAttack(Vector2 direction);

        /// <summary>Hủy đòn đang dở (đổi vũ khí, chết, tắt holder).</summary>
        public virtual void CancelAttack()
        {
            DOTween.Kill(transform);
        }

        protected virtual void OnDisable() => CancelAttack();
    }
}
