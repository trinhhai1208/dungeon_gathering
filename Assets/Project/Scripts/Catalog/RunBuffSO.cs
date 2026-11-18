using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Buff trong run (chỉ số + nội tại tùy chọn) tồn tại hết các tầng của run.</summary>
    [CreateAssetMenu(fileName = "Buff_", menuName = "Dungeon Gathering/Run Buff")]
    public class RunBuffSO : ScriptableObject
    {
        [Tooltip("Id ngắn định danh buff trong run (kebab-case như id vật phẩm).")]
        public string buffId;
        [Tooltip("Key tên buff trong bảng DG_Multilanguage.")]
        public string nameKey;
        [Tooltip("Key mô tả buff trong bảng DG_Multilanguage.")]
        public string descKey;
        [Tooltip("Icon hiển thị của buff; null = không có icon.")]
        public Sprite icon;
        [Tooltip("Bậc hiếm của buff (S cao nhất → D thấp nhất), dữ liệu hiển thị; Apply không dùng.")]
        public Rarity rarity = Rarity.B;
        [Tooltip("Chỉ số cộng khi nhận buff, giữ tới hết run, theo (gốc + ΣFlat) × (1 + ΣPercent); rỗng = không cộng.")]
        public List<StatModifier> statModifiers = new();
        [Tooltip("Nội tại tùy chọn bật khi nhận buff và tắt khi gỡ buff; null = buff chỉ có chỉ số.")]
        public ItemPassiveSO passive;

        public void Apply(IPlayerContext player)
        {
            if (player == null) return;
            foreach (var m in statModifiers) player.AddModifier(this, m);
            if (passive != null) passive.OnEquip(player);
        }

        public void Remove(IPlayerContext player)
        {
            if (player == null) return;
            player.RemoveModifiers(this);
            if (passive != null) passive.OnUnequip(player);
        }
    }
}
