using System;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Găng Tay Bóng Đêm (Assassin): chí mạng hồi mana + tăng tốc chạy ngắn.
    /// Buff tốc chạy KHÔNG cộng dồn: chí mạng khi đang có buff chỉ làm mới thời gian.
    /// </summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Equip Crit Reward", fileName = "EP_CritReward")]
    public class CritRewardPassive : EquipmentPassiveSO
    {
        public float mana = 2f;
        [Range(0, 2)] public float moveSpeedPercent = 0.2f;
        public float buffDuration = 1.5f;

        public override void OnEquip(IPlayerContext player)
        {
            OnUnequip(player);
            var rt = PassiveRuntime.For(player);
            if (rt == null) return;
            var state = rt.GetOrCreate(this);
            // Tick: hết hạn → gỡ modifier (nguồn = chính state). Trả false để runtime ngừng tick.
            state.Tick = () =>
            {
                if (!state.Active) return false;
                if (Time.time < state.Until) return true;
                player.RemoveModifiers(state);
                state.Active = false;
                return false;
            };
            Action<HitReport> h = r =>
            {
                if (!r.Crit) return;
                player.RestoreMana(mana);
                state.Until = Time.time + buffDuration;
                if (state.Active) return; // đang có buff → chỉ làm mới thời gian
                state.Active = true;
                player.AddModifier(state, new StatModifier(StatType.MoveSpeed, 0f, moveSpeedPercent));
                rt.StartTicking(state);
            };
            state.Handler = h;
            player.Hooks.HitDealt += h;
        }

        public override void OnUnequip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player, false);
            if (rt == null || !rt.TryGet(this, out var state)) return;
            if (state.Handler is Action<HitReport> h) player.Hooks.HitDealt -= h;
            if (state.Active) player.RemoveModifiers(state);
            state.Active = false;
            rt.Remove(this);
        }
    }
}
