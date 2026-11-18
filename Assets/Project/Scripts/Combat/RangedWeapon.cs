using System;
using System.Threading;
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
        CancellationTokenSource burstCts;

        public float RecoilDistance => recoilDistance;
        public float RecoilDuration => recoilDuration;
        public Animator FireAnimator => fireAnimator;
        public GameObject DefaultProjectilePrefab => defaultProjectilePrefab;
        public GameObject DefaultMuzzleVfx => defaultMuzzleVfx;

        /// <summary>Cooldown không ngắn hơn thời gian bắn hết một loạt (trước đây tốc đánh cao hủy viên cuối của loạt).</summary>
        protected override void ResetAttackCooldown()
        {
            base.ResetAttackCooldown();
            int bursts = spec != null ? Mathf.Max(1, spec.burstCount) : 1;
            if (bursts > 1) readyAt = Mathf.Max(readyAt, Time.time + (bursts - 1) * Mathf.Max(0.02f, spec.burstInterval) + 0.03f);
        }

        public override void Bind(WeaponData weapon, IPlayerContext ctx)
        {
            base.Bind(weapon, ctx);
            if (spec != null && spec.animator != null)
            {
                if (fireAnimator == null) fireAnimator = GetComponentInChildren<Animator>();
                if (fireAnimator == null && spriteRenderer != null) fireAnimator = spriteRenderer.gameObject.AddComponent<Animator>();
                if (fireAnimator != null) fireAnimator.runtimeAnimatorController = spec.animator;
            }
        }

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

            FireVolley(direction, empowered ? mult : 1f, extraPierce);

            int bursts = spec != null ? Mathf.Max(1, spec.burstCount) : 1;
            if (bursts > 1)
            {
                CancelBurst();
                burstCts = CancellationTokenSource.CreateLinkedTokenSource(Services.AppToken, destroyCancellationToken);
                FireBurstAsync(data, direction, empowered ? mult : 1f, extraPierce, bursts, spec.burstInterval, burstCts.Token);
            }
            return true;
        }

        void FireVolley(Vector2 direction, float empowerMult, int extraPierce)
        {
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector3 spawnPos = MuzzlePosition;
            int count = spec != null ? Mathf.Max(1, spec.projectileCount) : 1;
            float spread = spec != null ? spec.spread : 0f;

            var projPrefab = (spec != null && spec.projectilePrefab != null) ? spec.projectilePrefab : defaultProjectilePrefab;
            bool hitscan = (spec != null && spec.fireMode == RangedFireMode.Hitscan) || projPrefab == null;

            for (int i = 0; i < count; i++)
            {
                float angle = baseAngle;
                if (count > 1) angle += Mathf.Lerp(-spread, spread, i / (float)(count - 1));
                else if (spread > 0f) angle += UnityEngine.Random.Range(-spread, spread);
                Vector2 dir = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                float damage = RollDamage(out bool crit, empowerMult);

                if (hitscan) FireHitscan(spawnPos, dir, damage, crit);
                else FireProjectile(spawnPos, dir, damage, crit, extraPierce, projPrefab);
            }

            var muzzleVfx = (spec != null && spec.muzzleVfxPrefab != null) ? spec.muzzleVfxPrefab : defaultMuzzleVfx;
            if (muzzleVfx != null) CombatFx.Play(muzzleVfx, spawnPos, Quaternion.Euler(0f, 0f, baseAngle), null, 0.3f);
            Sfx.PlayOverride(spec != null ? spec.attackSfx : null, DefaultShotSfx());
            if (fireAnimator != null) fireAnimator.SetTrigger(FireHash);
            ApplyRecoil();
        }

        async void FireBurstAsync(WeaponData burstWeapon, Vector2 fallbackDir, float empowerMult, int extraPierce, int bursts, float interval, CancellationToken token)
        {
            for (int b = 1; b < bursts; b++)
            {
                try { await Awaitable.WaitForSecondsAsync(Mathf.Max(0.02f, interval), token); }
                catch (OperationCanceledException) { return; }

                if (this == null || !gameObject.activeInHierarchy || data != burstWeapon || owner == null || owner.IsDead) return;
                Vector2 dir = owner.AimDirection.sqrMagnitude > 0.0001f ? owner.AimDirection.normalized : fallbackDir;
                FireVolley(dir, empowerMult, extraPierce);
            }
        }

        void CancelBurst()
        {
            if (burstCts == null) return;
            burstCts.Cancel();
            burstCts.Dispose();
            burstCts = null;
        }

        /// <summary>Âm bắn mặc định khi WeaponData không gán attackSfx: tốn mana → phép, hitscan → súng, còn lại → cung/nỏ.</summary>
        SfxId DefaultShotSfx() => data == null ? SfxId.BowShot
            : spec.manaCost > 0 ? SfxId.MagicShot
            : spec.fireMode == RangedFireMode.Hitscan ? SfxId.GunShot : SfxId.BowShot;

        void FireProjectile(Vector3 pos, Vector2 dir, float damage, bool crit, int extraPierce, GameObject prefab = null)
        {
            var targetPrefab = prefab != null ? prefab : (spec != null ? spec.projectilePrefab : defaultProjectilePrefab);
            if (targetPrefab == null || Services.Pool == null) return;
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
                Speed = spec != null ? spec.projectileSpeed : 10f,
                Knockback = spec != null ? spec.knockback : 2f,
                Crit = crit,
                Pierce = (spec != null ? spec.pierce : 0) + extraPierce,
                ExplosionRadius = spec != null ? spec.explosionRadius : 0f,
                // attackRange nhỏ (mặc định cận chiến) → dùng lifetime của prefab đạn.
                Lifetime = (spec != null && spec.projectileSpeed > 0f && spec.attackRange > 2f) ? spec.attackRange / spec.projectileSpeed : 0f,
                Team = Team.Player,
                ImpactVfx = spec != null ? spec.hitImpactVfxPrefab : null,
                Source = owner != null ? owner.GameObject : gameObject,
                Owner = owner,
                Weapon = data,
                Status = OnHitPayload,
            }, dir);
        }

        void FireHitscan(Vector3 pos, Vector2 dir, float damage, bool crit)
        {
            int mask = CombatLayers.EnemyTargets | CombatLayers.Walls;
            var hit = Physics2D.Raycast(pos, dir, spec.attackRange, mask);
            if (hit.collider == null) return;
            CombatFx.Hit(spec.hitImpactVfxPrefab, hit.point);
            Sfx.PlayAt(SfxId.ProjectileHit, hit.point);
            if (spec.explosionRadius > 0f)
            {
                // Đạn nổ: nổ tại điểm chạm kể cả khi trúng tường (giống Projectile.Impact).
                var hits = CombatUtil.RentList();
                try
                {
                    CombatUtil.DamageArea(hit.point, spec.explosionRadius, CombatLayers.EnemyTargets,
                        new DamageInfo(damage, spec.knockback, dir, 0f, crit, owner.GameObject, Team.Player), hits);
                    for (int i = 0; i < hits.Count; i++) { ApplyOnHitStatus(hits[i]); ReportHit(hits[i], hit.point, dir, damage, crit); }
                }
                finally { CombatUtil.ReturnList(hits); }
                return;
            }
            var d = CombatUtil.Resolve(hit.collider);
            if (d == null || d.IsDead || d.Team == Team.Player) return;
            if (d.TakeDamage(new DamageInfo(damage, spec.knockback, dir, 0f, crit, owner.GameObject, Team.Player)))
            {
                ApplyOnHitStatus(d);
                ReportHit(d, hit.point, dir, damage, crit);
            }
        }

        void ApplyRecoil()
        {
            recoil?.Kill(true);
            var basePos = restLocalPos;   // lưu khi Bind — không lệch gấp đôi với view tự dựng
            transform.localPosition = basePos;
            float dist = spec != null ? spec.recoilDistance : recoilDistance;
            float dur = spec != null && spec.recoilDuration > 0f ? spec.recoilDuration : recoilDuration;
            recoil = transform.DOLocalMove(basePos - Vector3.right * dist, dur * 0.5f)
                .SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
        }

        public override void CancelAttack()
        {
            CancelBurst();
            recoil?.Kill();
            recoil = null;
            if (spec != null) transform.localPosition = restLocalPos;
            base.CancelAttack();
        }
    }
}
