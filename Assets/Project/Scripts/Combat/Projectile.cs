using System.Collections.Generic;
using DG.Tweening;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Tham số một viên đạn (đạn Player hoặc quái).</summary>
    public struct ProjectileSpec
    {
        public float Damage;
        public float Speed;
        public float Knockback;
        public bool Crit;
        public int Pierce;
        public float ExplosionRadius;
        public float Lifetime;          // <= 0 → dùng lifetime của prefab
        public Team Team;
        public GameObject ImpactVfx;
        public GameObject Source;
        public IPlayerContext Owner;    // để báo HitReport cho passive (null với đạn quái)
        public WeaponData Weapon;
    }

    /// <summary>
    /// Đạn bay thẳng qua Pool (WEAPONS §6.4). Cờ isLive: va chạm + hết giờ cùng frame chỉ Despawn một lần (R13).
    /// Prefab cần Collider2D trigger; Rigidbody2D (Kinematic) tùy chọn — không có thì tự di chuyển bằng transform.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] Rigidbody2D rb;
        [SerializeField] float lifetime = 3f;
        [Tooltip("Để 0 → tự tính theo Team (Enemy targets / Player targets)")]
        [SerializeField] LayerMask targetMask;
        [Tooltip("Để 0 → layer Wall")]
        [SerializeField] LayerMask wallMask;
        [SerializeField] bool rotateToVelocity = true;

        ProjectileSpec spec;
        float timer;
        bool isLive;
        int pierceLeft;
        Vector2 velocity;
        int targets, walls;
        readonly HashSet<IDamageable> hitSet = new();

        public Team Team => spec.Team;
        public bool IsLive => isLive;

        void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
        }

        public void Launch(in ProjectileSpec s, Vector2 direction)
        {
            spec = s;
            timer = s.Lifetime > 0f ? s.Lifetime : lifetime;
            pierceLeft = Mathf.Max(0, s.Pierce);
            hitSet.Clear();
            targets = targetMask.value != 0 ? targetMask.value : CombatLayers.TargetsOf(s.Team);
            walls = wallMask.value != 0 ? wallMask.value : CombatLayers.Walls;
            int layer = s.Team == Team.Player ? CombatLayers.PlayerProjectileLayer : CombatLayers.EnemyProjectileLayer;
            if (layer >= 0) gameObject.layer = layer;

            velocity = direction.normalized * s.Speed;
            if (rotateToVelocity && velocity.sqrMagnitude > 0f)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            if (rb != null) rb.linearVelocity = velocity;
            isLive = true;
        }

        void Update()
        {
            if (!isLive) return;
            if (rb == null) transform.position += (Vector3)(velocity * Time.deltaTime);
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                if (spec.ExplosionRadius > 0f) Explode(transform.position);
                Despawn();
            }
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            // Một bước vật lý có thể gọi nhiều lần (2 quái, quái + tường) — chỉ lần hợp lệ đầu được tính (R13).
            if (!isLive) return;
            int bit = 1 << col.gameObject.layer;

            if ((walls & bit) != 0)
            {
                Impact(transform.position);
                return;
            }
            if ((targets & bit) == 0) return;

            var target = CombatUtil.Resolve(col);
            if (target == null || target.IsDead || target.Team == spec.Team) return; // xác đang chết: bay xuyên
            if (!hitSet.Add(target)) return;

            if (spec.ExplosionRadius > 0f)
            {
                Impact(transform.position);
                return;
            }

            bool accepted = target.TakeDamage(new DamageInfo(spec.Damage, spec.Knockback, velocity.normalized, 0f, spec.Crit, spec.Source, spec.Team));
            if (accepted) Report(target, transform.position);
            CombatFx.Hit(spec.ImpactVfx, transform.position);
            if (pierceLeft-- <= 0) Despawn();
        }

        void Impact(Vector2 point)
        {
            if (spec.ExplosionRadius > 0f) Explode(point);
            else { CombatFx.Hit(spec.ImpactVfx, point); Sfx.PlayAt(SfxId.ProjectileHit, point); }
            Despawn();
        }

        void Explode(Vector2 point)
        {
            CombatFx.Hit(spec.ImpactVfx, point);
            Sfx.PlayAt(SfxId.Explosion, point);
            var hits = CombatUtil.RentList();
            try
            {
                CombatUtil.DamageArea(point, spec.ExplosionRadius, targets,
                    new DamageInfo(spec.Damage, spec.Knockback, velocity.normalized, 0f, spec.Crit, spec.Source, spec.Team), hits);
                for (int i = 0; i < hits.Count; i++) Report(hits[i], point);
            }
            finally { CombatUtil.ReturnList(hits); }
        }

        void Report(IDamageable target, Vector2 point)
        {
            if (spec.Owner == null) return;
            spec.Owner.Hooks.RaiseHit(new HitReport
            {
                Target = (target as Component)?.gameObject,
                Point = point,
                Direction = velocity.normalized,
                Damage = spec.Damage,
                Crit = spec.Crit,
                Killed = target.IsDead,
                Weapon = spec.Weapon,
            });
        }

        /// <summary>Gọi từ nhiều nơi (va chạm, hết giờ, bị kiếm chém tan, phòng/run kết thúc) — chỉ lần đầu có tác dụng.</summary>
        public void Despawn()
        {
            if (!isLive) return;
            isLive = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (Services.Pool != null) Services.Pool.Return(gameObject);
            else Destroy(gameObject);
        }

        public void OnSpawn() { }

        public void OnDespawn()
        {
            isLive = false;
            hitSet.Clear();
            spec = default;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            DOTween.Kill(transform);
        }

        // ---- Registry đạn đang bay: để dọn khi phòng/run kết thúc ----
        static readonly List<Projectile> live = new();
        void OnEnable() => live.Add(this);
        void OnDisable() => live.Remove(this);

        /// <summary>Xóa mọi đạn đang bay của một phe (LevelDirector gọi khi chốt run).</summary>
        public static void DespawnAll(Team team)
        {
            for (int i = live.Count - 1; i >= 0; i--)
                if (i < live.Count && live[i] != null && live[i].isLive && live[i].spec.Team == team) live[i].Despawn();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => live.Clear();
    }
}
