using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Vũ khí cầm tay (WEAPONS §6.1). Không có Role Synergy.</summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "Dungeon Gathering/Weapon Data")]
    public class WeaponData : ItemData
    {
        public override bool IsWeapon => true;

        [Header("Loại")]
        public WeaponType weaponType = WeaponType.Melee;
        public RangedFireMode fireMode = RangedFireMode.Projectile;

        [Header("Hiển thị")]
        public Sprite weaponSprite;
        public Vector2 holdOffset = Vector2.zero;
        public Vector2 muzzleOffset = new(0.5f, 0f);
        [Tooltip("Prefab vũ khí riêng (tùy chọn). Trống → WeaponHolder tự dựng từ weaponSprite.")]
        public GameObject weaponPrefab;

        [Header("Chỉ số")]
        public float damage = 10f;
        [Tooltip("Số đòn / giây")] public float fireRate = 2f;
        [Tooltip("Bán kính chém hoặc tầm bắn")] public float attackRange = 1.2f;
        [Tooltip("Độ lệch đạn (độ)")] public float spread;
        public float knockback = 5f;
        [Range(0, 1)] public float critBonus = 0.05f;
        public int manaCost;
        public float projectileSpeed = 14f;
        [Min(1)] public int projectileCount = 1;
        [Tooltip("Số mục tiêu đạn xuyên qua thêm")] public int pierce;
        [Tooltip("> 0: đạn nổ diện rộng")] public float explosionRadius;
        [Tooltip("Góc vung kiếm (độ)")] public float swingArc = 110f;
        public float swingDuration = 0.15f;
        [Tooltip("Kiếm chém tan đạn thường của quái")] public bool destroysProjectiles = true;

        [Header("VFX & đạn")]
        public GameObject slashVfxPrefab;
        public GameObject projectilePrefab;
        public GameObject muzzleVfxPrefab;
        public GameObject hitImpactVfxPrefab;
        public AudioClip attackSfx;

        [Header("Kỹ năng")]
        public WeaponPassiveSO passive;
    }
}
