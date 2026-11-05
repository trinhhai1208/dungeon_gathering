using DG.Tweening;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Súng / trượng: bắn đơn, bắn chùm (projectileCount + spread), tia tức thời (Hitscan), đạn nổ (explosionRadius).
    /// Giật nòng bằng DOTween; đổi vũ khí giữa chừng hủy tween (không bắn đạn của vũ khí cũ).
    /// </summary>
    public sealed class RangedWeapon : WeaponBase
    {
        [SerializeField] float recoilDistance = 0.12f;
        [SerializeField] float recoilDuration = 0.08f;
        [SerializeField] Animator fireAnimator;
        [SerializeField] GameObject defaultProjectilePrefab;
        [SerializeField] GameObject defaultMuzzleVfx;
        static readonly int FireHash = Animator.StringToHash("Fire");

        Tween recoil;

        public float RecoilDistance => recoilDistance;
        public float RecoilDuration => recoilDuration;
        public Animator FireAnimator => fireAnimator;
        public GameObject DefaultProjectilePrefab => defaultProjectilePrefab;
        public GameObject DefaultMuzzleVfx => defaultMuzzleVfx;

        public override bool TryAttack(Vector2 direction)
        {
            if (!CanAttack() || direction.sqrMagnitude < 0.0001f) return false;
            if (!PayMana()) return false;
            direction.Normalize();
            ResetAttackCooldown();
            owner.Hooks.RaiseAttacked(data);

            float mult = 1f;
            int extraPierce = 0;
            bool empowered = owner != null && owner.TryConsumeEmpowered(out mult, out extraPierce);
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector3 spawnPos = MuzzlePosition;
            int count = data != null ? Mathf.Max(1, data.projectileCount) : 1;
            float spread = data != null ? data.spread : 0f;

            var projPrefab = (data != null && data.projectilePrefab != null) ? data.projectilePrefab : defaultProjectilePrefab;
            bool hitscan = (data != null && data.fireMode == RangedFireMode.Hitscan) || projPrefab == null;

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle;
                if (count > 1) angle += Mathf.Lerp(-spread, spread, count == 1 ? 0.5f : i / (float)(count - 1));
                else if (spread > 0f) angle += Random.Range(-spread, spread);
                Vector2 dir = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                float damage = RollDamage(out bool crit, empowered ? mult : 1f);

                if (hitscan) FireHitscan(spawnPos, dir, damage, crit);
                else FireProjectile(spawnPos, dir, damage, crit, extraPierce, projPrefab);
            }

            var muzzleVfx = (data != null && data.muzzleVfxPrefab != null) ? data.muzzleVfxPrefab : defaultMuzzleVfx;
            if (muzzleVfx != null) CombatFx.Play(muzzleVfx, spawnPos, Quaternion.Euler(0f, 0f, baseAngle), null, 0.3f);
            Sfx.PlayOverride(data != null ? data.attackSfx : null, DefaultShotSfx());
            if (fireAnimator != null) fireAnimator.SetTrigger(FireHash);
            ApplyRecoil();
            return true;
        }

        /// <summary>Âm bắn mặc định khi WeaponData không gán attackSfx: tốn mana → phép, hitscan → súng, còn lại → cung/nỏ.</summary>
        SfxId DefaultShotSfx() => data == null ? SfxId.BowShot
            : data.manaCost > 0 ? SfxId.MagicShot
            : data.fireMode == RangedFireMode.Hitscan ? SfxId.GunShot : SfxId.BowShot;

        void FireProjectile(Vector3 pos, Vector2 dir, float damage, bool crit, int extraPierce, GameObject prefab = null)
        {
            var targetPrefab = prefab != null ? prefab : (data != null ? data.projectilePrefab : defaultProjectilePrefab);
            if (targetPrefab == null) return;
            var go = Services.Pool.Get(targetPrefab, pos, Quaternion.identity);
            if (go == null) return;
            if (!go.TryGetComponent<Projectile>(out var p))
            {
                Debug.LogWarning($"[Combat] Prefab đạn '{targetPrefab.name}' thiếu Projectile.");
                Services.Pool.Return(go);
                return;
            }
            p.Launch(new ProjectileSpec
            {
                Damage = damage,
                Speed = data != null ? data.projectileSpeed : 10f,
                Knockback = data != null ? data.knockback : 2f,
                Crit = crit,
                Pierce = (data != null ? data.pierce : 0) + extraPierce,
                ExplosionRadius = data != null ? data.explosionRadius : 0f,
                // attackRange nhỏ (mặc định cận chiến) → dùng lifetime của prefab đạn.
                Lifetime = (data != null && data.projectileSpeed > 0f && data.attackRange > 2f) ? data.attackRange / data.projectileSpeed : 0f,
                Team = Team.Player,
                ImpactVfx = data != null ? data.hitImpactVfxPrefab : null,
                Source = owner != null ? owner.GameObject : gameObject,
                Owner = owner,
                Weapon = data,
            }, dir);
        }

        void FireHitscan(Vector3 pos, Vector2 dir, float damage, bool crit)
        {
            int mask = CombatLayers.EnemyTargets | CombatLayers.Walls;
            var hit = Physics2D.Raycast(pos, dir, data.attackRange, mask);
            if (hit.collider == null) return;
            CombatFx.Hit(data.hitImpactVfxPrefab, hit.point);
            Sfx.PlayAt(SfxId.ProjectileHit, hit.point);
            if (data.explosionRadius > 0f)
            {
                // Đạn nổ: nổ tại điểm chạm kể cả khi trúng tường (giống Projectile.Impact).
                var hits = CombatUtil.RentList();
                try
                {
                    CombatUtil.DamageArea(hit.point, data.explosionRadius, CombatLayers.EnemyTargets,
                        new DamageInfo(damage, data.knockback, dir, 0f, crit, owner.GameObject, Team.Player), hits);
                    for (int i = 0; i < hits.Count; i++) ReportHit(hits[i], hit.point, dir, damage, crit);
                }
                finally { CombatUtil.ReturnList(hits); }
                return;
            }
            var d = CombatUtil.Resolve(hit.collider);
            if (d == null || d.IsDead || d.Team == Team.Player) return;
            if (d.TakeDamage(new DamageInfo(damage, data.knockback, dir, 0f, crit, owner.GameObject, Team.Player)))
                ReportHit(d, hit.point, dir, damage, crit);
        }

        void ApplyRecoil()
        {
            recoil?.Kill(true);
            var basePos = (Vector3)data.holdOffset;
            transform.localPosition = basePos;
            recoil = transform.DOLocalMove(basePos - Vector3.right * recoilDistance, recoilDuration * 0.5f)
                .SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
        }

        public override void CancelAttack()
        {
            recoil?.Kill();
            recoil = null;
            if (data != null) transform.localPosition = data.holdOffset;
            base.CancelAttack();
        }
    }
}
