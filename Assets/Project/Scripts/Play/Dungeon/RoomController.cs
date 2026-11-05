using System;
using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using UnityEngine;

namespace DungeonGathering.Play
{
    public enum RoomState { Idle, Active, Cleared }

    /// <summary>
    /// Điều khiển một phòng (ARCHITECTURE §2). Trạng thái một chiều Idle → Active → Cleared.
    /// Đếm quái bằng tập: alive (HashSet) + pendingSpawns + WavesRemaining — chỉ dọn khi cả ba rỗng/0 (R12).
    /// Là chủ duy nhất của RoomClearedEvent (một lần mỗi phòng).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomController : MonoBehaviour
    {
        static readonly List<RoomController> all = new();
        public static IReadOnlyList<RoomController> All => all;

        readonly HashSet<Enemy> alive = new();
        readonly List<RoomDoor> doors = new();
        int pendingSpawns;
        EnemySpawner spawner;
        LevelDirector director;
        Bounds bounds;

        public RoomNode Node { get; private set; }
        public RoomTemplate Template { get; private set; }
        public RoomState State { get; private set; } = RoomState.Idle;
        public int RoomId => Node?.Id ?? -1;
        public RoomType Type => Node?.Type ?? RoomType.Combat;
        public bool IsCleared => State == RoomState.Cleared;
        public bool Visited { get; private set; }
        public bool WavesRemaining { get; internal set; }
        public int AliveCount => alive.Count;
        public int PendingSpawns => pendingSpawns;
        public IReadOnlyCollection<Enemy> Alive => alive;
        public Bounds Bounds => bounds;
        public Collider2D CameraBounds { get; internal set; }
        public LevelDirector Director => director;

        public event Action<RoomController> Cleared;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => all.Clear();

        public static RoomController FindAt(Vector2 world)
        {
            foreach (var r in all)
                if (r != null && r.bounds.Contains(new Vector3(world.x, world.y, r.bounds.center.z))) return r;
            return null;
        }

        public void Setup(RoomNode node, RoomTemplate template, LevelDirector levelDirector, IEnumerable<RoomDoor> roomDoors)
        {
            Node = node;
            Template = template;
            director = levelDirector;
            bounds = template.WorldBounds;
            doors.Clear();
            if (roomDoors != null) doors.AddRange(roomDoors);
            foreach (var d in doors) d.SetClosed(false, true);

            // Phòng không chiến đấu coi như đã dọn từ đầu (không phát sự kiện).
            if (node.Type is RoomType.Start or RoomType.Treasure or RoomType.Shop) State = RoomState.Cleared;
        }

        /// <summary>RoomTriggerRelay gọi khi collider Player vào vùng trong phòng (có thể gọi nhiều lần).</summary>
        internal void OnPlayerEntered()
        {
            if (director == null || !director.IsPlaying) return;
            if (!Visited) Visited = true;
            director.OnRoomEntered(this);
            if (State != RoomState.Idle) return; // chỉ kích hoạt khi Idle
            if (Type is RoomType.Combat or RoomType.Boss) Activate();
        }

        void Activate()
        {
            State = RoomState.Active;
            foreach (var d in doors) d.SetClosed(true);
            EventBus<RoomEnteredEvent>.Raise(new RoomEnteredEvent { RoomId = RoomId, IsBoss = Type == RoomType.Boss });

            if (Type == RoomType.Boss)
            {
                WavesRemaining = true; // giữ phòng mở tới khi boss xuất hiện
                director.SpawnBoss(this);
                return;
            }
            spawner = new EnemySpawner(this, director.Config, director.Rng, director.Prefabs, director.Scope);
            spawner.Run();
        }

        /// <summary>Điểm có nằm trên NavMesh của quái không (chưa có NavMesh → true).</summary>
        public bool IsWalkable(Vector2 world) => EnemyNavigation.IsWalkable(world);

        public EnemySpawnContext CreateSpawnContext() => new()
        {
            RoomId = RoomId,
            HealthMultiplier = director != null ? director.EnemyHealthMultiplier : 1f,
            Summon = SummonMinion,
        };

        Enemy SummonMinion(Enemy prefab, Vector2 position)
        {
            if (State != RoomState.Active || director == null || !director.IsPlaying) return null;
            if (!EnemyNavigation.TrySnap(position, 2f, out position)) position = Template.transform.position;
            var go = Services.Pool.Get(prefab.gameObject, director.Scope, position, Quaternion.identity);
            if (go == null) return null;
            var e = go.GetComponent<Enemy>();
            e.Init(CreateSpawnContext());
            Register(e);
            return e;
        }

        // ---- Đếm quái (gọi bởi EnemySpawner) ----

        internal void BeginPendingSpawn() => pendingSpawns++;

        /// <summary>Quái thật xuất hiện: pending-- và alive.Add trong CÙNG một hàm.</summary>
        internal void CompletePendingSpawn(Enemy e)
        {
            pendingSpawns = Mathf.Max(0, pendingSpawns - 1);
            if (e != null) Register(e);
            else CheckCleared();
        }

        internal void CancelPendingSpawn()
        {
            pendingSpawns = Mathf.Max(0, pendingSpawns - 1);
            CheckCleared();
        }

        public void Register(Enemy e)
        {
            if (e == null || e.IsDead || !alive.Add(e)) return;
            e.Died += OnEnemyDied;
        }

        void OnEnemyDied(Enemy e)
        {
            if (!alive.Remove(e)) return; // báo chết 2 lần → bỏ qua
            CheckCleared();
        }

        internal void CheckCleared()
        {
            if (State != RoomState.Active || director == null || !director.IsPlaying) return;
            if (alive.Count > 0 || pendingSpawns > 0 || WavesRemaining) return;
            Clear();
        }

        void Clear()
        {
            if (State == RoomState.Cleared) return;
            State = RoomState.Cleared; // chỉ vào một lần
            foreach (var d in doors) d.SetClosed(false);
            spawner?.Stop();
            try { Cleared?.Invoke(this); } catch (Exception ex) { Debug.LogException(ex); }
            EventBus<RoomClearedEvent>.Raise(new RoomClearedEvent { RoomId = RoomId });
            director?.OnRoomCleared(this);
        }

        /// <summary>Run kết thúc: dừng spawner, hủy marker; không mở cửa / không phát sự kiện.</summary>
        public void Abort()
        {
            spawner?.Stop();
            WavesRemaining = false;
        }

        /// <summary>Giết hết quái còn sống (boss chết → dọn quái con).</summary>
        public void KillAll()
        {
            WavesRemaining = false;
            spawner?.Stop();
            foreach (var e in new List<Enemy>(alive)) if (e != null && !e.IsDead) e.Health.Kill();
        }

        void OnDestroy()
        {
            spawner?.Stop();
            foreach (var e in alive) if (e != null) e.Died -= OnEnemyDied;
            alive.Clear();
        }
    }
}
