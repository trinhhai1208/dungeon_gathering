namespace DungeonGathering.Play
{
    /// <summary>
    /// State của Player (ENTITIES §3.5.1). Cùng khuôn Enter/Tick/Exit với IEnemyState để quản lý thống nhất.
    /// Hành vi mỗi frame gom vào <see cref="PlayerController.DriveFrame"/>; state chỉ quyết định (đọc input, cho đánh).
    /// Chuyển state do PlayerController.ResolveState suy ra từ cờ + điều kiện mỗi frame (không desync với PlayerStats/Movement).
    /// </summary>
    public enum PlayerStateId { Spawn, Active, Dash, Stunned, Sealed, Dead }

    public interface IPlayerState
    {
        PlayerStateId Id { get; }
        void Enter(PlayerController p);
        void Tick(PlayerController p);
        void Exit(PlayerController p);
    }

    /// <summary>Trước khi bật input (hoặc input bị tắt): đứng yên, không đánh.</summary>
    public sealed class PlayerSpawnState : IPlayerState
    {
        public PlayerStateId Id => PlayerStateId.Spawn;
        public void Enter(PlayerController p) { }
        public void Tick(PlayerController p) => p.DriveFrame(readInput: false, allowAttack: false);
        public void Exit(PlayerController p) { }
    }

    /// <summary>Chơi bình thường: di chuyển, ngắm, đánh (kỹ năng/đổi vũ khí/tương tác qua sự kiện input).</summary>
    public sealed class PlayerActiveState : IPlayerState
    {
        public PlayerStateId Id => PlayerStateId.Active;
        public void Enter(PlayerController p) { }
        public void Tick(PlayerController p) => p.DriveFrame(readInput: true, allowAttack: true);
        public void Exit(PlayerController p) { }
    }

    /// <summary>Đang lướt: khóa hướng + bất tử do PlayerMovement/PlayerStats lo; vẫn ngắm/đánh như Active.</summary>
    public sealed class PlayerDashState : IPlayerState
    {
        public PlayerStateId Id => PlayerStateId.Dash;
        public void Enter(PlayerController p) { }
        public void Tick(PlayerController p) => p.DriveFrame(readInput: true, allowAttack: true);
        public void Exit(PlayerController p) { }
    }

    /// <summary>Bị choáng: DriveFrame tự chặn di chuyển + đánh khi StatusEffectController.IsStunned.</summary>
    public sealed class PlayerStunnedState : IPlayerState
    {
        public PlayerStateId Id => PlayerStateId.Stunned;
        public void Enter(PlayerController p) { }
        public void Tick(PlayerController p) => p.DriveFrame(readInput: true, allowAttack: true);
        public void Exit(PlayerController p) { }
    }

    /// <summary>Run đã chốt thắng (combat tắt): đi lại tới Portal, không đánh/kỹ năng.</summary>
    public sealed class PlayerSealedState : IPlayerState
    {
        public PlayerStateId Id => PlayerStateId.Sealed;
        public void Enter(PlayerController p) { }
        public void Tick(PlayerController p) => p.DriveFrame(readInput: true, allowAttack: false);
        public void Exit(PlayerController p) { }
    }

    /// <summary>Chết: không làm gì (teardown do PlayerController.OnDied). Trạng thái cuối tới khi hủy scene.</summary>
    public sealed class PlayerDeadState : IPlayerState
    {
        public PlayerStateId Id => PlayerStateId.Dead;
        public void Enter(PlayerController p) { }
        public void Tick(PlayerController p) { }
        public void Exit(PlayerController p) { }
    }
}
