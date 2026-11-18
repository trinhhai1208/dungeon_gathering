using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Choáng / đóng băng: mục tiêu không đi, không đánh, không dùng skill. Đóng băng = asset Stun có màu xanh.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Status/Stun (Freeze)", fileName = "Status_Stun")]
    public sealed class StunEffectSO : StatusEffectSO
    {
        [Tooltip("Hiển thị là đóng băng (dừng animation, màu băng)")] public bool freeze;

        public override bool IsCrowdControl => true;

        void Reset()
        {
            tags = StatusTag.CrowdControl | StatusTag.Debuff;
            stacking = StackPolicy.KeepStrongest;
            defaultDuration = 1f;
            defaultMagnitude = 1f;
        }
    }
}
