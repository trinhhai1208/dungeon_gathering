using DG.Tweening;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Vũ khí cầm tay (một view cho mỗi WeaponData). WeaponHolder gọi Bind rồi TryAttack theo hướng ngắm.
    /// Sát thương = data.damage × AttackDamage; tốc đánh = fireRate × AttackSpeed; chí mạng = critBonus + CritChance.
    /// </summary>
    public abstract class WeaponBase : MonoBehaviour
    {
        public const float CritMultiplier = 2f;

        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected Transform muzzlePoint;

        protected WeaponData data;
        protected IPlayerContext owner;
        protected float cooldown;

        public WeaponData Data => data;
        public virtual bool IsBusy => false;
        public SpriteRenderer Sprite => spriteRenderer;
        public Transform MuzzlePoint => muzzlePoint;

        public virtual void Bind(WeaponData weapon, IPlayerContext ctx)
        {
            data = weapon;
            owner = ctx;
            cooldown = 0f;
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null && weapon.weaponSprite != null) spriteRenderer.sprite = weapon.weaponSprite;
            transform.localPosition = weapon.weaponPrefab != null ? (Vector3)weapon.holdOffset : Vector3.zero;
            if (muzzlePoint == null)
            {
                muzzlePoint = new GameObject("Muzzle").transform;
                muzzlePoint.SetParent(transform, false);
            }
            muzzlePoint.localPosition = weapon.muzzleOffset;
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

        protected virtual void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
        }

        protected bool CanAttack() => data != null && owner != null && !owner.IsDead && cooldown <= 0f && !IsBusy;

        protected void ResetAttackCooldown()
        {
            float rate = Mathf.Max(0.05f, data.fireRate * Mathf.Max(0.1f, owner.GetStat(StatType.AttackSpeed)));
            cooldown = 1f / rate;
        }

        /// <summary>Kiểm tra + trừ mana. Gọi một lần mỗi đòn.</summary>
        protected bool PayMana() => data.manaCost <= 0 || owner.TrySpendMana(data.manaCost);

        /// <summary>Sát thương của một đòn (đã nhân chỉ số + cường hóa + chí mạng).</summary>
        protected float RollDamage(out bool crit, float empowerMultiplier = 1f)
        {
            float dmg = data.damage * Mathf.Max(0f, owner.GetStat(StatType.AttackDamage)) * empowerMultiplier;
            float chance = data.critBonus + owner.GetStat(StatType.CritChance);
            return CombatUtil.RollDamage(dmg, chance, CritMultiplier, out crit);
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
