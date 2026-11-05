using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Tham số trạng thái gây lên mục tiêu (dùng chung cho kỹ năng & passive).</summary>
    [System.Serializable]
    public struct StatusPayload
    {
        [Range(0, 1)] public float slowPercent;
        public float slowDuration;
        public float dotDps;
        public float dotDuration;
        public string dotId;
        public GameObject dotLoopVfx;
        public float stunDuration;
        public bool freeze;

        public bool IsEmpty => slowPercent <= 0f && dotDps <= 0f && stunDuration <= 0f;

        public void ApplyTo(GameObject target, GameObject source)
        {
            if (IsEmpty || target == null) return;
            var s = target.GetComponentInParent<StatusEffectController>();
            if (s == null) return;
            if (slowPercent > 0f) s.ApplySlow(slowPercent, slowDuration);
            if (dotDps > 0f) s.ApplyDot(string.IsNullOrEmpty(dotId) ? "dot" : dotId, dotDps, dotDuration, source, dotLoopVfx);
            if (stunDuration > 0f) s.ApplyStun(stunDuration, freeze);
        }
    }
}
