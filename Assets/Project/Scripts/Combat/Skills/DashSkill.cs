using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Lướt (có thể bất tử trong lúc lướt): Bóng Đêm, Blink.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Dash", fileName = "Skill_Dash")]
    public class DashSkill : ActiveSkillSO
    {
        public float distance = 4f;
        public float dashDuration = 0.18f;
        public bool invulnerable = true;
        public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.Dash;

        public override void Activate(IPlayerContext ctx)
        {
            Vector2 dir = ctx.MoveInput.sqrMagnitude > 0.01f ? ctx.MoveInput : ctx.AimDirection;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            CombatFx.Play(vfx, ctx.Transform.position, Quaternion.identity);
            if (invulnerable) InvulnFor(ctx, dashDuration + 0.1f);
            ctx.Dash(dir.normalized, distance, dashDuration);
        }

        static async void InvulnFor(IPlayerContext ctx, float t)
        {
            var key = new object();
            ctx.SetInvulnerable(key, true);
            // Hủy sớm khi Player bị Destroy (token gắn destroyCancellationToken + AppToken); gỡ bất tử nếu Player còn.
            await PassiveRuntime.WaitWhileAlive(ctx, t);
            if (ctx != null && ctx.GameObject != null) ctx.SetInvulnerable(key, false);
        }
    }
}
