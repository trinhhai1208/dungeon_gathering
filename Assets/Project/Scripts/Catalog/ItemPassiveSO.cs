using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Base chung cho passive vũ khí và trang bị.</summary>
    public abstract class ItemPassiveSO : ScriptableObject
    {
        [Tooltip("Mô tả dạng văn bản thô, hiện trong UI khi descKey rỗng (dự phòng).")]
        [TextArea] public string description;
        [Tooltip("Key mô tả trong DG_Multilanguage; có giá trị thì được dùng thay cho description.")]
        public string descKey;
        /// <summary>
        /// Chỉ số passive cộng cố định khi đang trang bị (không phụ thuộc trận đấu) — để UI ngoài trận (màn Trang bị) xem
        /// trước đúng như lúc chơi. Passive có điều kiện (máu thấp, sau khi giết…) để trống.
        /// </summary>
        public virtual System.Collections.Generic.IReadOnlyList<StatModifier> StatModifiers => System.Array.Empty<StatModifier>();
        public abstract void OnEquip(IPlayerContext player);
        public abstract void OnUnequip(IPlayerContext player);
    }
}
