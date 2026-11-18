using System.Collections.Generic;
using DG.Tweening;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Kiếm / Thương: vung cung / đâm / xoay 360 trong swingDuration (DOTween), hitbox phía trước hoặc toàn vòng,
    /// mỗi mục tiêu trúng tối đa 1 lần/đòn, chém tan đạn quái, có thể phóng kiếm khí (projectilePrefab).
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
            return spec != null ? spec.attackRange : 1.2f;
        }

        /// <summary>Tầm chém tính từ tâm WeaponPivot: khoảng tay + lưỡi kiếm (không nhỏ hơn attackRange).</summary>
        public float GetBladeReach()
        {
            float baseRange = spec != null ? spec.attackRange : 1.2f;
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

        protected override void ResetAttackCooldown()
        {
            float rate = Mathf.Max(0.05f, (spec != null ? spec.fireRate : 2f) * Mathf.Max(0.1f, owner != null ? owner.GetStat(StatType.AttackSpeed) : 1f));
            float duration = spec != null && spec.swingDuration > 0f ? spec.swingDuration : swingDuration;
            // Cooldown phải ít nhất bằng thời gian vung + 0.12s để không bị lặp đòn kép trong 1 lần chạm (tap 200ms)
            readyAt = Time.time + Mathf.Max(duration + 0.12f, 1f / rate);
        }

        public override bool TryAttack(Vector2 direction)
        {
            if (!CanAttack() || direction.sqrMagnitude < 0.0001f) return false;
            if (!PayMana()) return false;
            direction.Normalize();
            ResetAttackCooldown();
            isSwinging = true;
            owner.Hooks.RaiseAttacked(data);

            var style = spec != null ? spec.meleeStyle : MeleeAttackStyle.Swing;
            float arc = spec != null && spec.swingArc > 0f ? spec.swingArc : swingArc;
            if (style == MeleeAttackStyle.Thrust) arc = Mathf.Min(arc, 55f);
            else if (style == MeleeAttackStyle.Spin360) arc = 360f;

            float duration = Mathf.Max(0.05f, spec != null && spec.swingDuration > 0f ? spec.swingDuration : swingDuration);
            float half = arc * 0.5f * swingSign;
            swingSign = -swingSign;

            // Mọi thứ (VFX, hitbox) đo từ tay cầm thật: WeaponPivot + hướng × khoảng tay.
            float blade = GetBladeLength();
            float handDist = GetHandDistance();
            Vector2 pivotPos = GetPivotPosition();
            Vector2 hand = pivotPos + direction * handDist;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Tính scale của vệt chém và tầm vươn xa nhất của mũi vệt chém (vfxTipReach)
            float vfxScale = Mathf.Clamp(((blade + handDist) * 2.2f) / SlashVfxArcWidth, 0.35f, 1.2f);
            float vfxTipReach = handDist + (blade * 0.55f) + (vfxScale * 2.56f);

            // Hitbox Soul Knight: ôm trọn toàn bộ tầm vươn của vệt chém + biên an toàn 0.1 đơn vị
            float reach = Mathf.Max(spec != null ? spec.attackRange : 1.2f, vfxTipReach + 0.1f);

            if (style == MeleeAttackStyle.Thrust)
            {
                Vector3 basePos = restLocalPos;
                transform.localRotation = Quaternion.identity;
                transform.localPosition = basePos;
                float thrustDist = Mathf.Clamp(reach * 0.3f, 0.25f, 0.6f);
                swing = transform.DOLocalMove(basePos + Vector3.right * thrustDist, duration * 0.5f)
                    .SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad)
                    .OnComplete(() => { transform.localPosition = basePos; isSwinging = false; });
            }
            else if (style == MeleeAttackStyle.Spin360)
            {
                transform.localRotation = Quaternion.identity;
                swing = transform.DOLocalRotate(new Vector3(0f, 0f, swingSign >= 0f ? 360f : -360f), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => { transform.localRotation = Quaternion.identity; isSwinging = false; });
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, half);
                swing = transform.DOLocalRotate(new Vector3(0f, 0f, -half), duration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => { transform.localRotation = Quaternion.identity; isSwinging = false; });
            }

            var slashVfx = spec != null && spec.slashVfxPrefab != null ? spec.slashVfxPrefab : defaultSlashVfx;
            if (slashVfx != null)
            {
                // Vệt chém là cung do mũi kiếm vẽ ra quanh tay: tâm ở giữa lưỡi, vươn ra ngoài mũi kiếm
                Vector2 vfxCenter = style == MeleeAttackStyle.Spin360 ? pivotPos : hand + direction * (blade * 0.55f);
                var fx = CombatFx.Play(slashVfx, vfxCenter, Quaternion.Euler(0f, 0f, angle));
                if (fx != null)
                {
                    // Lật vệt chém theo chiều vung (đòn sau vung ngược đòn trước)
                    fx.transform.localScale = new Vector3(vfxScale, half >= 0f ? vfxScale : -vfxScale, 1f);
                }
            }
            Sfx.PlayOverride(spec != null ? spec.attackSfx : null, SfxId.SwordSwing);

            float mult = 1f;
            int extraPierce = 0;
            bool empowered = owner != null && owner.TryConsumeEmpowered(out mult, out extraPierce);
            float damage = RollDamage(out bool crit, empowered ? mult : 1f);

            DoHit(pivotPos, hand, direction, reach, arc, style == MeleeAttackStyle.Spin360, damage, crit);
            FireSwordWave(hand, direction, damage, crit, extraPierce);
            return true;
        }

        void FireSwordWave(Vector2 origin, Vector2 direction, float slashDamage, bool crit, int extraPierce)
        {
            if (data == null || spec.projectilePrefab == null || Services.Pool == null) return;
            var go = Services.Pool.Get(spec.projectilePrefab, origin, Quaternion.identity);
            if (go == null) return;
            if (!go.TryGetComponent<Projectile>(out var p)) { Services.Pool.Return(go); return; }

            float ratio = spec.swordWaveDamageRatio > 0f ? spec.swordWaveDamageRatio : 0.6f;
            float spd = spec.projectileSpeed > 0f ? spec.projectileSpeed : 12f;
            p.Launch(new ProjectileSpec
            {
                Damage = slashDamage * ratio,
                Speed = spd,
                Knockback = spec.knockback * 0.6f,
                Crit = crit,
                Pierce = spec.pierce + extraPierce,
                ExplosionRadius = spec.explosionRadius,
                Lifetime = Mathf.Max(0.4f, (spec.attackRange > 2f ? spec.attackRange : 6f) / spd),
                Team = Team.Player,
                ImpactVfx = spec.hitImpactVfxPrefab,
                Source = owner != null ? owner.GameObject : gameObject,
                Owner = owner,
                Weapon = data,
                Status = OnHitPayload,
            }, direction);
        }

        /// <summary>Bề rộng (world units, scale 1) của vệt chém trong sprite VFX (~72px / 16 PPU).</summary>
        const float SlashVfxArcWidth = 4.5f;

        /// <summary>
        /// Vùng trúng theo <see cref="MeleeHitbox"/> của vũ khí:
        /// Auto = quạt khớp cung vung (Swing / Spin) hoặc hộp hẹp phía trước (Thrust — trước đây là quạt 150°);
        /// Arc = quạt bán kính size.x, góc arc; Box = hộp dài size.x × rộng size.y theo hướng ngắm.
        /// Mục tiêu sát người (≤ tay + 0.35) luôn trúng; không chém xuyên tường.
        /// </summary>
        void DoHit(Vector2 pivotPos, Vector2 hand, Vector2 direction, float reach, float arc, bool fullCircle, float damage, bool crit)
        {
            int mask = CombatLayers.EnemyTargets | (spec != null && spec.destroysProjectiles ? CombatLayers.EnemyBullets : 0);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = true };
            float closeRange = Vector2.Distance(pivotPos, hand) + 0.35f;

            var hb = spec != null ? spec.hitbox : MeleeHitbox.Auto;
            var shape = hb.shape;
            bool thrust = spec != null && spec.meleeStyle == MeleeAttackStyle.Thrust;
            if (shape == HitboxShape.Auto && thrust)
            {
                // Đâm: hộp hẹp từ tay tới hết tầm (sửa lỗi quạt 150° trúng cả hai bên sườn)
                hb = new MeleeHitbox { shape = HitboxShape.Box, offset = Vector2.zero,
                                       size = new Vector2(Mathf.Max(0.8f, reach - Vector2.Distance(pivotPos, hand)), 0.9f) };
                shape = HitboxShape.Box;
            }
            Vector2 side = new(-direction.y, direction.x);
            Vector2 origin = hand + direction * hb.offset.x + side * hb.offset.y;
            float queryRadius = shape switch
            {
                HitboxShape.Arc => Vector2.Distance(pivotPos, origin) + Mathf.Max(0.1f, hb.size.x),
                HitboxShape.Box => Vector2.Distance(pivotPos, origin) + new Vector2(Mathf.Max(0.1f, hb.size.x), hb.size.y * 0.5f).magnitude,
                _ => reach,
            };
            float halfArc = shape == HitboxShape.Arc ? Mathf.Max(1f, hb.arc * 0.5f) : Mathf.Max(arc * 0.5f + 25f, 75f);

            int n = Physics2D.OverlapCircle(pivotPos, queryRadius, filter, buffer);
            struck.Clear();
            for (int i = 0; i < n; i++)
            {
                var col = buffer[i];
                buffer[i] = null;
                if (col == null) continue;
                // Điểm gần nhất của collider để quái to (boss) vẫn trúng khi mép thân nằm trong vùng
                Vector2 hitPoint = col.ClosestPoint(pivotPos);
                float dist = Vector2.Distance(pivotPos, hitPoint);
                bool inside = dist <= closeRange || fullCircle && shape == HitboxShape.Auto;
                if (!inside)
                {
                    switch (shape)
                    {
                        case HitboxShape.Box:
                        {
                            Vector2 local = (Vector2)col.ClosestPoint(origin + direction * hb.size.x * 0.5f) - origin;
                            float along = Vector2.Dot(local, direction), across = Vector2.Dot(local, side);
                            inside = along >= -0.05f && along <= hb.size.x && Mathf.Abs(across) <= hb.size.y * 0.5f;
                            break;
                        }
                        case HitboxShape.Arc:
                        {
                            Vector2 p2 = col.ClosestPoint(origin);
                            Vector2 to = p2 - origin;
                            inside = to.magnitude <= hb.size.x && Vector2.Angle(direction, to) <= halfArc;
                            break;
                        }
                        default:
                            inside = dist <= reach && Vector2.Angle(direction, hitPoint - pivotPos) <= halfArc;
                            break;
                    }
                }
                if (!inside) continue;
                // Chặn chém xuyên tường (vẫn cho phép phá đạn sát mép hoặc mục tiêu ở khoảng 0)
                if (dist > 0.05f && Physics2D.Linecast(pivotPos, hitPoint, CombatLayers.Walls).collider != null) continue;
                if (spec != null && spec.destroysProjectiles && col.TryGetComponent<Projectile>(out var proj))
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
                if (d.TakeDamage(new DamageInfo(damage, spec.knockback, dir, 0f, crit, owner.GameObject, Team.Player)))
                {
                    // Kiếm chỉ có vệt chém: không phát VFX trúng. Màu nguyên tố (đốt, độc, băng…) do VFX lặp của hiệu ứng
                    // trạng thái bám trên mục tiêu thể hiện; hitImpactVfx chỉ còn dùng cho kiếm khí.
                    Sfx.PlayAt(SfxId.SwordHit, d.transform.position);
                    ApplyOnHitStatus(d);
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
            if (spec != null) transform.localPosition = restLocalPos;
            base.CancelAttack();
        }
    }
}
