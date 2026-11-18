using System;
using System.Collections.Generic;

namespace DungeonGathering.Combat
{
    /// <summary>Trạng thái dùng chung cho hero và quái. Không phải mọi actor dùng hết.</summary>
    public enum ActorStateId
    {
        Spawn = 0,      // đang xuất hiện / chưa bật điều khiển
        Idle = 1,       // quái chưa có mục tiêu
        Active = 2,     // hero: điều khiển bình thường
        Chase = 3,      // quái: đuổi / giữ khoảng cách
        Attack = 4,     // quái: đang ra đòn / skill
        Dash = 5,       // hero: đang lướt
        Stunned = 6,    // bị khống chế (choáng, đóng băng)
        Sealed = 7,     // hero: run đã chốt, chỉ đi lại
        Dead = 8,
    }

    public interface IState<in TOwner>
    {
        ActorStateId Id { get; }
        void Enter(TOwner owner);
        void Tick(TOwner owner);
        void Exit(TOwner owner);
    }

    /// <summary>Những gì trạng thái nền dùng chung (Stunned, Dead) cần biết về actor.</summary>
    public interface IStateActor
    {
        bool IsDead { get; }
        bool IsCrowdControlled { get; }
        /// <summary>Bị khống chế: dừng di chuyển, hủy đòn / skill / lướt đang dở.</summary>
        void InterruptActions();
        /// <summary>Hết khống chế thì quay về trạng thái nào.</summary>
        ActorStateId StateAfterStun { get; }
    }

    /// <summary>
    /// FSM dùng chung: trạng thái đăng ký theo Id, chuyển tường minh qua <see cref="ChangeState"/>, Dead là trạng thái cuối
    /// (chỉ <see cref="Reset"/> mới thoát). Luật toàn cục (<see cref="AddGlobalRule"/>) được xét trước mỗi Tick theo thứ tự
    /// ưu tiên — vd "chết → Dead", "bị khống chế → Stunned" — để hero và quái không phải tự lặp lại.
    /// Thuần C#: test được không cần scene.
    /// </summary>
    public sealed class StateMachine<TOwner>
    {
        readonly TOwner owner;
        readonly Dictionary<ActorStateId, IState<TOwner>> states = new();
        readonly List<(Func<TOwner, bool> when, ActorStateId to, Func<ActorStateId, bool> from)> rules = new();
        IState<TOwner> current;
        bool changing;

        public StateMachine(TOwner owner) => this.owner = owner;

        public ActorStateId? CurrentId => current?.Id;
        public IState<TOwner> Current => current;
        public bool Is(ActorStateId id) => current != null && current.Id == id;

        /// <summary>(từ, sang) — từ = null khi Reset.</summary>
        public event Action<ActorStateId?, ActorStateId> Changed;

        public StateMachine<TOwner> Add(IState<TOwner> state)
        {
            states[state.Id] = state;
            return this;
        }

        public T Get<T>(ActorStateId id) where T : class, IState<TOwner> => states.TryGetValue(id, out var s) ? s as T : null;

        /// <summary>
        /// Luật toàn cục: khi <paramref name="when"/> đúng và trạng thái hiện tại thỏa <paramref name="from"/> (null = mọi trạng
        /// thái khác đích) → chuyển sang <paramref name="to"/>. Xét theo thứ tự thêm; luật đầu tiên khớp thắng.
        /// </summary>
        public StateMachine<TOwner> AddGlobalRule(Func<TOwner, bool> when, ActorStateId to, Func<ActorStateId, bool> from = null)
        {
            rules.Add((when, to, from));
            return this;
        }

        /// <summary>Bắt đầu lại từ <paramref name="id"/> (pool / hồi sinh) — thoát được cả Dead.</summary>
        public void Reset(ActorStateId id)
        {
            current?.Exit(owner);
            current = null;
            Enter(id, null);
        }

        /// <summary>Chuyển trạng thái. Bỏ qua nếu đang Dead, đích chưa đăng ký, hoặc đang ở đúng trạng thái đó.</summary>
        public bool ChangeState(ActorStateId id)
        {
            if (current != null && current.Id == ActorStateId.Dead) return false;
            if (current != null && current.Id == id) return false;
            if (!states.ContainsKey(id)) return false;
            if (changing) return false; // Enter/Exit gọi ChangeState lồng → bỏ, tránh đệ quy
            var from = current?.Id;
            changing = true;
            try
            {
                current?.Exit(owner);
                current = null;
            }
            finally { changing = false; }
            Enter(id, from);
            return true;
        }

        void Enter(ActorStateId id, ActorStateId? from)
        {
            if (!states.TryGetValue(id, out var next)) return;
            changing = true;
            try
            {
                current = next;
                next.Enter(owner);
            }
            finally { changing = false; }
            Changed?.Invoke(from, id);
        }

        public void Tick()
        {
            if (current == null) return;
            foreach (var r in rules)
            {
                if (current.Id == r.to || current.Id == ActorStateId.Dead) continue;
                if (r.from != null && !r.from(current.Id)) continue;
                if (!r.when(owner)) continue;
                ChangeState(r.to);
                break;
            }
            current?.Tick(owner);
        }
    }

    /// <summary>Khống chế: vào thì hủy hành động đang dở; hết khống chế thì về <see cref="IStateActor.StateAfterStun"/>.</summary>
    public sealed class StunnedState<TOwner> : IState<TOwner> where TOwner : IStateActor
    {
        readonly Func<StateMachine<TOwner>> machine;
        readonly Action<TOwner> whileStunned;

        /// <param name="whileStunned">Việc mỗi frame khi đang choáng (vd Player vẫn cập nhật hướng nhìn / anim).</param>
        public StunnedState(Func<StateMachine<TOwner>> machine, Action<TOwner> whileStunned = null)
        {
            this.machine = machine;
            this.whileStunned = whileStunned;
        }

        public ActorStateId Id => ActorStateId.Stunned;
        public void Enter(TOwner owner) => owner.InterruptActions();
        public void Tick(TOwner owner)
        {
            if (owner.IsDead) return;
            if (owner.IsCrowdControlled) { whileStunned?.Invoke(owner); return; }
            machine().ChangeState(owner.StateAfterStun);
        }
        public void Exit(TOwner owner) { }
    }

    /// <summary>Trạng thái cuối. Dọn dẹp cụ thể do actor tự làm khi chết (Enemy.HandleDeath, PlayerController.OnDied).</summary>
    public sealed class DeadState<TOwner> : IState<TOwner> where TOwner : IStateActor
    {
        readonly Action<TOwner> onEnter;
        public DeadState(Action<TOwner> onEnter = null) => this.onEnter = onEnter;

        public ActorStateId Id => ActorStateId.Dead;
        public void Enter(TOwner owner) => onEnter?.Invoke(owner);
        public void Tick(TOwner owner) { }
        public void Exit(TOwner owner) { }
    }
}
