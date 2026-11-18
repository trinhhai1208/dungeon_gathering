using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Runner cho StillnessEmpowerPassive (SO không có Update).</summary>
    public sealed class StillnessTracker : MonoBehaviour
    {
        IPlayerContext ctx;
        StillnessEmpowerPassive cfg;
        StatusEffectController status;
        float still;
        bool queued;

        public void Configure(IPlayerContext c, StillnessEmpowerPassive p)
        {
            if (ctx != null && ctx != c) ctx.Hooks.Attacked -= OnAttacked;
            ctx = c; cfg = p; still = 0f; queued = false;
            status = ctx != null && ctx.GameObject != null ? ctx.GameObject.GetComponent<StatusEffectController>() : null;
            ctx.Hooks.Attacked -= OnAttacked;
            ctx.Hooks.Attacked += OnAttacked;
            enabled = true;
        }

        /// <summary>Gỡ passive (không Destroy để tránh xung đột với OnEquip lại trong cùng frame).</summary>
        public void Unconfigure()
        {
            if (ctx != null) ctx.Hooks.Attacked -= OnAttacked;
            ctx = null; cfg = null; status = null; still = 0f; queued = false;
            enabled = false;
        }

        void OnAttacked(WeaponData _) { queued = false; still = 0f; }

        void Update()
        {
            if (ctx == null || cfg == null || ctx.IsDead) return;
            if ((status != null && status.IsStunned) || ctx.MoveInput.sqrMagnitude > 0.01f) { still = 0f; return; }
            if (queued) return;
            still += Time.deltaTime;
            if (still >= cfg.stillTime)
            {
                queued = true;
                ctx.QueueEmpoweredAttack(cfg.damageMultiplier, cfg.extraPierce);
            }
        }

        void OnDestroy()
        {
            if (ctx != null) ctx.Hooks.Attacked -= OnAttacked;
        }
    }
}
