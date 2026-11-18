using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Gốc chung của mọi skill: skill chủ động của hero / vật phẩm (<see cref="ActiveSkillSO"/>) và đòn của quái / boss
    /// (EnemyAttackSO). Mỗi lớp con là một kiểu hành vi (lướt, nổ vùng, bắn chùm, lao…) kèm tham số mặc định.
    /// Skill được liệt kê trong <see cref="SkillLibrarySO"/> và gán cho hero / quái / boss bằng enum <see cref="SkillId"/>.
    /// </summary>
    public abstract class SkillBaseSO : ScriptableObject
    {
        [Tooltip("Key định danh (vd \"shadow-step\") — trùng key của entry trong SkillLibrary")]
        public string skillId;
        [Tooltip("Key localization tên skill (Lobby / Loadout). Trống → hiện skillId")] public string nameKey;
        [Tooltip("Key localization mô tả skill (hiện chưa UI nào đọc)")] public string descKey;
        [Tooltip("Icon skill trên HUD trận đánh. Trống → ẩn icon")] public Sprite icon;
        [Min(0)] public float cooldown = 2f;
    }
}
