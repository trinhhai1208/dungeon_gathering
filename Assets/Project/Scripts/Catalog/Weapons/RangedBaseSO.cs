using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Gốc vũ khí tầm xa (súng, cung, trượng): kiểu bắn, chùm, loạt, giật nòng, đạn mặc định.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Weapons/Ranged Base", fileName = "WeaponBase_Ranged")]
    public sealed class RangedBaseSO : WeaponBaseSO
    {
        [Header("Tầm xa")]
        [Tooltip("Projectile = bắn đạn bay; Hitscan = tia trúng tức thì. Không có prefab đạn → luôn Hitscan")] public RangedFireMode fireMode = RangedFireMode.Projectile;
        [Tooltip("Độ lệch đạn (độ)")] public float spread;
        [Min(1)] public int projectileCount = 1;
        [Min(1)] public int burstCount = 1;
        [Min(0.02f)] public float burstInterval = 0.08f;
        [Tooltip("Độ giật lùi của vũ khí mỗi phát (world-unit); 0 = không giật")] public float recoilDistance = 0.12f;
        [Tooltip("Thời gian một lần giật, lùi + về (giây); ≤ 0 → giá trị của prefab vũ khí")] public float recoilDuration = 0.08f;
        [Tooltip("VFX đầu nòng mỗi phát. Trống → VFX mặc định của prefab vũ khí")] public GameObject muzzleVfx;
        [Tooltip("Id đạn trong ProjectileLibrary dùng khi entry không chọn đạn")] public string defaultProjectileId;

        public override WeaponType Type => WeaponType.Ranged;

        void Reset() { spriteForwardAngle = 0f; holdStyle = WeaponHoldStyle.Ranged; attackRange = 8f; }

        public override void WriteDefaults(WeaponSpec s)
        {
            base.WriteDefaults(s);
            s.fireMode = fireMode; s.spread = spread; s.projectileCount = projectileCount;
            s.burstCount = burstCount; s.burstInterval = burstInterval;
            s.recoilDistance = recoilDistance; s.recoilDuration = recoilDuration; s.muzzleVfxPrefab = muzzleVfx;
        }
    }
}
