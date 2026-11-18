using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Buff / debuff chỉ số trong thời gian ngắn. Giá trị modifier × magnitude (mặc định 1) × số tầng.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Status/Stat Modifier", fileName = "Status_Buff")]
    public sealed class StatModifierEffectSO : StatusEffectSO
    {
        [Tooltip("Modifier áp khi đang dính; Flat và % đều nhân magnitude × số tầng, tự gỡ khi hết")] public List<StatModifier> modifiers = new();

        void Reset()
        {
            tags = StatusTag.Buff;
            stacking = StackPolicy.Refresh;
            defaultDuration = 5f;
            defaultMagnitude = 1f;
        }

        public override void OnApply(IStatusTarget target, StatusInstance inst) => Write(target, inst);
        public override void OnMagnitudeChanged(IStatusTarget target, StatusInstance inst) => Write(target, inst);
        public override void OnRemove(IStatusTarget target, StatusInstance inst) => target.Stats?.RemoveSource(inst.ModifierKey);

        void Write(IStatusTarget target, StatusInstance inst)
        {
            var s = target.Stats;
            if (s == null || modifiers == null) return;
            s.RemoveSource(inst.ModifierKey);
            foreach (var m in modifiers)
                s.Add(inst.ModifierKey, new StatModifier(m.Stat, m.FlatValue * inst.Magnitude, m.PercentValue * inst.Magnitude));
        }
    }
}
