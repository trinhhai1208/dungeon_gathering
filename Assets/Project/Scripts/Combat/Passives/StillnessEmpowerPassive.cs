using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Giày Phong Vũ (Ranger): đứng yên N giây → đòn kế tiếp xuyên thấu + tăng sát thương.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Equip Stillness Empower", fileName = "EP_Stillness")]
    public class StillnessEmpowerPassive : EquipmentPassiveSO
    {
        [Tooltip("Số giây đứng yên (không di chuyển, không bị choáng) để nạp đòn cường hóa; tấn công sẽ đếm lại.")]
        public float stillTime = 1f;
        [Tooltip("Hệ số nhân sát thương cho đòn kế tiếp sau khi nạp (1.4 = +40%); 1 = không tăng.")]
        public float damageMultiplier = 1.4f;
        [Tooltip("Số mục tiêu xuyên thêm của đòn cường hóa kế tiếp; 0 = không xuyên thêm.")]
        public int extraPierce = 3;

        public override void OnEquip(IPlayerContext player)
        {
            if (player == null) return;
            var rt = PassiveRuntime.For(player);
            if (rt != null && ++rt.GetOrCreate(this).RefCount > 1) return;
            var t = player.GameObject.GetOrAdd<StillnessTracker>();
            t.Configure(player, this);
        }

        public override void OnUnequip(IPlayerContext player)
        {
            if (player?.GameObject == null) return;
            var rt = PassiveRuntime.For(player, false);
            if (rt != null && rt.TryGet(this, out var state))
            {
                if (--state.RefCount > 0) return;
                rt.Remove(this);
            }
            var t = player.GameObject.GetComponent<StillnessTracker>();
            // Không Destroy: Destroy bị hoãn tới cuối frame → nếu OnEquip chạy lại cùng frame (PlayerEquipment.Apply: Clear rồi Equip)
            // GetOrAdd trả về component sắp bị hủy → passive mất. Chỉ gỡ cấu hình + tắt.
            if (t != null) t.Unconfigure();
        }
    }
}
