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
        public float stillTime = 1f;
        public float damageMultiplier = 1.4f;
        public int extraPierce = 3;

        public override void OnEquip(IPlayerContext player)
        {
            var t = player.GameObject.GetOrAdd<StillnessTracker>();
            t.Configure(player, this);
        }

        public override void OnUnequip(IPlayerContext player)
        {
            if (player?.GameObject == null) return;
            var t = player.GameObject.GetComponent<StillnessTracker>();
            // Không Destroy: Destroy bị hoãn tới cuối frame → nếu OnEquip chạy lại cùng frame (PlayerEquipment.Apply: Clear rồi Equip)
            // GetOrAdd trả về component sắp bị hủy → passive mất. Chỉ gỡ cấu hình + tắt.
            if (t != null) t.Unconfigure();
        }
    }
}
