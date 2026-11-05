using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    public interface IEnemyState
    {
        void Enter(EnemyBrain brain);
        void Tick(EnemyBrain brain);
        void Exit(EnemyBrain brain);
    }

    /// <summary>
    /// FSM của quái: Spawn → Idle → Chase ⇄ Attack → Dead. DeadState là trạng thái cuối cho tới khi về pool.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyBrain : MonoBehaviour
    {
        public readonly SpawnState Spawn = new();
        public readonly IdleState Idle = new();
        public readonly ChaseState Chase = new();
        public readonly AttackState Attack = new();
        public readonly DeadState Dead = new();

        IEnemyState current;
        public Enemy Enemy { get; private set; }
        public IEnemyState Current => current;
        public bool IsSpawning => current == Spawn;
        public ITargetable Target => CombatTargets.Player;
        /// <summary>Phòng đã kích hoạt → luôn đuổi Player, bỏ qua aggroRange.</summary>
        public bool AlwaysAggro { get; set; } = true;

        void Awake() => Enemy = GetComponent<Enemy>();

        public void Begin()
        {
            current = null;
            ChangeState(Spawn);
        }

        public void ChangeState(IEnemyState next)
        {
            if (current == Dead) return; // không thoát khỏi DeadState
            current?.Exit(this);
            current = next;
            current?.Enter(this);
        }

        public void EnterDead() => ChangeState(Dead);

        void Update()
        {
            if (current == null || Enemy.Data == null) return;
            current.Tick(this);
        }

        public float DistanceToTarget()
        {
            var t = Target;
            if (t == null || !t.IsAlive) return float.MaxValue;
            return Vector2.Distance(transform.position, t.Transform.position);
        }

        // ---------------- States ----------------

        public sealed class SpawnState : IEnemyState
        {
            float until;
            public void Enter(EnemyBrain b)
            {
                until = Time.time + b.Enemy.Data.spawnDuration;
                b.Enemy.Health.Invulnerable = true;
                b.Enemy.Motor.Stop();
                b.Enemy.Anim?.Play(AnimKey.Idle, true);
            }
            public void Tick(EnemyBrain b) { if (Time.time >= until) b.ChangeState(b.Idle); }
            public void Exit(EnemyBrain b) => b.Enemy.Health.Invulnerable = false;
        }

        public sealed class IdleState : IEnemyState
        {
            public void Enter(EnemyBrain b) => b.Enemy.Motor.Stop();
            public void Tick(EnemyBrain b)
            {
                var t = b.Target;
                if (t == null || !t.IsAlive) return;
                if (b.AlwaysAggro || b.DistanceToTarget() <= b.Enemy.Data.aggroRange) b.ChangeState(b.Chase);
            }
            public void Exit(EnemyBrain b) { }
        }

        public sealed class ChaseState : IEnemyState
        {
            public void Enter(EnemyBrain b) { }
            public void Tick(EnemyBrain b)
            {
                var t = b.Target;
                if (t == null || !t.IsAlive) { b.ChangeState(b.Idle); return; }
                if (b.Enemy.Status != null && b.Enemy.Status.IsStunned) { b.Enemy.Motor.Stop(); return; }
                var attack = b.Enemy.Attacks.Pick(t);
                if (attack != null)
                {
                    b.Attack.Pending = attack;
                    b.ChangeState(b.Attack);
                    return;
                }
                b.Enemy.Motor.MoveTowards(t.Transform.position, b.Enemy.Data.preferredDistance);
            }
            public void Exit(EnemyBrain b) { }
        }

        public sealed class AttackState : IEnemyState
        {
            public EnemyAttackSO Pending;
            public void Enter(EnemyBrain b)
            {
                b.Enemy.Attacks.Run(Pending, b.Target);
                Pending = null;
            }
            public void Tick(EnemyBrain b)
            {
                // Bị choáng/đóng băng giữa windup → hủy đòn (không tung đòn khi đang choáng).
                if (b.Enemy.Status != null && b.Enemy.Status.IsStunned && b.Enemy.Attacks.IsAttacking)
                {
                    b.Enemy.Attacks.Cancel();
                    b.Enemy.Motor.Stop();
                }
                if (!b.Enemy.Attacks.IsAttacking) b.ChangeState(b.Chase);
            }
            public void Exit(EnemyBrain b) { }
        }

        public sealed class DeadState : IEnemyState
        {
            public void Enter(EnemyBrain b) => b.Enemy.Motor.Stop();
            public void Tick(EnemyBrain b) { }
            public void Exit(EnemyBrain b) { }
        }
    }
}
