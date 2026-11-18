using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Bộ trang bị: mặc đủ 2 món kích hoạt cộng chỉ số, đủ 4 món kích hoạt nội tại.</summary>
    [CreateAssetMenu(fileName = "Set_", menuName = "Dungeon Gathering/Equipment Set")]
    public class EquipmentSetSO : ScriptableObject
    {
        [Tooltip("Id ngắn định danh bộ set (kebab-case); hiện runtime không đọc field này.")]
        public string setId;
        [Tooltip("Key tên bộ set trong bảng DG_Multilanguage (hiện chưa có UI đọc).")]
        public string nameKey;
        [Tooltip("Danh sách món thuộc set (chỉ tham khảo); runtime đếm set qua field equipmentSet của từng EquipmentSO.")]
        public List<EquipmentSO> pieces = new();
        [Tooltip("Chỉ số cộng khi mặc từ 2 món cùng set trở lên; rỗng = không có thưởng 2 món.")]
        public List<StatModifier> bonus2Pieces = new();
        [Tooltip("Nội tại bật khi mặc từ 4 món cùng set trở lên; null = không có thưởng 4 món.")]
        public ItemPassiveSO bonus4Passive;
    }
}
