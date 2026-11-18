using System.Collections.Generic;
using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Hiệu ứng gây lên mục tiêu (dùng chung cho skill, passive, đòn quái, bẫy, đạn): danh sách StatusEffectRef —
    /// mỗi phần tử trỏ tới một StatusEffectSO + ghi đè thời gian / độ mạnh / xác suất.
    /// </summary>
    [System.Serializable]
    public struct StatusPayload
    {
        [Tooltip("Hiệu ứng gây ra (StatusEffectSO + ghi đè thời gian / độ mạnh / xác suất)")]
        public List<StatusEffectRef> effects;

        public bool IsEmpty
        {
            get
            {
                if (effects == null) return true;
                foreach (var e in effects) if (e.IsValid) return false;
                return true;
            }
        }

        public void ApplyTo(IDamageable target, GameObject source)
        {
            if (target is Component c && c != null) ApplyTo(c.gameObject, source);
        }

        public void ApplyTo(GameObject target, GameObject source)
        {
            if (IsEmpty || target == null) return;
            var s = target.GetComponentInParent<StatusEffectController>();
            if (s == null) return;
            foreach (var e in effects) s.Apply(e, source);
        }
    }
}
