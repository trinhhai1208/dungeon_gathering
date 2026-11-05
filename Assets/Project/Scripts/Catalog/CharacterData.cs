using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Lớp nhân vật (ENTITIES §2.2). Một Player.prefab cho mọi nhân vật; khác biệt = data + AnimatorOverride.</summary>
    [CreateAssetMenu(fileName = "Char_", menuName = "Dungeon Gathering/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Định danh")]
        public string characterId = "knight";
        public string nameKey;
        public string descKey;
        public CharacterRole role = CharacterRole.Warrior;
        [Min(0)] public int unlockPrice;
        [Tooltip("false = Chưa ra mắt: vẫn hiện ở sảnh (dáng tối + nhãn), xem được giới thiệu nhưng không mở khóa / không vào run.")]
        public bool released = true;

        [Header("Hình")]
        public Sprite portrait;
        [Tooltip("AnimatorOverrideController (hoặc controller riêng) gắn lên Visual của Player.prefab")]
        public RuntimeAnimatorController animator;

        [Header("Chỉ số gốc")]
        public BaseStats baseStats = BaseStats.Default;
        [Tooltip("Giây không bị đánh trước khi giáp tự hồi")] public float armorRegenDelay = 4f;
        [Tooltip("Giáp hồi mỗi giây khi đang hồi")] public float armorRegenRate = 1f;
        [Tooltip("Mana tự hồi mỗi giây")] public float manaRegen = 2f;

        [Header("Khởi đầu")]
        public WeaponData startingWeapon;
        public ActiveSkillSO activeSkill;
    }
}
