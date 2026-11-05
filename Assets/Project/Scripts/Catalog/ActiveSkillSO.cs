using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Kỹ năng chủ động (của nhân vật hoặc vật phẩm).</summary>
    public abstract class ActiveSkillSO : ScriptableObject
    {
        public string skillId;
        public string nameKey;
        public string descKey;
        public Sprite icon;
        public string displayName => !string.IsNullOrEmpty(nameKey) ? nameKey : skillId;
        [Min(0)] public float cooldown = 12f;
        [Min(0)] public float duration;
        [Min(0)] public float manaCost;
        [Tooltip("None = dùng âm mặc định của loại kỹ năng.")]
        public SfxId activateSfx = SfxId.None;

        /// <summary>Âm phát khi kích hoạt (PlayerSkillController gọi).</summary>
        public SfxId ActivateSfx => activateSfx != SfxId.None ? activateSfx : DefaultActivateSfx;
        protected virtual SfxId DefaultActivateSfx => SfxId.Buff;

        /// <summary>Có dùng được lúc này không (ngoài cooldown/mana do controller kiểm).</summary>
        public virtual bool CanActivate(IPlayerContext ctx) => ctx != null && !ctx.IsDead;
        public abstract void Activate(IPlayerContext ctx);
    }
}
