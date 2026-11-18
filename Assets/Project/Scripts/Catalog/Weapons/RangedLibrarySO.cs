using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Thư viện vũ khí tầm xa: một SO gốc + danh sách súng / cung / trượng (sprite, animator, nòng, đạn, VFX).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Weapons/Ranged Library", fileName = "RangedLibrary")]
    public sealed class RangedLibrarySO : ScriptableObject
    {
        [Tooltip("SO gốc: giá trị mặc định cho mọi vũ khí tầm xa. Trống → mọi súng dùng thông số tối thiểu")] public RangedBaseSO baseConfig;
        [Tooltip("Từng vũ khí tầm xa; id trùng id WeaponData, chỉ ghi đè phần khác SO gốc")] public List<RangedEntry> weapons = new();
    }
}
