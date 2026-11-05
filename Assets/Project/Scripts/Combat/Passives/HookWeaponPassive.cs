using System;
using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Base passive dựa trên hook chiến đấu. Handler + state riêng từng Player nằm trong <see cref="PassiveRuntime"/>
    /// (SO là asset dùng chung, không giữ state) để OnUnequip gỡ đúng và không rò state giữa các lần Play.
    /// </summary>
    public abstract class HookWeaponPassive : WeaponPassiveSO
    {
        public override void OnEquip(IPlayerContext player)
        {
            OnUnequip(player);
            var rt = PassiveRuntime.For(player);
            if (rt == null) return;
            var state = rt.GetOrCreate(this);
            Action<HitReport> h = r => OnHit(player, state, r);
            state.Handler = h;
            player.Hooks.HitDealt += h;
            OnEquipped(player, state);
        }

        public override void OnUnequip(IPlayerContext player)
        {
            var rt = PassiveRuntime.For(player, false);
            if (rt == null || !rt.TryGet(this, out var state)) return;
            if (state.Handler is Action<HitReport> h) player.Hooks.HitDealt -= h;
            rt.Remove(this);
            OnUnequipped(player, state);
        }

        protected virtual void OnEquipped(IPlayerContext p, PassiveState state) { }
        protected virtual void OnUnequipped(IPlayerContext p, PassiveState state) { }
        protected abstract void OnHit(IPlayerContext player, PassiveState state, in HitReport hit);
    }
}
