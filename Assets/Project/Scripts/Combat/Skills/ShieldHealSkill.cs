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
        [Tooltip("Lượng máu hồi (đơn vị HP, Player ~6 HP); 0 = không hồi.")] public float heal = 2f;
        [Tooltip("Lượng giáp hồi (điểm giáp); 0 = không hồi.")] public float armor = 2f;
        [Tooltip("Lượng mana hồi; 0 = không hồi.")] public float mana;
        [Tooltip("Thời gian bất tử sau khi dùng (giây); 0 = không có khiên bất tử.")] public float invulnerableDuration = 2f;
        [Tooltip("Tăng tốc chạy tạm thời (vd 0.2 = +20%); 0 = không tăng tốc.")] public float moveSpeedBonus;
        [Tooltip("Thời gian tăng tốc chạy (giây).")] public float moveSpeedDuration = 3f;
        [Tooltip("VFX gắn theo Player, tồn tại ít nhất 1 giây hoặc bằng invulnerableDuration / buff; null = không có.")] public GameObject vfx;

        protected override SfxId DefaultActivateSfx => SfxId.Shield;

        public override void Activate(IPlayerContext ctx)
        {
            if (heal > 0f) ctx.Heal(heal);
            if (armor > 0f) ctx.RestoreArmor(armor);
            if (mana > 0f) ctx.RestoreMana(mana);
            if (moveSpeedBonus > 0f && moveSpeedDuration > 0f)
                ctx.AddTimedModifier(new StatModifier(StatType.MoveSpeed, 0f, moveSpeedBonus), moveSpeedDuration);

            float vfxDur = Mathf.Max(1f, invulnerableDuration, moveSpeedBonus > 0f ? moveSpeedDuration : 0f);
            if (vfx != null)
                CombatFx.AttachScaled(vfx, ctx.Transform, overhead: false, fallbackDuration: vfxDur);
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
