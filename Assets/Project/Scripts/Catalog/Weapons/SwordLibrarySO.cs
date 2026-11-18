using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Thư viện vũ khí cận chiến: một SO gốc + danh sách kiếm / giáo / dao (sprite, hitbox, kiếm khí, VFX).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Weapons/Sword Library", fileName = "SwordLibrary")]
    public sealed class SwordLibrarySO : ScriptableObject
    {
        [Tooltip("SO gốc: giá trị mặc định cho mọi vũ khí cận chiến. Trống → mọi kiếm dùng thông số tối thiểu")] public SwordBaseSO baseConfig;
        [Tooltip("Từng vũ khí cận chiến; id trùng id WeaponData, chỉ ghi đè phần khác SO gốc")] public List<SwordEntry> swords = new();
    }
}
