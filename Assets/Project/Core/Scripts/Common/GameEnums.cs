namespace DungeonGathering.Core
{
    /// <summary>6 ô trang bị (ENTITIES §5.2). Thứ tự = chỉ số trong LoadoutData.equipment.</summary>
    public enum EquipmentSlotType { Helmet, Armor, Gloves, Boots, Necklace, Accessory }

    public enum StatType { MaxHp, MaxArmor, MaxMana, AttackDamage, AttackSpeed, MoveSpeed, CritChance }

    public enum GameState { Boot, Menu, Lobby, Playing, Paused, Victory, GameOver }

    /// <summary>Phe trong chiến đấu: đạn/bẫy dùng để bỏ qua đồng minh.</summary>
    public enum Team { Player, Enemy, Neutral }
}
