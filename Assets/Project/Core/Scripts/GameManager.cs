using UnityEngine;

namespace DungeonGathering.Core
{
    /// <summary>State machine toàn cục: Menu / Lobby / Playing / Paused / Victory / GameOver.</summary>
    public sealed class GameManager
    {
        public GameState State { get; private set; } = GameState.Boot;
        GameState beforePause = GameState.Playing;

        public bool IsPaused => State == GameState.Paused;

        public void SetState(GameState next)
        {
            if (next == State) return;
            var prev = State;
            State = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent { Previous = prev, Current = next });
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            beforePause = State;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            SetState(beforePause);
        }
    }
}
