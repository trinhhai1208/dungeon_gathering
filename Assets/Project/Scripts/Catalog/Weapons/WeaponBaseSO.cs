using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Gốc mọi vũ khí: chỉ số + hiệu ứng mặc định. Kiếm (<see cref="SwordBaseSO"/>) và vũ khí tầm xa
    /// (<see cref="RangedBaseSO"/>) kế thừa và thêm thông số riêng. Từng vũ khí cụ thể là một entry trong thư viện,
    /// chỉ ghi đè những gì khác với gốc.
    /// </summary>
    public abstract class WeaponBaseSO : ScriptableObject
    {
        [Header("Chỉ số mặc định")]
        [Tooltip("Sát thương mỗi đòn / mỗi viên, nhân với chỉ số AttackDamage của người cầm")] public float damage = 15f;
        [Tooltip("Số đòn / giây")] public float fireRate = 1.5f;
        [Tooltip("Bán kính chém hoặc tầm bắn")] public float attackRange = 1.2f;
        [Tooltip("Lực đẩy lùi mục tiêu (giảm theo KnockbackResist); 0 = không đẩy, kiếm khí đẩy 60%")] public float knockback = 5f;
        [Range(0, 1)] public float critBonus = 0.05f;
        [Tooltip("Mana tốn mỗi lần tấn công; 0 = miễn phí, thiếu mana thì không đánh được")] public int manaCost;

        [Header("Cầm & hiển thị mặc định")]
        [Tooltip("Kiểu cầm; OneHandedSide trên vũ khí tầm xa tự đổi thành Ranged")] public WeaponHoldStyle holdStyle = WeaponHoldStyle.OneHandedSide;
        [Tooltip("Tỉ lệ hiển thị sprite vũ khí trên tay; ≤ 0 → 0.55")] public float visualScale = 0.55f;
        [Tooltip("Góc vẽ gốc của sprite (độ): kiếm 135° (mũi lên trên-trái), súng 0°")]
        public float spriteForwardAngle;

        [Header("Hiệu ứng mặc định")]
        [Tooltip("VFX khi đạn / tia / kiếm khí trúng; chém cận chiến không phát. Trống → defaultHitVfx")] public GameObject hitImpactVfx;
        [Tooltip("Âm khi tấn công. Trống → âm mặc định theo loại (vung kiếm / súng / phép / cung)")] public AudioClip attackSfx;
        [Tooltip("Hiệu ứng gây khi trúng (mọi vũ khí thuộc loại này)")]
        public List<StatusEffectRef> onHitStatus = new();

        public abstract WeaponType Type { get; }

        /// <summary>Chép giá trị mặc định chung vào spec (lớp con thêm phần riêng).</summary>
        public virtual void WriteDefaults(WeaponSpec s)
        {
            s.weaponType = Type;
            s.damage = damage; s.fireRate = fireRate; s.attackRange = attackRange; s.knockback = knockback;
            s.critBonus = critBonus; s.manaCost = manaCost;
            s.holdStyle = holdStyle; s.visualScale = visualScale;
            s.overrideForwardAngle = true; s.customForwardAngle = spriteForwardAngle;
            s.hitImpactVfxPrefab = hitImpactVfx; s.attackSfx = attackSfx;
            if (onHitStatus != null) s.onHitStatus.AddRange(onHitStatus);
        }
    }
}
