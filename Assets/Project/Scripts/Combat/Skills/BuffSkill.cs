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
        [Tooltip("Các chỉ số cộng thêm tạm thời cho Player, kéo dài theo duration của skill; rỗng = không buff.")] public List<StatModifier> modifiers = new();
        [Tooltip("VFX gắn dưới chân Player suốt thời gian buff; null = không có.")] public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.Buff;

        public override void Activate(IPlayerContext ctx)
        {
            foreach (var m in modifiers) ctx.AddTimedModifier(m, duration);
            if (vfx != null)
                CombatFx.AttachScaled(vfx, ctx.Transform, overhead: false, fallbackDuration: Mathf.Max(0.5f, duration));
        }
    }
}
