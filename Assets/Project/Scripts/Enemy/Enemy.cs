using System;
using System.Threading;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// Gốc của prefab quái (ENTITIES §3.1). Điều phối Health / Motor / Brain / AttackRunner, vòng đời pool.
    /// Luồng: Pool.Get → OnSpawn (reset) → Init(ctx) → … → Health về 0 → HandleDeath (một lần) → DeadState → ReturnAfter.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(EnemyHealth), typeof(EnemyMotor), typeof(EnemyBrain))]
    [RequireComponent(typeof(EnemyAttackRunner))]
    [DisallowMultipleComponent]
    public sealed class Enemy : MonoBehaviour, IPoolable
    {
        [SerializeField] EnemyData data;
        [SerializeField] Transform attackOrigin;
        [SerializeField] SpriteAnimDriver anim;
        [SerializeField] Collider2D[] disableOnDeath;
        [SerializeField] bool isBoss;

        CancellationTokenSource lifeCts;
        EnemyData defaultData;   // data gán trên prefab — Init không override thì quay về data này (pool dùng lại)
        int lifeId;              // tăng mỗi lần spawn: callback từ đời trước (quái con) không ảnh hưởng đời mới
        bool initialized;
        float contactTimer;
        int summonCount;

        public EnemyData Data => data;
        public EnemyHealth Health { get; private set; }
        public EnemyMotor Motor { get; private set; }
        public EnemyBrain Brain { get; private set; }
        public EnemyAttackRunner Attacks { get; private set; }
        public StatusEffectController Status { get; private set; }
        public SpriteAnimDriver Anim => anim;
        public Transform AttackOrigin => attackOrigin != null ? attackOrigin : transform;
        public EnemySpawnContext Context { get; private set; } = EnemySpawnContext.Empty;
        public bool IsDead => Health.IsDead;
        public bool IsBoss => isBoss;
        public int SummonCount => summonCount;
        public CancellationToken LifeToken => lifeCts?.Token ?? CancellationToken.None;

        /// <summary>Phát một lần khi chết (RoomController bỏ khỏi alive).</summary>
        public event Action<Enemy> Died;

        void Awake()
        {
            defaultData = data;
            Health = GetComponent<EnemyHealth>();
            Motor = GetComponent<EnemyMotor>();
            Brain = GetComponent<EnemyBrain>();
            Attacks = GetComponent<EnemyAttackRunner>();
            Status = GetComponent<StatusEffectController>();
            if (anim == null) anim = GetComponentInChildren<SpriteAnimDriver>();
            if (disableOnDeath == null || disableOnDeath.Length == 0) disableOnDeath = GetComponentsInChildren<Collider2D>(true);
        }

        void Start()
        {
            // Quái đặt sẵn trong scene (test) — không qua spawner.
            if (!initialized && GetComponent<PooledObject>() == null)
            {
                OnSpawn();
                Init(EnemySpawnContext.Empty);
            }
        }

        public void OnSpawn()
        {
            lifeCts?.Cancel();
            lifeCts?.Dispose();
            lifeCts = new CancellationTokenSource();
            lifeId++;
            initialized = false;
            summonCount = 0;
            contactTimer = 0f;
            Died = null;
            foreach (var c in disableOnDeath) if (c != null) c.enabled = true;
            if (anim != null) anim.ResetState();
        }

        /// <summary>Gọi ngay sau Pool.Get. dataOverride cho phép một prefab dùng nhiều EnemyData.</summary>
        public void Init(EnemySpawnContext ctx, EnemyData dataOverride = null)
        {
            // Override chỉ áp cho lần spawn này; không override → về data gốc của prefab (tránh dính override cũ khi pool dùng lại).
            data = dataOverride != null ? dataOverride : defaultData;
            if (data == null)
            {
                Debug.LogError($"[Enemy] {name} thiếu EnemyData.", this);
                return;
            }
            Context = ctx ?? EnemySpawnContext.Empty;
            Health.ResetHealth(data.maxHp * Mathf.Max(0.1f, Context.HealthMultiplier));
            Motor.ResetMotor();
            Attacks.SetAttacks(data.attacks);
            Brain.Begin();
            initialized = true;
        }

        /// <summary>Gọi bởi EnemyHealth đúng một lần (isDead đã đặt trước).</summary>
        internal void HandleDeath()
        {
            lifeCts?.Cancel(); // hủy windup đang chờ — không tung đòn sau khi chết
            Status?.ClearAll();
            Motor.Stop();
            foreach (var c in disableOnDeath) if (c != null) c.enabled = false;
            Brain.EnterDead();
            anim?.Play(AnimKey.Death, true);

            var died = Died;
            Died = null;
            try { died?.Invoke(this); } catch (Exception e) { Debug.LogException(e); }
            EventBus<EnemyDiedEvent>.Raise(new EnemyDiedEvent { Enemy = this, Data = data, Position = transform.position, IsBoss = isBoss });

            float delay = Mathf.Max(0.1f, data != null ? data.deathDuration : 0.8f);
            if (GetComponent<PooledObject>() != null) Services.Pool.ReturnAfter(gameObject, delay);
            else Destroy(gameObject, delay);
        }

        /// <summary>Dọn ngay (phòng/run kết thúc): chết không rơi loot không cần — gọi Health.Kill thay vì Despawn nếu muốn loot.</summary>
        public void DespawnImmediate()
        {
            lifeCts?.Cancel();
            Died = null;
            if (GetComponent<PooledObject>() != null) Services.Pool.Return(gameObject);
            else Destroy(gameObject);
        }

        public void SpawnMinion(Enemy prefab, Vector2 position)
        {
            if (prefab == null || Context.Summon == null || IsDead) return;
            var minion = Context.Summon(prefab, position);
            if (minion == null) return;
            summonCount++;
            int life = lifeId;
            minion.Died += _ =>
            {
                // Con của đời trước (summoner đã chết rồi được pool dùng lại) không được trừ bộ đếm của đời mới.
                if (this != null && lifeId == life) summonCount = Mathf.Max(0, summonCount - 1);
            };
        }

        void OnCollisionStay2D(Collision2D c) => TryContact(c.collider);
        void OnTriggerStay2D(Collider2D c) => TryContact(c);

        void TryContact(Collider2D col)
        {
            if (!initialized || IsDead || data == null || data.contactDamage <= 0f || Brain.IsSpawning) return;
            if (Time.time < contactTimer) return;
            var d = CombatUtil.Resolve(col);
            if (d == null || d.Team != Team.Player || d.IsDead) return;
            Vector2 dir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            if (d.TakeDamage(new DamageInfo(data.contactDamage, 3f, dir, 0f, false, gameObject, Team.Enemy)))
                contactTimer = Time.time + data.contactCooldown;
        }

        public void OnDespawn()
        {
            lifeCts?.Cancel();
            Died = null;
            initialized = false;
            Motor.Stop();
            Status?.ClearAll();
        }

        void OnDestroy()
        {
            lifeCts?.Cancel();
            lifeCts?.Dispose();
            lifeCts = null;
        }
    }
}
