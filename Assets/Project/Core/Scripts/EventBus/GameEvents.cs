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
    public struct CharacterPanelChangedEvent : IEvent { public bool Open; }
    /// <summary>
    /// SaveSystem vừa chuyển sang CanWrite=false (không ghi save nữa). NewerSaveVersion=false: tab khác giữ khóa (WebGL);
    /// true: file save của bản game mới hơn. Không sticky — UI mở muộn đọc <c>Services.Save.CanWrite</c>.
    /// </summary>
    public struct SaveLockLostEvent : IEvent { public bool NewerSaveVersion; }

    /// <summary>Portal đi đâu: sang level kế (qua màn kết quả) hay về sảnh.</summary>
    public enum LevelExitDestination { NextLevel = 0, Lobby = 1 }

    /// <summary>Player bước vào Portal trong level. LevelDirector nghe và quyết định (Portal không gọi thẳng code khác).</summary>
    public struct LevelExitRequestedEvent : IEvent { public LevelExitDestination Destination; }

    /// <summary>Portal ở sảnh được bấm. LobbyEntry nghe, thử vào run, gọi <see cref="Done"/>(thành công?).</summary>
    public struct RunStartRequestedEvent : IEvent { public System.Action<bool> Done; }
}
