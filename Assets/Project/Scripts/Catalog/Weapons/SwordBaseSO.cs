using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Gốc vũ khí cận chiến (kiếm, giáo, dao): kiểu đánh, cung chém, hitbox, vệt chém, kiếm khí.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Weapons/Sword Base", fileName = "WeaponBase_Sword")]
    public sealed class SwordBaseSO : WeaponBaseSO
    {
        [Header("Cận chiến")]
        [Tooltip("Swing = vung cung; Thrust = đâm thẳng (cung ≤ 55°); Spin360 = xoay tròn trúng mọi hướng")] public MeleeAttackStyle meleeStyle = MeleeAttackStyle.Swing;
        [Tooltip("Góc vung (độ)")] public float swingArc = 110f;
        [Tooltip("Thời gian một lần vung (giây); hồi đòn ít nhất bằng giá trị này + 0.12s. ≤ 0 → giá trị prefab")] public float swingDuration = 0.15f;
        [Tooltip("Vùng trúng: Auto = khớp vệt chém, Arc = quạt, Box = hộp; offset/size theo hướng ngắm")] public MeleeHitbox hitbox = MeleeHitbox.Auto;
        [Tooltip("Chém tan đạn thường của quái")] public bool destroysProjectiles = true;
        [Tooltip("VFX vệt chém mỗi lần vung (đòn chém chỉ phát VFX này). Trống → VFX mặc định của prefab")] public GameObject slashVfx;
        [Tooltip("Tỉ lệ sát thương kiếm khí (khi entry có đạn)")] [Range(0.1f, 2f)] public float swordWaveDamageRatio = 0.6f;
        // Tốc độ kiếm khí: lấy theo đạn kiếm khí trong ProjectileLibrary, entry ghi đè bằng SwordEntry.waveSpeed
        // (field waveSpeed của SO gốc đã bỏ — luôn bị tốc độ của đạn ghi đè nên không có tác dụng).

        public override WeaponType Type => WeaponType.Melee;

        void Reset() { spriteForwardAngle = 135f; holdStyle = WeaponHoldStyle.OneHandedSide; }

        public override void WriteDefaults(WeaponSpec s)
        {
            base.WriteDefaults(s);
            s.meleeStyle = meleeStyle; s.swingArc = swingArc; s.swingDuration = swingDuration; s.hitbox = hitbox;
            s.destroysProjectiles = destroysProjectiles; s.slashVfxPrefab = slashVfx;
            s.swordWaveDamageRatio = swordWaveDamageRatio;
        }
    }
}
