namespace DungeonGathering.Core
{
    /// <summary>
    /// Đổi nhạc nền theo sự kiện trong run (chỉ nghe EventBus, không đổi state game):
    /// - Vào phòng boss → nhạc boss; hạ boss → quay lại nhạc của module.
    /// - Kết thúc run → tắt dần nhạc nền (jingle thắng/thua là SFX, do GameSfxHooks phát).
    /// Nhạc theo module (menu/lobby/play) do SceneService phát từ ModuleDefinition.bgmAddress.
    /// </summary>
    sealed class GameBgmHooks
    {
        public static readonly string BossBgm = AddressKeys.Bgm("boss");

        readonly AudioManager audio;
        string resumeAddress;
        bool bound;

        public GameBgmHooks(AudioManager audio) => this.audio = audio;

        public void Bind()
        {
            if (bound) return;
            bound = true;
            EventBus<RoomEnteredEvent>.Subscribe(OnRoomEntered);
            EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
            EventBus<RunFinishedEvent>.Subscribe(OnRunFinished);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameState);
        }

        public void Unbind()
        {
            if (!bound) return;
            bound = false;
            EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEntered);
            EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);
            EventBus<RunFinishedEvent>.Unsubscribe(OnRunFinished);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameState);
        }

        void OnRoomEntered(RoomEnteredEvent e)
        {
            if (!e.IsBoss || audio == null || audio.CurrentBgm == BossBgm) return;
            resumeAddress = audio.CurrentBgm;
            audio.PlayBgm(BossBgm, 1.2f);
        }

        void OnBossDefeated(BossDefeatedEvent e)
        {
            if (audio == null || audio.CurrentBgm != BossBgm) return;
            if (!string.IsNullOrEmpty(resumeAddress)) audio.PlayBgm(resumeAddress, 1.5f);
            resumeAddress = null;
        }

        void OnRunFinished(RunFinishedEvent e)
        {
            resumeAddress = null;
            audio?.StopBgm(0.8f);
        }

        // Rời run (về sảnh/menu) mà chưa hạ boss: SceneService tự đổi nhạc, chỉ cần quên trạng thái boss.
        void OnGameState(GameStateChangedEvent e)
        {
            if (e.Current != GameState.Playing && e.Current != GameState.Paused) resumeAddress = null;
        }
    }
}
