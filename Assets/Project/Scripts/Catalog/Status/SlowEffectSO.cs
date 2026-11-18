using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Làm chậm: modifier MoveSpeed −magnitude (0.3 = −30%) trên StatSheet, tự gỡ khi hết.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Status/Slow", fileName = "Status_Slow")]
    public sealed class SlowEffectSO : StatusEffectSO
    {
        void Reset()
        {
            tags = StatusTag.Debuff;
            stacking = StackPolicy.KeepStrongest;
            defaultDuration = 2f;
            defaultMagnitude = 0.3f;
        }

        public static float Clamp(float magnitude) => Mathf.Clamp(magnitude, 0f, 0.95f);

        // Nhiều nguồn làm chậm (khác asset) trên cùng một StatSheet: chỉ áp nguồn MẠNH NHẤT qua một modifier chung,
        // khớp nhánh dự phòng của StatusEffectController (Mathf.Max). Cộng dồn % sẽ vượt −100% → MoveSpeed = 0.
        static readonly object SharedKey = new();
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<StatSheet, Dictionary<StatusInstance, float>> active = new();

        public override void OnApply(IStatusTarget target, StatusInstance inst) => Write(target, inst, Clamp(inst.Magnitude));
        public override void OnMagnitudeChanged(IStatusTarget target, StatusInstance inst) => Write(target, inst, Clamp(inst.Magnitude));
        public override void OnRemove(IStatusTarget target, StatusInstance inst) => Write(target, inst, null);

        static void Write(IStatusTarget target, StatusInstance inst, float? magnitude)
        {
            var s = target.Stats;
            if (s == null) return;
            var map = active.GetOrCreateValue(s);
            if (magnitude.HasValue) map[inst] = magnitude.Value; else map.Remove(inst);
            float strongest = 0f;
            foreach (var m in map.Values) strongest = Mathf.Max(strongest, m);
            s.RemoveSource(SharedKey);
            if (strongest > 0f) s.Add(SharedKey, new StatModifier(StatType.MoveSpeed, 0f, -strongest));
        }
    }
}
