using System.Collections.Generic;
using DG.Tweening;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Kiếm: vung cung swingArc trong swingDuration (DOTween), hitbox OverlapCircle phía trước,
    /// mỗi mục tiêu trúng tối đa 1 lần/đòn, chém tan đạn quái (WEAPONS §4.1).
    /// </summary>
    public sealed class MeleeWeapon : WeaponBase
    {
        static readonly Collider2D[] buffer = new Collider2D[32];
        readonly HashSet<IDamageable> struck = new();

        [SerializeField] float swingArc = 110f;
        [SerializeField] float swingDuration = 0.15f;
        [SerializeField] GameObject defaultSlashVfx;

        bool isSwinging;
        Tween swing;
        float swingSign = 1f;

        public override bool IsBusy => isSwinging;
        public float SwingArc => swingArc;
        public float SwingDuration => swingDuration;
        public GameObject DefaultSlashVfx => defaultSlashVfx;

        /// <summary>
        /// Độ dài lưỡi kiếm thực tế (world units) tính từ tay cầm (gốc view) tới mũi kiếm,
        /// đo trên sprite đã căn theo trục +X của view. Không có sprite → dùng attackRange.
        /// </summary>
        public float GetBladeLength()
        {
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                var b = spriteRenderer.sprite.bounds; // tọa độ quanh pivot (chuôi kiếm)
                var srT = spriteRenderer.transform;
                float maxX = 0f;
                for (int i = 0; i < 4; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, 0f);
                    // sprite-local → view-local (bỏ qua scale âm của lật trái)
                    Vector3 p = srT == transform ? corner : transform.InverseTransformPoint(srT.TransformPoint(corner));
                    maxX = Mathf.Max(maxX, p.x);
                }
                return maxX * Mathf.Abs(transform.lossyScale.x);
            }
            return data != null ? data.attackRange : 1.2f;
        }

        /// <summary>Tầm chém tính từ tâm WeaponPivot: khoảng tay + lưỡi kiếm (không nhỏ hơn attackRange).</summary>
        public float GetBladeReach()
        {
            float baseRange = data != null ? data.attackRange : 1.2f;
            return Mathf.Max(baseRange, GetHandDistance() + GetBladeLength());
        }

        /// <summary>Khoảng cách từ WeaponPivot (tâm xoay) tới socket tay cầm.</summary>
        float GetHandDistance()
        {
            var socket = transform.parent;
            var pivot = socket != null ? socket.parent : null;
            return socket != null && pivot != null ? Vector2.Distance(socket.position, pivot.position) : 0f;
        }

        Vector2 GetPivotPosition()
        {
            var socket = transform.parent;
            var pivot = socket != null ? socket.parent : null;
            if (pivot != null) return pivot.position;
            return owner != null ? (Vector2)owner.Transform.position : (Vector2)transform.position;
        }

        new void ResetAttackCooldown()
        {
            float rate = Mathf.Max(0.05f, (data != null ? data.fireRate : 2f) * Mathf.Max(0.1f, owner != null ? owner.GetStat(StatType.AttackSpeed) : 1f));
            float duration = data != null && data.swingDuration > 0f ? data.swingDuration : swingDuration;
            // Cooldown phải ít nhất bằng thời gian vung + 0.12s để không bị lặp đòn kép trong 1 lần chạm (tap 200ms)
            cooldown = Mathf.Max(duration + 0.12f, 1f / rate);
        }

        public override bool TryAttack(Vector2 direction)
        {
            if (!CanAttack() || direction.sqrMagnitude < 0.0001f) return false;
            if (!PayMana()) return false;
            direction.Normalize();
            ResetAttackCooldown();
            isSwinging = true;
            owner.Hooks.RaiseAttacked(data);

            // Vung: xoay view quanh pivot từ +arc/2 → -arc/2 (đảo chiều mỗi đòn).
            float arc = data != null && data.swingArc > 0f ? data.swingArc : swingArc;
            float duration = data != null && data.swingDuration > 0f ? data.swingDuration : swingDuration;
            float half = arc * 0.5f * swingSign;
            swingSign = -swingSign;
            transform.localRotation = Quaternion.Euler(0f, 0f, half);
            swing = transform.DOLocalRotate(new Vector3(0f, 0f, -half), Mathf.Max(0.05f, duration))
                .SetEase(Ease.OutQuad)
                .OnComplete(() => { transform.localRotation = Quaternion.identity; isSwinging = false; });

            // Mọi thứ (VFX, hitbox) đo từ tay cầm thật: WeaponPivot + hướng × khoảng tay.
            float blade = GetBladeLength();
            float handDist = GetHandDistance();
            Vector2 pivotPos = GetPivotPosition();
            Vector2 hand = pivotPos + direction * handDist;
            float reach = Mathf.Max(data != null ? data.attackRange : 1.2f, handDist + blade);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            var slashVfx = data != null && data.slashVfxPrefab != null ? data.slashVfxPrefab : defaultSlashVfx;
            if (slashVfx != null)
            {
                // Vệt chém là cung do mũi kiếm vẽ ra quanh tay: tâm ở giữa lưỡi, đường kính ≈ 2 × lưỡi.
                Vector2 vfxCenter = hand + direction * (blade * 0.5f);
                var fx = CombatFx.Play(slashVfx, vfxCenter, Quaternion.Euler(0f, 0f, angle));
                if (fx != null)
                {
                    float s = Mathf.Clamp(blade * 2f / SlashVfxArcWidth, 0.3f, 1.2f);
                    // Lật vệt chém theo chiều vung (đòn sau vung ngược đòn trước)
                    fx.transform.localScale = new Vector3(s, half >= 0f ? s : -s, 1f);
                }
            }
            Sfx.PlayOverride(data != null ? data.attackSfx : null, SfxId.SwordSwing);

            DoHit(pivotPos, hand, direction, reach, arc);
            return true;
        }

        /// <summary>Bề rộng (world units, scale 1) của vệt chém trong sprite VFX sword-combo (~68px / 16 PPU).</summary>
        const float SlashVfxArcWidth = 4.2f;

        /// <summary>
        /// Hitbox hình quạt khớp với cung vung: tâm tại WeaponPivot, bán kính = tay + lưỡi,
        /// góc = arc + biên 20°. Mục tiêu sát người (≤ tay + 0.25) luôn trúng.
        /// </summary>
        void DoHit(Vector2 pivotPos, Vector2 hand, Vector2 direction, float reach, float arc)
        {
            float mult = 1f;
            bool empowered = owner != null && owner.TryConsumeEmpowered(out mult, out _);
            float damage = RollDamage(out bool crit, empowered ? mult : 1f);

            int mask = CombatLayers.EnemyTargets | (data != null && data.destroysProjectiles ? CombatLayers.EnemyBullets : 0);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = true };
            float halfArc = arc * 0.5f + 20f;
            float closeRange = Vector2.Distance(pivotPos, hand) + 0.25f;
            int n = Physics2D.OverlapCircle(pivotPos, reach, filter, buffer);
            struck.Clear();
            for (int i = 0; i < n; i++)
            {
                var col = buffer[i];
                // Dùng điểm gần nhất của collider để quái to (boss) vẫn trúng khi mép thân nằm trong cung
                Vector2 to = col.ClosestPoint(pivotPos) - pivotPos;
                float dist = to.magnitude;
                if (dist > closeRange && Vector2.Angle(direction, to) > halfArc) continue;
                if (data != null && data.destroysProjectiles && col.TryGetComponent<Projectile>(out var proj))
                {
                    if (proj.Team == Team.Enemy) proj.Despawn(); // idempotent
                    continue;
                }
                var d = CombatUtil.Resolve(col);
                if (d == null || d.IsDead || d.Team == Team.Player) continue;
                struck.Add(d);
            }
            foreach (var d in struck)
            {
                Vector2 dir = ((Vector2)d.transform.position - (Vector2)owner.Transform.position).normalized;
                if (dir == Vector2.zero) dir = direction;
                if (d.TakeDamage(new DamageInfo(damage, data.knockback, dir, 0f, crit, owner.GameObject, Team.Player)))
                {
                    CombatFx.Hit(data.hitImpactVfxPrefab, d.transform.position);
                    Sfx.PlayAt(SfxId.SwordHit, d.transform.position);
                    ReportHit(d, d.transform.position, dir, damage, crit);
                }
            }
            struck.Clear();
        }

        public override void CancelAttack()
        {
            swing?.Kill();
            swing = null;
            isSwinging = false;
            transform.localRotation = Quaternion.identity;
            base.CancelAttack();
        }
    }
}
