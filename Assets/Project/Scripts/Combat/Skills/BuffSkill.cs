using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Buff chỉ số tạm thời (Cuồng nộ, tăng tốc...).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Skills/Buff", fileName = "Skill_Buff")]
    public class BuffSkill : ActiveSkillSO
    {
        public List<StatModifier> modifiers = new();
        public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.Buff;

        public override void Activate(IPlayerContext ctx)
        {
            foreach (var m in modifiers) ctx.AddTimedModifier(m, duration);
            if (vfx != null)
            {
                Vector3 feetPos = ctx.Transform.position + new Vector3(0f, -0.35f, 0f);
                CombatFx.Play(vfx, feetPos, Quaternion.identity, ctx.Transform, Mathf.Max(0.5f, duration));
            }
        }
    }
}
