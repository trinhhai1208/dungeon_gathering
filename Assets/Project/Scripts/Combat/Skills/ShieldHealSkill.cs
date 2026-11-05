using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Hồi máu/giáp + khiên bất tử ngắn: Thánh Quang, Giơ Khiên.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Shield Heal", fileName = "Skill_ShieldHeal")]
    public class ShieldHealSkill : ActiveSkillSO
    {
        public float heal = 2f;
        public float armor = 2f;
        public float mana;
        public float invulnerableDuration = 2f;
        public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.Shield;

        public override void Activate(IPlayerContext ctx)
        {
            if (heal > 0f) ctx.Heal(heal);
            if (armor > 0f) ctx.RestoreArmor(armor);
            if (mana > 0f) ctx.RestoreMana(mana);
            if (vfx != null)
            {
                var fx = CombatFx.Play(vfx, ctx.Transform.position, Quaternion.identity, ctx.Transform, Mathf.Max(1f, invulnerableDuration));
                if (fx != null) fx.transform.localPosition = Vector3.zero;
            }
            if (invulnerableDuration > 0f) Invuln(ctx);
        }

        async void Invuln(IPlayerContext ctx)
        {
            var key = new object();
            ctx.SetInvulnerable(key, true);
            // Hủy sớm khi Player bị Destroy (token gắn destroyCancellationToken + AppToken); gỡ bất tử nếu Player còn.
            await PassiveRuntime.WaitWhileAlive(ctx, invulnerableDuration);
            if (ctx != null && ctx.GameObject != null) ctx.SetInvulnerable(key, false);
        }
    }
}
