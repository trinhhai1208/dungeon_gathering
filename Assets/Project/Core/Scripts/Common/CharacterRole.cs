namespace DungeonGathering.Core
{
    /// <summary>
    /// Phân loại Role của nhân vật trong Dungeon Gathering.
    /// Quyết định hiệu ứng Role Synergy khi mặc trang bị EquipmentData.
    /// Default = Any (0) để đảm bảo an toàn cho các trường chưa gán giá trị.
    /// </summary>
    public enum CharacterRole
    {
        Any = 0,        // Thích hợp mọi Role (mặc định cho trang bị phổ thông)
        Warrior = 1,    // Chiến binh / Kỵ sĩ: Kiếm cận chiến, giáp dày, đẩy lùi, càn quét
        Ranger = 2,     // Xạ thủ: Súng, đạn chùm, plasma, tầm xa, xuyên thấu
        Mage = 3,       // Pháp sư: Gậy phép, sách thần chú, năng lượng, nguyên tố
        Assassin = 4    // Sát thủ / Đạo tặc: Dao găm, liễu kiếm, bạo kích, độc dược, cơ động
    }
}
