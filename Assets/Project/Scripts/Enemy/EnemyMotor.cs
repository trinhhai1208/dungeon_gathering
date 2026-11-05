using DungeonGathering.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonGathering.Enemies
{
    /// <summary>
    /// Di chuyển quái: đi bộ theo NavMeshAgent (AI Navigation) hoặc bay thẳng; đẩy lùi; lao (charge).
    /// NavMeshAgent chỉ dùng để tìm đường + né nhau (updatePosition = false) — vị trí vẫn do Rigidbody2D quyết định,
    /// nên va chạm tường, knockback và charge giữ nguyên hành vi vật lý.
    /// Thiếu NavMesh (chưa nướng / ngoài lưới) → đi thẳng về mục tiêu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class EnemyMotor : MonoBehaviour
    {
        [SerializeField] Vector2 repathInterval = new(0.25f, 0.4f);
        [SerializeField] float knockbackDecay = 12f;
        [Tooltip("Khoảng cách tối đa để gắn quái vào NavMesh lúc spawn")]
        [SerializeField] float navSnapDistance = 1.5f;

        Rigidbody2D rb;
        Enemy enemy;
        StatusEffectController status;
        NavMeshAgent agent;

        Vector2 desired;
        Vector2 knock;
        Vector2? overrideVelocity;
        float repathTimer;
        int wallContacts;
        int wallMask;

        /// <summary>Hệ số tốc độ ngoài (phase Boss).</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public bool HitWallLastStep => wallContacts > 0;
        public Vector2 Velocity => rb.linearVelocity;

        bool AgentReady => agent != null && agent.enabled && agent.isOnNavMesh;
        bool Flying => enemy != null && enemy.Data != null && enemy.Data.isFlying;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            enemy = GetComponent<Enemy>();
            status = GetComponent<StatusEffectController>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            wallMask = CombatLayers.Walls;
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Configure(agent);
                agent.enabled = false; // chỉ bật trong AttachAgent khi chắc chắn đứng trên NavMesh
            }
        }

        void OnDisable()
        {
            // Pool tắt object: tắt agent để lần bật lại không tự gắn ở vị trí cũ (cảnh báo "not close enough to the NavMesh").
            if (agent != null) agent.enabled = false;
        }

        static void Configure(NavMeshAgent a)
        {
            a.agentTypeID = EnemyNavigation.AgentTypeId;
            a.updatePosition = false;
            a.updateRotation = false;
            a.updateUpAxis = false;
            a.radius = EnemyNavigation.AgentRadius;
            a.height = EnemyNavigation.AgentHeight;
            a.baseOffset = 0f;
            a.acceleration = 1000f; // hướng đổi ngay, gia tốc thật do Rigidbody2D
            a.angularSpeed = 0f;
            a.autoBraking = false;
            a.autoRepath = true;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        }

        /// <summary>Gắn agent vào NavMesh tại vị trí hiện tại (gọi khi spawn). Quái bay hoặc chưa có NavMesh → không dùng agent.</summary>
        void AttachAgent()
        {
            if (Flying || !EnemyNavigation.Ready ||
                !NavMesh.SamplePosition(EnemyNavigation.ToNav(rb.position), out var hit, navSnapDistance, NavMesh.AllAreas))
            {
                if (agent != null) agent.enabled = false;
                return;
            }
            if (agent == null)
            {
                // Prefab cũ chưa có agent: thêm lúc đầu tiên đứng trên NavMesh (OnEnable của agent gắn được ngay).
                agent = gameObject.AddComponent<NavMeshAgent>();
                Configure(agent);
            }
            else agent.enabled = true;
            agent.Warp(hit.position);
        }

        public void ResetMotor()
        {
            desired = Vector2.zero;
            knock = Vector2.zero;
            overrideVelocity = null;
            repathTimer = 0f;
            wallContacts = 0;
            SpeedMultiplier = 1f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            AttachAgent();
        }

        public void Stop()
        {
            desired = Vector2.zero;
            overrideVelocity = null;
            if (AgentReady && agent.hasPath) agent.ResetPath();
            repathTimer = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        /// <summary>Ghi đè vận tốc cho bước vật lý kế tiếp (charge).</summary>
        public void SetVelocity(Vector2 v) => overrideVelocity = v;

        public void Knockback(Vector2 impulse) => knock += impulse;

        /// <summary>Đi về phía target, dừng khi cách stopDistance.</summary>
        public void MoveTowards(Vector2 target, float stopDistance)
        {
            Vector2 pos = rb.position;
            Vector2 to = target - pos;
            if (to.sqrMagnitude <= stopDistance * stopDistance)
            {
                desired = Vector2.zero;
                if (AgentReady && agent.hasPath) agent.ResetPath();
                return;
            }

            if (Flying || !AgentReady)
            {
                desired = to.normalized;
                return;
            }

            // Đồng bộ vị trí mô phỏng của agent với Rigidbody2D (đã bị knockback/charge/va chạm dịch đi).
            agent.nextPosition = EnemyNavigation.ToNav(pos);
            agent.speed = enemy.Data.moveSpeed * SpeedMultiplier;

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f || (!agent.hasPath && !agent.pathPending))
            {
                repathTimer = Random.Range(repathInterval.x, repathInterval.y);
                agent.SetDestination(EnemyNavigation.ToNav(target));
            }

            Vector3 dv = agent.desiredVelocity;
            var dir = new Vector2(dv.x, dv.y);
            if (dir.sqrMagnitude > 0.0001f) desired = dir.normalized;
            else desired = agent.pathPending ? to.normalized : Vector2.zero; // chờ đường đầu tiên: đi thẳng một nhịp
        }

        void FixedUpdate()
        {
            if (enemy == null || enemy.Data == null) return;
            if (enemy.IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
            float slow = status != null ? status.SpeedMultiplier : 1f;
            Vector2 v = overrideVelocity ?? desired * enemy.Data.moveSpeed * SpeedMultiplier * slow;
            overrideVelocity = null;
            rb.linearVelocity = v + knock;
            knock = Vector2.MoveTowards(knock, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
            if (enemy.Anim != null)
            {
                if (Mathf.Abs(v.x) > 0.05f) enemy.Anim.Face(v.x);
                if (!enemy.Attacks.IsAttacking && !enemy.Brain.IsSpawning) enemy.Anim.Locomotion(v);
            }
        }

        void OnCollisionEnter2D(Collision2D c) { if (((1 << c.gameObject.layer) & wallMask) != 0) wallContacts++; }
        void OnCollisionExit2D(Collision2D c) { if (((1 << c.gameObject.layer) & wallMask) != 0) wallContacts = Mathf.Max(0, wallContacts - 1); }
    }
}
