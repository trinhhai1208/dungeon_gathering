using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Định nghĩa hero: định danh, bậc, hình / animation, VFX đòn đánh, chỉ số (StatProfile + ghi đè), vũ khí khởi đầu,
    /// skill (SkillId), nội tại. Một Player.prefab cho mọi hero — khác biệt nằm hết ở đây.
    /// </summary>
    [CreateAssetMenu(fileName = "Char_", menuName = "Dungeon Gathering/Hero Definition")]
    public class HeroDefinitionSO : ScriptableObject
    {
        [Header("Định danh")]
        [Tooltip("ID duy nhất của hero: khóa lưu save (mở khóa, loadout, tiến trình) và tra cứu trong catalog.")]
        public string characterId = "knight";
        [Tooltip("Khóa localization tên hero (sảnh, loadout, pause). Trống = hiện tên dự phòng.")]
        public string nameKey;
        [Tooltip("Khóa localization mô tả hero ở bảng nhân vật. Trống = không hiện mô tả.")]
        public string descKey;
        [Tooltip("Vai trò hero: hiện nhãn / icon ở sảnh và kích hoạt hiệu ứng cộng hưởng của trang bị theo vai trò.")]
        public CharacterRole role = CharacterRole.Warrior;
        [Min(0)] public int unlockPrice;
        [Tooltip("false = Chưa ra mắt: vẫn hiện ở sảnh (dáng tối + nhãn), xem được giới thiệu nhưng không mở khóa / không vào run.")]
        public bool released = true;
        [Tooltip("Bậc hero (S cao nhất). Dùng cho hiển thị và cân bằng giá mở khóa.")]
        public DungeonGathering.Inventory.Rarity rank = DungeonGathering.Inventory.Rarity.C;

        [Header("Hình")]
        [Tooltip("Ảnh hero cho UI (sảnh, loadout, pause); trong run làm sprite đầu và mốc canh cỡ / vị trí hình Player.")]
        public Sprite portrait;
        [Tooltip("AnimatorOverrideController (hoặc controller riêng) gắn lên Visual của Player.prefab")]
        public RuntimeAnimatorController animator;

        [Header("VFX đòn đánh")]
        [Tooltip("Phát tại vũ khí mỗi lần hero ra đòn (thêm vào VFX riêng của vũ khí). Trống = không có.")]
        public GameObject attackVfx;
        [Tooltip("Phát tại điểm trúng mỗi khi đòn của hero trúng mục tiêu. Trống = không có.")]
        public GameObject hitVfx;

        [Header("Chỉ số")]
        [Tooltip("StatProfile + ghi đè (máu, giáp, mana, sát thương, tốc chạy, tốc đánh, chí mạng, hồi chiêu, hồi mana, hồi giáp)")]
        public StatBlock stats = new();

        [Header("Khởi đầu")]
        [Tooltip("Vũ khí tặng khi mở khóa hero, gắn sẵn vào ô vũ khí 1 của loadout. Trống = không có vũ khí khởi đầu.")]
        public WeaponData startingWeapon;
        [Tooltip("Skill nhân vật (chọn trong SkillLibrary)")]
        public SkillId skill;
        [Tooltip("Nội tại bẩm sinh kích hoạt suốt run (tùy chọn).")]
        public ItemPassiveSO innatePassive;

        /// <summary>Skill nhân vật theo SkillId (null nếu chưa chọn / chưa đăng ký).</summary>
        public ActiveSkillSO ResolveSkill() => skill != SkillId.None ? SkillRegistry.Get<ActiveSkillSO>(skill) : null;

        /// <summary>Chỉ số gốc (StatProfile + ghi đè; thiếu profile → BaseStats.Default).</summary>
        public BaseStats ResolveStats() => stats != null ? stats.Resolve(BaseStats.Default) : BaseStats.Default;
        public float ResolveArmorRegenDelay() => stats != null ? stats.ArmorRegenDelay(4f) : 4f;
        public float ResolveArmorRegenRate() => stats != null ? stats.ArmorRegenRate(1f) : 1f;
    }
}
