using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Kỹ năng chủ động: slot 0 = kỹ năng nhân vật, slot 1..2 = tối đa 2 vật phẩm có kỹ năng chủ động (§5.5).
    /// HUD đọc GetRemaining/GetSkill mỗi frame để vẽ hồi chiêu.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerSkillController : MonoBehaviour
    {
        public const int SlotCount = 1 + LoadoutRules.MaxActiveItems;

        readonly ActiveSkillSO[] skills = new ActiveSkillSO[SlotCount];
        readonly ItemData[] sources = new ItemData[SlotCount];
        readonly float[] readyAt = new float[SlotCount];
        IPlayerContext ctx;

        public void Bind(IPlayerContext context, ActiveSkillSO characterSkill, IEnumerable<ItemData> loadoutItems)
        {
            ctx = context;
            for (int i = 0; i < SlotCount; i++) { skills[i] = null; sources[i] = null; readyAt[i] = 0f; }
            skills[0] = characterSkill;
            int slot = 1;
            if (loadoutItems != null)
                foreach (var it in loadoutItems)
                {
                    if (it == null || it.ActiveSkill == null) continue;
                    if (slot >= SlotCount) break; // loadout đã được Sanitize, đây chỉ là chốt an toàn
                    skills[slot] = it.ActiveSkill;
                    sources[slot] = it;
                    slot++;
                }
        }

        public ActiveSkillSO GetSkill(int slot) => slot is >= 0 and < SlotCount ? skills[slot] : null;
        public ItemData GetSource(int slot) => slot is >= 0 and < SlotCount ? sources[slot] : null;
        public float GetRemaining(int slot) => slot is >= 0 and < SlotCount ? Mathf.Max(0f, readyAt[slot] - Time.time) : 0f;
        public float GetCooldown(int slot) => EffectiveCooldown(GetSkill(slot));

        float EffectiveCooldown(ActiveSkillSO s)
        {
            if (s == null) return 0f;
            float cdr = ctx != null ? Mathf.Clamp(ctx.GetStat(StatType.CooldownReduction), 0f, 0.5f) : 0f;
            return SkillRegistry.Cooldown(s) * (1f - cdr);
        }

        public bool TryUse(int slot)
        {
            var s = GetSkill(slot);
            if (s == null || ctx == null || ctx.IsDead) return false;
            if (Time.time < readyAt[slot]) return false;
            if (!s.CanActivate(ctx)) return false;
            if (!ctx.TrySpendMana(SkillRegistry.ManaCost(s))) return false;
            float cd = EffectiveCooldown(s);
            readyAt[slot] = Time.time + cd;
            s.Activate(ctx);
            Sfx.Play(s.ActivateSfx);
            var entry = SkillRegistry.Entry(s);   // VFX / âm thanh kích hoạt khai báo ở SkillLibrary
            if (entry != null)
            {
                if (entry.castVfx != null) DungeonGathering.Combat.CombatFx.Play(entry.castVfx, ctx.Transform.position, Quaternion.identity);
                if (entry.castSfx != null) Sfx.PlayOverride(entry.castSfx, SfxId.None);
            }
            EventBus<ActiveSkillUsedEvent>.Raise(new ActiveSkillUsedEvent { SlotIndex = slot, Cooldown = cd });
            return true;
        }
    }
}
