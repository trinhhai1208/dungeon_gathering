using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    public enum HitboxShape
    {
        /// <summary>Tự tính như cũ: quạt theo độ dài lưỡi + vệt chém (Swing / Spin), hộp hẹp với Thrust.</summary>
        Auto = 0,
        /// <summary>Quạt: bán kính = size.x, góc = arc, tâm = tay cầm + offset.</summary>
        Arc = 1,
        /// <summary>Hộp xoay theo hướng ngắm: dài size.x × rộng size.y, bắt đầu từ tay cầm + offset.</summary>
        Box = 2,
    }

    /// <summary>Vùng trúng của đòn cận chiến. offset/size theo trục hướng ngắm (x = phía trước, y = ngang).</summary>
    [Serializable]
    public struct MeleeHitbox
    {
        [Tooltip("Auto = khớp vệt chém (Thrust → hộp hẹp); Arc = quạt bán kính size.x; Box = hộp size.x × size.y")] public HitboxShape shape;
        [Tooltip("Dời gốc vùng trúng từ tay cầm (world-unit; x = phía trước, y = ngang). Arc / Box mới dùng")] public Vector2 offset;
        [Tooltip("Arc: x = bán kính; Box: x = dài, y = rộng (world-unit). Auto không dùng")] public Vector2 size;
        [Tooltip("Góc quạt (độ) — chỉ dùng với Arc")] public float arc;

        public static MeleeHitbox Auto => new() { shape = HitboxShape.Auto, size = new Vector2(1.6f, 0.9f), arc = 110f };
    }

    /// <summary>
    /// Thông số vũ khí đã resolve (SO gốc ← entry thư viện, hoặc từ WeaponData cũ). Runtime, không ghi vào SO.
    /// Tên field giữ giống WeaponData để code vũ khí đọc thống nhất.
    /// </summary>
    public sealed class WeaponSpec
    {
        public WeaponData Item;                 // định danh vật phẩm (hook passive, HitReport)
        public string id;
        public bool fromLibrary;

        public WeaponType weaponType;
        public RangedFireMode fireMode;
        public WeaponHoldStyle holdStyle;
        public float customRestAngle;

        // Hiển thị
        public Sprite weaponSprite;
        public RuntimeAnimatorController animator;
        public Vector2 holdOffset;
        public Vector2 muzzleOffset;
        public bool overrideForwardAngle;
        public float customForwardAngle;
        public float visualScale = 0.55f;
        public GameObject weaponPrefab;

        // Chỉ số
        public float damage, fireRate, attackRange, spread, knockback, critBonus;
        public int manaCost;
        public float projectileSpeed;
        public int projectileCount = 1, pierce;
        public float explosionRadius;
        public MeleeAttackStyle meleeStyle;
        public float swingArc, swingDuration;
        public bool destroysProjectiles;
        public float swordWaveDamageRatio = 0.6f;
        public int burstCount = 1;
        public float burstInterval = 0.08f;
        public float recoilDistance = 0.12f, recoilDuration = 0.08f;
        public MeleeHitbox hitbox = MeleeHitbox.Auto;

        // VFX / âm thanh / đạn
        public GameObject slashVfxPrefab, projectilePrefab, muzzleVfxPrefab, hitImpactVfxPrefab;
        public AudioClip attackSfx;
        /// <summary>Hiệu ứng gây khi trúng (đòn chém, đạn, tia).</summary>
        public readonly List<StatusEffectRef> onHitStatus = new();

        public WeaponHoldStyle EffectiveHoldStyle => holdStyle != WeaponHoldStyle.OneHandedSide
            ? holdStyle
            : (weaponType == WeaponType.Ranged ? WeaponHoldStyle.Ranged : WeaponHoldStyle.OneHandedSide);

        public float GetForwardAngle(float defaultMelee, float defaultRanged) =>
            overrideForwardAngle ? customForwardAngle : (weaponType == WeaponType.Melee ? defaultMelee : defaultRanged);

        public float Dps => damage * fireRate * Mathf.Max(1, projectileCount) * Mathf.Max(1, burstCount);

        /// <summary>Spec tối thiểu khi vũ khí chưa có entry trong thư viện (cấu hình thiếu — Validate All báo lỗi).</summary>
        public static WeaponSpec Missing(WeaponData w) => new()
        {
            Item = w, id = w != null ? w.id : "", fromLibrary = false,
            weaponType = WeaponType.Melee, damage = 1f, fireRate = 1f, attackRange = 1.2f, swingArc = 90f, swingDuration = 0.15f,
            visualScale = 0.55f, weaponSprite = w != null ? w.icon : null,
        };
    }
}
