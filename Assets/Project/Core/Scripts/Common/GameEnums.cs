namespace DungeonGathering.Core
{
    /// <summary>6 ô trang bị (ENTITIES §5.2). Thứ tự = chỉ số trong LoadoutData.equipment.</summary>
    public enum EquipmentSlotType { Helmet, Armor, Gloves, Boots, Necklace, Accessory }

    public enum StatType
    {
        MaxHp,
        MaxArmor,
        MaxMana,
        AttackDamage,
        AttackSpeed,
        MoveSpeed,
        CritChance,
        CritDamage,
        CooldownReduction,
        ManaRegen,
        // Thêm mới luôn nối vào CUỐI (enum lưu dạng số trong asset).
        /// <summary>Giảm đẩy lùi nhận vào (0 = bị đẩy đủ, 1 = miễn).</summary>
        KnockbackResist,
    }

    public enum GameState { Boot, Menu, Lobby, Playing, Paused, Victory, GameOver }

    /// <summary>Bậc quái: quyết định hệ số chỉ số (MonsterTierConfigSO) và bảng rớt đồ.</summary>
    public enum MonsterTier { Normal = 0, Elite = 1, Boss = 2 }

    /// <summary>Phe trong chiến đấu: đạn/bẫy dùng để bỏ qua đồng minh.</summary>
    public enum Team { Player, Enemy, Neutral }
}
