using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// FSM của quái trên khung dùng chung <see cref="StateMachine{TOwner}"/>: Spawn → Idle → Chase ⇄ Attack, Stunned
    /// (luật toàn cục: bị khống chế khi đang Idle/Chase/Attack), Dead là trạng thái cuối cho tới khi về pool (Begin → Reset).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyBrain : MonoBehaviour, ITauntableTargetAI, IStateActor
    {
        [Tooltip("Mất tầm nhìn mục tiêu bao lâu (giây) thì tiến lại gần dù đã trong preferredDistance")]
        [SerializeField] float losRecheckInterval = 0.2f;

        StateMachine<EnemyBrain> fsm;
        StateMachine<EnemyBrain> Fsm => fsm ??= Build();

        public Enemy Enemy { get; private set; }
        public ActorStateId? CurrentId => Fsm.CurrentId;
        public bool IsSpawning => Fsm.Is(ActorStateId.Spawn);
        public ITargetable OverrideTarget { get; set; }
        public ITargetable Target
        {
            get
            {
                if (OverrideTarget != null && (!(OverrideTarget is Component c) || c != null) && OverrideTarget.IsAlive)
                    return OverrideTarget;
                return CombatTargets.Player;
            }
        }
        /// <summary>Phòng đã kích hoạt → luôn đuổi Player, bỏ qua aggroRange.</summary>
        public bool AlwaysAggro { get; set; } = true;

        // ---- IStateActor ----
        public bool IsDead => Enemy != null && Enemy.IsDead;
        public bool IsCrowdControlled => Enemy != null && Enemy.Status != null && Enemy.Status.IsStunned;
        public ActorStateId StateAfterStun => ActorStateId.Chase;
        public void InterruptActions()
        {
            Enemy.Attacks.Cancel();   // choáng giữa windup → hủy đòn
            Enemy.Motor.Stop();
        }

        void Awake() => Enemy = GetComponent<Enemy>();

        StateMachine<EnemyBrain> Build()
        {
            StateMachine<EnemyBrain> m = null;
            m = new StateMachine<EnemyBrain>(this)
                .Add(new SpawnState())
                .Add(new IdleState())
                .Add(new ChaseState())
                .Add(new AttackState())
                .Add(new StunnedState<EnemyBrain>(() => m, b => b.Enemy.Motor.Stop()))
                .Add(new DeadState<EnemyBrain>(b => b.Enemy.Motor.Stop()))
                .AddGlobalRule(b => b.IsCrowdControlled, ActorStateId.Stunned,
                    from: s => s == ActorStateId.Idle || s == ActorStateId.Chase || s == ActorStateId.Attack);
            return m;
        }

        public void Begin()
        {
            OverrideTarget = null;
            AlwaysAggro = true;
            Fsm.Reset(ActorStateId.Spawn);
        }

        public bool ChangeState(ActorStateId id) => Fsm.ChangeState(id);

        /// <summary>Đòn chờ thực hiện khi vào Attack (Chase chọn).</summary>
        internal EnemyAttackSO PendingAttack;

        public void EnterDead() => Fsm.ChangeState(ActorStateId.Dead);

        void Update()
        {
            if (Enemy.Data == null) return;
            Fsm.Tick();
        }

        public float DistanceToTarget()
        {
            var t = Target;
            if (t == null || !t.IsAlive) return float.MaxValue;
            return Vector2.Distance(transform.position, t.Transform.position);
        }

        // ---------------- States ----------------

        sealed class SpawnState : IState<EnemyBrain>
        {
            float until;
            public ActorStateId Id => ActorStateId.Spawn;
            public void Enter(EnemyBrain b)
            {
                until = Time.time + b.Enemy.Data.spawnDuration;
                b.Enemy.Health.Invulnerable = true;
                b.Enemy.Motor.Stop();
                b.Enemy.Anim?.Play(AnimKey.Idle, true);
            }
            public void Tick(EnemyBrain b) { if (Time.time >= until) b.Fsm.ChangeState(ActorStateId.Idle); }
            public void Exit(EnemyBrain b) => b.Enemy.Health.Invulnerable = false;
        }

        sealed class IdleState : IState<EnemyBrain>
        {
            public ActorStateId Id => ActorStateId.Idle;
            public void Enter(EnemyBrain b) => b.Enemy.Motor.Stop();
            public void Tick(EnemyBrain b)
            {
                var t = b.Target;
                if (t == null || !t.IsAlive) return;
                if (b.AlwaysAggro || b.DistanceToTarget() <= b.Enemy.Data.aggroRange) b.Fsm.ChangeState(ActorStateId.Chase);
            }
            public void Exit(EnemyBrain b) { }
        }

        sealed class ChaseState : IState<EnemyBrain>
        {
            float nextLosCheck;
            bool hasLos = true;

            public ActorStateId Id => ActorStateId.Chase;
            public void Enter(EnemyBrain b) { nextLosCheck = 0f; hasLos = true; }
            public void Tick(EnemyBrain b)
            {
                var t = b.Target;
                if (t == null || !t.IsAlive) { b.Fsm.ChangeState(ActorStateId.Idle); return; }
                var attack = b.Enemy.Attacks.Pick(t);
                if (attack != null)
                {
                    b.PendingAttack = attack;
                    b.Fsm.ChangeState(ActorStateId.Attack);
                    return;
                }

                var data = b.Enemy.Data;
                float dist = b.DistanceToTarget();
                if (data.retreatDistance > 0f && dist < data.retreatDistance)
                {
                    b.Enemy.Motor.MoveAwayFrom(t.Transform.position, data.preferredDistance);
                    return;
                }
                // Đã trong tầm giữ khoảng cách nhưng bị tường/cột che → tiến lại gần (trước đây đứng im vô thời hạn).
                float keep = data.preferredDistance;
                if (dist <= keep + 0.1f)
                {
                    if (Time.time >= nextLosCheck)
                    {
                        nextLosCheck = Time.time + b.losRecheckInterval;
                        hasLos = b.Enemy.Attacks.HasLineOfSight(t);
                    }
                    if (!hasLos) keep = 0f;
                }
                b.Enemy.Motor.MoveTowards(t.Transform.position, keep);
            }
            public void Exit(EnemyBrain b) { }
        }

        sealed class AttackState : IState<EnemyBrain>
        {
            public ActorStateId Id => ActorStateId.Attack;
            public void Enter(EnemyBrain b)
            {
                var a = b.PendingAttack;
                b.PendingAttack = null;
                if (a != null) b.Enemy.Attacks.Run(a, b.Target);
            }
            public void Tick(EnemyBrain b)
            {
                if (!b.Enemy.Attacks.IsAttacking) b.Fsm.ChangeState(ActorStateId.Chase);
            }
            public void Exit(EnemyBrain b) { }
        }
    }
}
