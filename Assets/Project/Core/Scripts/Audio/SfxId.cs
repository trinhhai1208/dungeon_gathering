namespace DungeonGathering.Core
{
    /// <summary>
    /// Định danh hiệu ứng âm thanh. Giá trị số được serialize trong SfxLibrary / prefab / SO
    /// → chỉ THÊM mới ở cuối nhóm với số chưa dùng, không đổi số đã có.
    /// Tên enum khớp tên file: sfx_&lt;nhóm&gt;_&lt;snake_case&gt;[_NN].wav (VD: UiClick ↔ sfx_ui_ui_click.wav).
    /// </summary>
    public enum SfxId
    {
        None = 0,

        // Combat
        SwordSwing = 1,
        SwordHit = 2,
        CritHit = 3,
        BowShot = 4,
        GunShot = 5,
        MagicShot = 6,
        EnemyShot = 7,
        ProjectileHit = 8,
        Explosion = 9,
        PlayerHurt = 10,
        PlayerDeath = 11,
        EnemyHurt = 12,
        EnemyDeath = 13,
        BossRoar = 14,
        BossAppear = 15,
        WeaponSwap = 16,

        // Skill
        Dash = 30,
        Shield = 31,
        Heal = 32,
        Buff = 33,
        SkillBurst = 34,

        // Loot
        CoinPickup = 50,
        ManaPickup = 51,
        PotionDrink = 52,
        ItemPickup = 53,
        RareItemPickup = 54,
        ChestOpen = 55,

        // World
        DoorOpen = 70,
        DoorClose = 71,
        PortalEnter = 72,
        SpawnWarning = 73,
        TrapTrigger = 74,
        Footstep = 75,

        // Jingle
        RoomCleared = 90,
        Victory = 91,
        Defeat = 92,
        LevelUp = 93,

        // UI
        UiClick = 110,
        UiConfirm = 111,
        UiBack = 112,
        UiError = 113,
        UiPurchase = 114,
        UiPanelOpen = 115,
        UiPanelClose = 116,
    }
}
