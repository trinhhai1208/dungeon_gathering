namespace DungeonGathering.Core
{
    /// <summary>
    /// Phát SFX theo sự kiện EventBus của Core — gameplay không cần biết tới âm thanh.
    /// Chỉ nghe (không Raise, không đổi state), handler cực nhẹ; EventBus đã bọc try/catch từng handler.
    /// Âm gắn hành động cụ thể (đánh, trúng, nhặt) được gọi trực tiếp bằng <see cref="Sfx"/> tại chỗ.
    /// </summary>
    sealed class GameSfxHooks
    {
        bool bound;

        public void Bind()
        {
            if (bound) return;
            bound = true;
            EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
            EventBus<RoomClearedEvent>.Subscribe(OnRoomCleared);
            EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
            EventBus<WeaponSwappedEvent>.Subscribe(OnWeaponSwapped);
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<RunFinishedEvent>.Subscribe(OnRunFinished);
        }

        public void Unbind()
        {
            if (!bound) return;
            bound = false;
            EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
            EventBus<RoomClearedEvent>.Unsubscribe(OnRoomCleared);
            EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);
            EventBus<WeaponSwappedEvent>.Unsubscribe(OnWeaponSwapped);
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<RunFinishedEvent>.Unsubscribe(OnRunFinished);
        }

        static void OnRoomEntered(RoomEnteredEvent e)
        {
            Sfx.Play(SfxId.DoorClose);
            if (e.IsBoss) Sfx.Play(SfxId.BossAppear);
        }

        static void OnRoomCleared(RoomClearedEvent e)
        {
            Sfx.Play(SfxId.DoorOpen);
            Sfx.Play(SfxId.RoomCleared);
        }

        static void OnBossDefeated(BossDefeatedEvent e)
        {
            Sfx.Play(SfxId.BossRoar);
            Sfx.Play(SfxId.Explosion);
        }

        static void OnWeaponSwapped(WeaponSwappedEvent e) => Sfx.Play(SfxId.WeaponSwap);
        static void OnPlayerDied(PlayerDiedEvent e) => Sfx.Play(SfxId.PlayerDeath);
        static void OnRunFinished(RunFinishedEvent e) => Sfx.Play(e.Victory ? SfxId.Victory : SfxId.Defeat);
    }
}
