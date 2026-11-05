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
                    if (it == null || it.activeSkill == null) continue;
                    if (slot >= SlotCount) break; // loadout đã được Sanitize, đây chỉ là chốt an toàn
                    skills[slot] = it.activeSkill;
                    sources[slot] = it;
                    slot++;
                }
        }

        public ActiveSkillSO GetSkill(int slot) => slot is >= 0 and < SlotCount ? skills[slot] : null;
        public ItemData GetSource(int slot) => slot is >= 0 and < SlotCount ? sources[slot] : null;
        public float GetRemaining(int slot) => slot is >= 0 and < SlotCount ? Mathf.Max(0f, readyAt[slot] - Time.time) : 0f;
        public float GetCooldown(int slot) => GetSkill(slot) != null ? GetSkill(slot).cooldown : 0f;

        public bool TryUse(int slot)
        {
            var s = GetSkill(slot);
            if (s == null || ctx == null || ctx.IsDead) return false;
            if (Time.time < readyAt[slot]) return false;
            if (!s.CanActivate(ctx)) return false;
            if (!ctx.TrySpendMana(s.manaCost)) return false;
            readyAt[slot] = Time.time + s.cooldown;
            s.Activate(ctx);
            Sfx.Play(s.ActivateSfx);
            EventBus<ActiveSkillUsedEvent>.Raise(new ActiveSkillUsedEvent { SlotIndex = slot, Cooldown = s.cooldown });
            return true;
        }
    }
}
