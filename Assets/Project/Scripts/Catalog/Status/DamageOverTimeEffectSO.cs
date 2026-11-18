using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Sát thương theo thời gian (cháy, độc, chảy máu): magnitude = sát thương mỗi giây.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Status/Damage Over Time", fileName = "Status_Burn")]
    public sealed class DamageOverTimeEffectSO : StatusEffectSO
    {
        [Min(0.05f)] public float tickInterval = 0.5f;

        void Reset()
        {
            tags = StatusTag.DamageOverTime | StatusTag.Debuff;
            stacking = StackPolicy.KeepStrongest;
            defaultDuration = 3f;
            defaultMagnitude = 2f;
        }

        public override void OnApply(IStatusTarget target, StatusInstance inst) => inst.TickTimer = tickInterval;

        public override void OnTick(IStatusTarget target, StatusInstance inst, float dt)
        {
            inst.TickTimer -= dt;
            while (inst.TickTimer <= 0f && !target.IsDead)
            {
                inst.TickTimer += tickInterval;
                target.ApplyStatusDamage(inst.Magnitude * tickInterval, inst.Source);
            }
        }
    }
}
