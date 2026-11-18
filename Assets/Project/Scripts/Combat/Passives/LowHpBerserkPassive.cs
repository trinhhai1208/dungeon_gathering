using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Nội tại Berserk: HP dưới ngưỡng thì tăng sát thương và tốc đánh.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Low HP Berserk", fileName = "IP_LowHpBerserk")]
    public class LowHpBerserkPassive : ItemPassiveSO
    {
        [Range(0.1f, 0.9f)] public float hpThreshold = 0.5f;
        [Tooltip("% sát thương cộng thêm (0.35 = +35%, vào ΣPercent) khi tỉ lệ HP ≤ hpThreshold; 0 = không cộng.")]
        public float bonusAttackDamagePct = 0.35f;
        [Tooltip("% tốc đánh cộng thêm (0.25 = +25%, vào ΣPercent) khi tỉ lệ HP ≤ hpThreshold; 0 = không cộng.")]
        public float bonusAttackSpeedPct = 0.25f;

        public override void OnEquip(IPlayerContext player)
        {
            if (player == null) return;
            var rt = PassiveRuntime.For(player);
            if (rt == null) return;
            var state = rt.GetOrCreate(this);
            if (++state.RefCount > 1) return;

            state.Active = false;
            state.Tick = () =>
            {
                if (player == null || player.IsDead)
                {
                    if (state.Active && player != null)
                    {
                        player.RemoveModifiers(state);
                        state.Active = false;
                    }
                    return false;
                }

                float maxHp = player.GetStat(StatType.MaxHp);
                bool low = maxHp > 0f && (player.CurrentHp / maxHp) <= hpThreshold;
                if (low && !state.Active)
                {
                    state.Active = true;
                    if (bonusAttackDamagePct != 0f)
                        player.AddModifier(state, new StatModifier(StatType.AttackDamage, 0f, bonusAttackDamagePct));
                    if (bonusAttackSpeedPct != 0f)
                        player.AddModifier(state, new StatModifier(StatType.AttackSpeed, 0f, bonusAttackSpeedPct));
                }
                else if (!low && state.Active)
                {
                    state.Active = false;
                    player.RemoveModifiers(state);
                }
                return true;
            };

            rt.StartTicking(state);
        }

        public override void OnUnequip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player, false);
            if (rt == null || !rt.TryGet(this, out var state)) return;
            if (--state.RefCount > 0) return;
            if (state.Active && player != null) player.RemoveModifiers(state);
            state.Active = false;
            rt.Remove(this);
        }
    }
}
