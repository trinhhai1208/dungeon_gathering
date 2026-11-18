using System.Collections.Generic;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    [System.Flags]
    public enum DamageFlags
    {
        None = 0,
        /// <summary>Sát thương từ hiệu ứng theo thời gian: bỏ qua i-frame, không đặt lại i-frame, không giật/đẩy.</summary>
        DoT = 1 << 0,
        /// <summary>Bỏ qua giáp (Kill, sát thương chuẩn).</summary>
        IgnoreArmor = 1 << 1,
    }

    public readonly struct DamageInfo
    {
        public readonly float Amount, Knockback, CritBonus;
        public readonly Vector2 Direction;
        public readonly bool IsCrit;
        public readonly GameObject Source;
        public readonly Team Team;
        public readonly DamageFlags Flags;

        public DamageInfo(float amount, float knockback, Vector2 direction, float critBonus = 0f,
                          bool isCrit = false, GameObject source = null, Team team = Team.Neutral,
                          DamageFlags flags = DamageFlags.None)
        {
            Amount = amount; Knockback = knockback; Direction = direction; CritBonus = critBonus;
            IsCrit = isCrit; Source = source; Team = team; Flags = flags;
        }

        public bool IsDoT => (Flags & DamageFlags.DoT) != 0;
        public bool IgnoresArmor => (Flags & DamageFlags.IgnoreArmor) != 0;

        public DamageInfo WithAmount(float amount) => new(amount, Knockback, Direction, CritBonus, IsCrit, Source, Team, Flags);
    }

    /// <summary>Nguồn sát thương có VFX trúng riêng (quái theo định nghĩa) — bên nhận phát VFX này tại chỗ trúng.</summary>
    public interface IHitVfxSource
    {
        GameObject HitVfx { get; }
    }

    /// <summary>
    /// Mọi thứ nhận sát thương. TakeDamage kiểm IsDead đầu tiên; HP về 0 → đặt IsDead TRƯỚC rồi Die() một lần (R12).
    /// </summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        Team Team { get; }
        Transform transform { get; }
        /// <summary>Trả false nếu đòn bị bỏ qua (đã chết, bất tử, cùng phe).</summary>
        bool TakeDamage(in DamageInfo info);
    }

    /// <summary>Mục tiêu mà quái truy đuổi (Player).</summary>
    public interface ITargetable
    {
        Transform Transform { get; }
        bool IsAlive { get; }
        IDamageable Damageable { get; }
    }

    /// <summary>AI mục tiêu có thể bị khiêu khích (Companion/Decoy gán mục tiêu ưu tiên).</summary>
    public interface ITauntableTargetAI
    {
        ITargetable OverrideTarget { get; set; }
    }

    /// <summary>Registry tối giản để Enemy tìm Player mà không tham chiếu module Play.</summary>
    public static class CombatTargets
    {
        public static ITargetable Player { get; private set; }
        public static void RegisterPlayer(ITargetable p) => Player = p;
        public static void UnregisterPlayer(ITargetable p) { if (Player == p) Player = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => Player = null;
    }

    /// <summary>Layer va chạm theo tên (ENTITIES §3.1). Thiếu layer → mask 0 + cảnh báo một lần.</summary>
    public static class CombatLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string PlayerHurtbox = "PlayerHurtbox";
        public const string EnemyHurtbox = "EnemyHurtbox";
        public const string PlayerProjectile = "PlayerProjectile";
        public const string EnemyProjectile = "EnemyProjectile";
        public const string Wall = "Wall";
        public const string Pickup = "Pickup";
        public const string Trap = "Trap";

        static readonly HashSet<string> warned = new();

        public static int Layer(string name)
        {
            int l = LayerMask.NameToLayer(name);
            if (l < 0 && warned.Add(name)) Debug.LogWarning($"[Combat] Thiếu layer '{name}' trong Tags & Layers.");
            return l;
        }

        public static int Mask(params string[] names)
        {
            int m = 0;
            foreach (var n in names) { int l = Layer(n); if (l >= 0) m |= 1 << l; }
            return m;
        }

        // Cache mask/layer: tên layer không đổi lúc chạy → tính một lần, không cấp phát mảng params mỗi lần gọi.
        static bool cached;
        static int enemyTargets, playerTargets, walls, enemyBullets, playerProjectileLayer, enemyProjectileLayer;

        static void EnsureCache()
        {
            if (cached) return;
            enemyTargets = Mask(Enemy, EnemyHurtbox);
            playerTargets = Mask(Player, PlayerHurtbox);
            walls = Mask(Wall);
            enemyBullets = Mask(EnemyProjectile);
            playerProjectileLayer = Layer(PlayerProjectile);
            enemyProjectileLayer = Layer(EnemyProjectile);
            cached = true;
        }

        public static int EnemyTargets { get { EnsureCache(); return enemyTargets; } }
        public static int PlayerTargets { get { EnsureCache(); return playerTargets; } }
        public static int Walls { get { EnsureCache(); return walls; } }
        public static int EnemyBullets { get { EnsureCache(); return enemyBullets; } }
        /// <summary>Layer index của đạn Player / quái (-1 nếu thiếu).</summary>
        public static int PlayerProjectileLayer { get { EnsureCache(); return playerProjectileLayer; } }
        public static int EnemyProjectileLayer { get { EnsureCache(); return enemyProjectileLayer; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() { cached = false; warned.Clear(); }

        public static int TargetsOf(Team attacker) => attacker switch
        {
            Team.Player => EnemyTargets,
            Team.Enemy => PlayerTargets,
            _ => 0,
        };
    }

    public static class CombatUtil
    {
        static readonly Collider2D[] buffer = new Collider2D[64];
        // Pool danh sách tạm: DamageArea có thể lồng nhau (TakeDamage → hook → DamageArea khác) nên mỗi lần gọi thuê list riêng.
        static readonly Stack<List<IDamageable>> listPool = new();

        /// <summary>Thuê list tạm (không cấp phát nếu pool còn). Bắt buộc trả bằng <see cref="ReturnList"/>.</summary>
        public static List<IDamageable> RentList() => listPool.Count > 0 ? listPool.Pop() : new List<IDamageable>(16);

        public static void ReturnList(List<IDamageable> list)
        {
            if (list == null) return;
            list.Clear();
            listPool.Push(list);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => listPool.Clear();

        /// <summary>IDamageable gắn với collider (collider con như Hurtbox tìm lên cha).</summary>
        public static IDamageable Resolve(Collider2D col)
        {
            if (col == null) return null;
            return col.GetComponentInParent<IDamageable>();
        }

        /// <summary>
        /// Gây sát thương vùng: gom mục tiêu DUY NHẤT trước (mỗi mục tiêu tối đa một lần mỗi đòn — R12).
        /// Trả số mục tiêu trúng; results (tùy chọn) nhận danh sách.
        /// An toàn khi lồng nhau: buffer collider dùng xong trước khi gọi TakeDamage, danh sách mục tiêu là list thuê riêng.
        /// <paramref name="blockMask"/> ≠ 0: bỏ qua mục tiêu bị lớp đó (vd tường) che giữa tâm và mục tiêu.
        /// </summary>
        public static int DamageArea(Vector2 center, float radius, int mask, in DamageInfo info, List<IDamageable> results = null,
                                     IDamageable ignore = null, int blockMask = 0)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = true };
            int n = Physics2D.OverlapCircle(center, radius, filter, buffer);
            var unique = RentList();
            try
            {
                for (int i = 0; i < n; i++)
                {
                    var d = Resolve(buffer[i]);
                    buffer[i] = null;
                    if (d == null || d.IsDead || d == ignore || unique.Contains(d)) continue;
                    unique.Add(d);
                }
                int hits = 0;
                for (int i = 0; i < unique.Count; i++)
                {
                    var d = unique[i];
                    if (d.IsDead) continue; // có thể đã chết do hiệu ứng lồng từ mục tiêu trước
                    if (blockMask != 0 && Physics2D.Linecast(center, d.transform.position, blockMask).collider != null) continue;
                    Vector2 dir = ((Vector2)d.transform.position - center);
                    dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : info.Direction;
                    var hit = new DamageInfo(info.Amount, info.Knockback, dir, info.CritBonus, info.IsCrit, info.Source, info.Team, info.Flags);
                    if (d.TakeDamage(hit)) { hits++; results?.Add(d); }
                }
                return hits;
            }
            finally { ReturnList(unique); }
        }

        /// <summary>Mục tiêu gần nhất còn sống trong tầm, có đường nhìn (không bị tường chắn).</summary>
        public static IDamageable FindNearest(Vector2 origin, float range, int targetMask, int wallMask, bool requireLineOfSight = true)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask, useTriggers = true };
            int n = Physics2D.OverlapCircle(origin, range, filter, buffer);
            IDamageable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var d = Resolve(buffer[i]);
                buffer[i] = null;
                if (d == null || d.IsDead) continue;
                Vector2 p = d.transform.position;
                float sqr = (p - origin).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                if (requireLineOfSight && wallMask != 0)
                {
                    var hit = Physics2D.Linecast(origin, p, wallMask);
                    if (hit.collider != null) continue;
                }
                best = d;
                bestSqr = sqr;
            }
            return best;
        }

        /// <summary>Tính sát thương cuối có chí mạng.</summary>
        public static float RollDamage(float baseDamage, float critChance, float critMultiplier, out bool crit)
        {
            crit = Random.value < critChance;
            return crit ? baseDamage * critMultiplier : baseDamage;
        }
    }
}
