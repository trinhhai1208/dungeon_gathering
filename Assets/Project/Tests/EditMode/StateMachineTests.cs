using System.Collections.Generic;
using DungeonGathering.Combat;
using NUnit.Framework;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 3: StateMachine dùng chung — chuyển tường minh, Dead là cuối, luật toàn cục, Stunned.</summary>
    public class StateMachineTests
    {
        sealed class Actor : IStateActor
        {
            public bool Dead, Cc;
            public int Interrupts;
            public readonly List<string> Log = new();
            public bool IsDead => Dead;
            public bool IsCrowdControlled => Cc;
            public void InterruptActions() => Interrupts++;
            public ActorStateId StateAfterStun => ActorStateId.Chase;
        }

        sealed class LogState : IState<Actor>
        {
            public LogState(ActorStateId id) => Id = id;
            public ActorStateId Id { get; }
            public int Ticks;
            public void Enter(Actor a) => a.Log.Add("enter " + Id);
            public void Tick(Actor a) => Ticks++;
            public void Exit(Actor a) => a.Log.Add("exit " + Id);
        }

        static StateMachine<Actor> Build(Actor a)
        {
            StateMachine<Actor> m = null;
            m = new StateMachine<Actor>(a)
                .Add(new LogState(ActorStateId.Spawn))
                .Add(new LogState(ActorStateId.Chase))
                .Add(new LogState(ActorStateId.Attack))
                .Add(new StunnedState<Actor>(() => m))
                .Add(new DeadState<Actor>())
                .AddGlobalRule(x => x.IsDead, ActorStateId.Dead)
                .AddGlobalRule(x => x.IsCrowdControlled, ActorStateId.Stunned,
                    from: s => s == ActorStateId.Chase || s == ActorStateId.Attack);
            return m;
        }

        [Test]
        public void ChangeState_CallsExitThenEnter()
        {
            var a = new Actor();
            var m = Build(a);
            m.Reset(ActorStateId.Spawn);
            m.ChangeState(ActorStateId.Chase);
            CollectionAssert.AreEqual(new[] { "enter Spawn", "exit Spawn", "enter Chase" }, a.Log);
            Assert.IsFalse(m.ChangeState(ActorStateId.Chase), "Đổi sang chính nó → bỏ qua");
        }

        [Test]
        public void Dead_IsTerminal_UntilReset()
        {
            var a = new Actor();
            var m = Build(a);
            m.Reset(ActorStateId.Chase);
            a.Dead = true;
            m.Tick();
            Assert.IsTrue(m.Is(ActorStateId.Dead));
            Assert.IsFalse(m.ChangeState(ActorStateId.Chase));
            a.Dead = false;
            m.Reset(ActorStateId.Spawn);   // về pool, sinh lại
            Assert.IsTrue(m.Is(ActorStateId.Spawn));
        }

        [Test]
        public void CrowdControl_InterruptsAndReturns()
        {
            var a = new Actor();
            var m = Build(a);
            m.Reset(ActorStateId.Attack);
            a.Cc = true;
            m.Tick();
            Assert.IsTrue(m.Is(ActorStateId.Stunned));
            Assert.AreEqual(1, a.Interrupts, "Vào Stunned phải hủy hành động đang dở");
            m.Tick();
            Assert.IsTrue(m.Is(ActorStateId.Stunned), "Còn khống chế thì ở lại");
            a.Cc = false;
            m.Tick();
            Assert.IsTrue(m.Is(ActorStateId.Chase), "Hết khống chế → StateAfterStun");
        }

        [Test]
        public void GlobalRule_RespectsFromFilter()
        {
            var a = new Actor { Cc = true };
            var m = Build(a);
            m.Reset(ActorStateId.Spawn);
            m.Tick();
            Assert.IsTrue(m.Is(ActorStateId.Spawn), "Đang Spawn (bất tử xuất hiện) thì không bị luật Stunned kéo đi");
        }

        [Test]
        public void GlobalRule_DeadBeatsStun()
        {
            var a = new Actor { Cc = true, Dead = true };
            var m = Build(a);
            m.Reset(ActorStateId.Chase);
            m.Tick();
            Assert.IsTrue(m.Is(ActorStateId.Dead));
        }

        [Test]
        public void Changed_EventReportsTransition()
        {
            var a = new Actor();
            var m = Build(a);
            ActorStateId? from = null; ActorStateId to = ActorStateId.Spawn;
            m.Changed += (f, t) => { from = f; to = t; };
            m.Reset(ActorStateId.Spawn);
            m.ChangeState(ActorStateId.Chase);
            Assert.AreEqual(ActorStateId.Spawn, from);
            Assert.AreEqual(ActorStateId.Chase, to);
        }
    }
}
