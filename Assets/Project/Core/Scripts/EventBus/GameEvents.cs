using UnityEngine;

namespace DungeonGathering.Core
{
    public struct PlayerStatsChangedEvent : IEvent { public int Hp, MaxHp, Armor, MaxArmor, Mana, MaxMana; }
    public struct EquipmentChangedEvent : IEvent { public EquipmentSlotType Slot; public string CharacterId; }
    public struct RoomEnteredEvent : IEvent { public int RoomId; public bool IsBoss; }
    public struct RoomClearedEvent : IEvent { public int RoomId; }
    public struct BossHealthChangedEvent : IEvent { public string BossName; public int Current, Max; }
    public struct BossDefeatedEvent : IEvent { public string BossId; }
    public struct WeaponSwappedEvent : IEvent { public int ActiveSlot; }
    public struct ItemPickedUpEvent : IEvent { public string ItemId; }
    public struct ActiveSkillUsedEvent : IEvent { public int SlotIndex; public float Cooldown; }
    public struct RunFinishedEvent : IEvent { public bool Victory; public int Floor; }
    public struct RunCoinsChangedEvent : IEvent { public int Coins; }
    public struct WalletChangedEvent : IEvent { public int Coins; }
    public struct GameStateChangedEvent : IEvent { public GameState Previous, Current; }
    public struct PlayerDiedEvent : IEvent { public Vector3 Position; }
}
