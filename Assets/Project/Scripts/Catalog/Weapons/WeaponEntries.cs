using System;
using System.Collections.Generic;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Một vũ khí cụ thể trong thư viện: hình, hitbox / nòng, đạn, VFX và phần chỉ số ghi đè so với SO gốc.
    /// Ô ghi đè tắt = dùng giá trị của SO gốc.
    /// </summary>
    [Serializable]
    public abstract class WeaponEntryBase
    {
        [Header("Định danh")]
        [Tooltip("Trùng id vật phẩm (vd \"sword-iron\")")] public string id;
        public string nameKey;
        public string descKey;
        public Rarity rank = Rarity.D;
        public Sprite icon;
        public int shopPrice;

        [Header("Hiển thị")]
        public Sprite sprite;
        [Tooltip("Lệch vị trí sprite so với tay cầm")] public Vector2 holdOffset;
        public Overridable<float> visualScale;
        [Tooltip("Góc vẽ gốc của sprite (độ)")] public Overridable<float> spriteForwardAngle;
        public Overridable<WeaponHoldStyle> holdStyle;
        [Tooltip("Góc nâng khi đứng yên (độ). 0 = theo kiểu cầm")] public float restAngle;
        [Tooltip("Prefab view riêng (tùy chọn). Trống → dựng từ sprite")] public GameObject viewPrefab;

        [Header("Chỉ số ghi đè")]
        public Overridable<float> damage;
        public Overridable<float> fireRate;
        public Overridable<float> attackRange;
        public Overridable<float> knockback;
        public Overridable<float> critBonus;
        public Overridable<int> manaCost;

        [Header("Hiệu ứng")]
        [Tooltip("VFX khi đạn trúng (súng / cung / trượng, và kiếm khí). Chém cận chiến không phát VFX trúng — chỉ có vệt chém. " +
                 "Trống = của đạn / SO gốc")] public GameObject hitVfx;
        public AudioClip attackSfx;
        [Tooltip("Hiệu ứng gây thêm khi trúng (cộng với của SO gốc)")] public List<StatusEffectRef> statusEffects = new();
        public WeaponPassiveSO passive;
        public ActiveSkillSO activeSkill;

        public virtual void Apply(WeaponSpec s)
        {
            s.id = id;
            s.weaponSprite = sprite;
            s.holdOffset = holdOffset;
            s.visualScale = visualScale.Resolve(s.visualScale);
            if (spriteForwardAngle.enabled) { s.overrideForwardAngle = true; s.customForwardAngle = spriteForwardAngle.value; }
            s.holdStyle = holdStyle.Resolve(s.holdStyle);
            s.customRestAngle = restAngle;
            s.weaponPrefab = viewPrefab;
            s.damage = damage.Resolve(s.damage);
            s.fireRate = fireRate.Resolve(s.fireRate);
            s.attackRange = attackRange.Resolve(s.attackRange);
            s.knockback = knockback.Resolve(s.knockback);
            s.critBonus = critBonus.Resolve(s.critBonus);
            s.manaCost = manaCost.Resolve(s.manaCost);
            if (hitVfx != null) s.hitImpactVfxPrefab = hitVfx;
            if (attackSfx != null) s.attackSfx = attackSfx;
            if (statusEffects != null) s.onHitStatus.AddRange(statusEffects);
        }
    }

    [Serializable]
    public sealed class SwordEntry : WeaponEntryBase
    {
        [Header("Cận chiến")]
        [Tooltip("Kiểu đánh Swing / Thrust / Spin360. Bỏ tick = SO gốc")] public Overridable<MeleeAttackStyle> meleeStyle;
        [Tooltip("Góc vung (độ); Thrust tối đa 55°, Spin360 luôn 360°. Bỏ tick = SO gốc")] public Overridable<float> swingArc;
        [Tooltip("Thời gian một lần vung (giây). Bỏ tick = SO gốc")] public Overridable<float> swingDuration;
        [Tooltip("Vùng trúng: offset/size theo hướng ngắm (x = phía trước)")] public Overridable<MeleeHitbox> hitbox;
        [Tooltip("Chém tan đạn của quái trong vùng trúng. Bỏ tick = SO gốc")] public Overridable<bool> destroysProjectiles;
        [Tooltip("VFX vệt chém riêng. Trống = của SO gốc")] public GameObject slashVfx;

        [Header("Kiếm khí (tùy chọn)")]
        [Tooltip("Id trong ProjectileLibrary; trống = không bắn kiếm khí")] public string waveProjectileId;
        [Tooltip("Sát thương kiếm khí = sát thương đòn chém × tỉ lệ này. Bỏ tick = SO gốc")] public Overridable<float> swordWaveDamageRatio;
        [Tooltip("Tốc độ kiếm khí (world-unit / giây). Bỏ tick = speed của đạn kiếm khí")] public Overridable<float> waveSpeed;
        [Tooltip("Số mục tiêu kiếm khí xuyên thêm. Bỏ tick = theo đạn kiếm khí")] public Overridable<int> pierce;
        [Tooltip("Bán kính nổ kiếm khí (world-unit), 0 = không nổ. Bỏ tick = theo đạn kiếm khí")] public Overridable<float> explosionRadius;

        public override void Apply(WeaponSpec s)
        {
            base.Apply(s);
            s.meleeStyle = meleeStyle.Resolve(s.meleeStyle);
            s.swingArc = swingArc.Resolve(s.swingArc);
            s.swingDuration = swingDuration.Resolve(s.swingDuration);
            s.hitbox = hitbox.Resolve(s.hitbox);
            s.destroysProjectiles = destroysProjectiles.Resolve(s.destroysProjectiles);
            if (slashVfx != null) s.slashVfxPrefab = slashVfx;
            if (!string.IsNullOrEmpty(waveProjectileId) && WeaponLibrary.TryGetProjectile(waveProjectileId, out var p)) p.Apply(s);
            if (hitVfx != null) s.hitImpactVfxPrefab = hitVfx; // VFX của entry thắng VFX của đạn
            s.swordWaveDamageRatio = swordWaveDamageRatio.Resolve(s.swordWaveDamageRatio);
            s.projectileSpeed = waveSpeed.Resolve(s.projectileSpeed);
            s.pierce = pierce.Resolve(s.pierce);
            s.explosionRadius = explosionRadius.Resolve(s.explosionRadius);
        }
    }

    [Serializable]
    public sealed class RangedEntry : WeaponEntryBase
    {
        [Header("Tầm xa")]
        [Tooltip("Animator của súng (trigger \"Fire\" khi bắn) — tùy chọn")] public RuntimeAnimatorController animator;
        [Tooltip("Vị trí đầu nòng so với tay cầm (world-unit, x = phía trước); đạn và VFX nòng xuất phát ở đây")] public Vector2 muzzleOffset = new(0.5f, 0f);
        [Tooltip("Id trong ProjectileLibrary; trống = đạn mặc định của SO gốc")] public string projectileId;
        [Tooltip("Kiểu bắn Projectile / Hitscan. Bỏ tick = SO gốc")] public Overridable<RangedFireMode> fireMode;
        [Tooltip("Độ lệch (độ): nhiều viên chia đều trong ±spread, 1 viên lệch ngẫu nhiên. Bỏ tick = SO gốc")] public Overridable<float> spread;
        [Tooltip("Số viên mỗi phát (bắn chùm, tối thiểu 1). Bỏ tick = SO gốc")] public Overridable<int> projectileCount;
        [Tooltip("Số phát liên tiếp mỗi lần bắn (tối thiểu 1). Bỏ tick = SO gốc")] public Overridable<int> burstCount;
        [Tooltip("Giãn cách giữa các phát trong loạt (giây, tối thiểu 0.02). Bỏ tick = SO gốc")] public Overridable<float> burstInterval;
        [Tooltip("Tốc độ đạn (world-unit / giây). Bỏ tick = speed của đạn đã chọn")] public Overridable<float> projectileSpeed;
        [Tooltip("Số mục tiêu đạn xuyên thêm. Bỏ tick = theo đạn đã chọn")] public Overridable<int> pierce;
        [Tooltip("Bán kính nổ (world-unit), 0 = không nổ. Bỏ tick = theo đạn đã chọn")] public Overridable<float> explosionRadius;
        [Tooltip("Độ giật lùi mỗi phát (world-unit). Bỏ tick = SO gốc")] public Overridable<float> recoilDistance;
        [Tooltip("VFX đầu nòng riêng. Trống = của SO gốc")] public GameObject muzzleVfx;

        public override void Apply(WeaponSpec s)
        {
            base.Apply(s);
            s.animator = animator;
            s.muzzleOffset = muzzleOffset;
            if (!string.IsNullOrEmpty(projectileId) && WeaponLibrary.TryGetProjectile(projectileId, out var p)) p.Apply(s);
            if (hitVfx != null) s.hitImpactVfxPrefab = hitVfx; // VFX của entry thắng VFX của đạn
            s.fireMode = fireMode.Resolve(s.fireMode);
            s.spread = spread.Resolve(s.spread);
            s.projectileCount = Mathf.Max(1, projectileCount.Resolve(s.projectileCount));
            s.burstCount = Mathf.Max(1, burstCount.Resolve(s.burstCount));
            s.burstInterval = burstInterval.Resolve(s.burstInterval);
            s.projectileSpeed = projectileSpeed.Resolve(s.projectileSpeed);
            s.pierce = pierce.Resolve(s.pierce);
            s.explosionRadius = explosionRadius.Resolve(s.explosionRadius);
            s.recoilDistance = recoilDistance.Resolve(s.recoilDistance);
            if (muzzleVfx != null) s.muzzleVfxPrefab = muzzleVfx;
        }
    }

    /// <summary>Một loại đạn: prefab (có component Projectile) + thông số mặc định. Vũ khí chọn theo id.</summary>
    [Serializable]
    public sealed class ProjectileEntry
    {
        [Tooltip("Id đạn để vũ khí tham chiếu (projectileId / waveProjectileId / defaultProjectileId)")] public string id;
        [Tooltip("Prefab đạn (cần component Projectile). Trống → súng bắn Hitscan, kiếm không ra kiếm khí")] public GameObject prefab;
        [Tooltip("Tốc độ đạn (world-unit / giây); entry vũ khí có thể ghi đè")] public float speed = 12f;
        [Tooltip("Số mục tiêu xuyên thêm sau mục tiêu đầu; 0 = biến mất khi trúng")] public int pierce;
        [Tooltip("Bán kính nổ (world-unit) khi trúng hoặc hết tầm; 0 = không nổ")] public float explosionRadius;
        [Tooltip("VFX khi đạn trúng; thắng hitImpactVfx của SO gốc, thua hitVfx của entry vũ khí. Trống → giữ VFX của SO gốc")] public GameObject hitVfx;
        [Tooltip("Hiệu ứng gây khi đạn trúng (cộng thêm vào hiệu ứng của SO gốc và entry vũ khí)")] public List<StatusEffectRef> statusEffects = new();

        public void Apply(WeaponSpec s)
        {
            s.projectilePrefab = prefab;
            s.projectileSpeed = speed;
            s.pierce = pierce;
            s.explosionRadius = explosionRadius;
            if (hitVfx != null) s.hitImpactVfxPrefab = hitVfx;
            if (statusEffects != null) s.onHitStatus.AddRange(statusEffects);
        }
    }
}
