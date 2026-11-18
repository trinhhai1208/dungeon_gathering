using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Tra cứu vũ khí theo id trong các thư viện đã đăng ký (ItemCatalog.Register gọi <see cref="Register"/>).
    /// <see cref="Resolve"/>: SO gốc → (đạn) → entry; vũ khí chưa có entry dùng <see cref="WeaponSpec.Missing"/>.
    /// </summary>
    public static class WeaponLibrary
    {
        static SwordLibrarySO swordLib;
        static RangedLibrarySO rangedLib;
        static readonly Dictionary<string, SwordEntry> swords = new();
        static readonly Dictionary<string, RangedEntry> ranged = new();
        static readonly Dictionary<string, ProjectileEntry> projectiles = new();
        static readonly Dictionary<WeaponData, WeaponSpec> cache = new();

        public static int Version { get; private set; }

        public static void Register(SwordLibrarySO s, RangedLibrarySO r, ProjectileLibrarySO p)
        {
            swordLib = s; rangedLib = r;
            swords.Clear(); ranged.Clear(); projectiles.Clear(); cache.Clear();
            if (p != null) foreach (var e in p.projectiles) if (e != null && !string.IsNullOrEmpty(e.id)) projectiles[e.id] = e;
            if (s != null) foreach (var e in s.swords) if (e != null && !string.IsNullOrEmpty(e.id)) swords[e.id] = e;
            if (r != null) foreach (var e in r.weapons) if (e != null && !string.IsNullOrEmpty(e.id)) ranged[e.id] = e;
            Version++;
        }

        public static bool TryGetProjectile(string id, out ProjectileEntry e) => projectiles.TryGetValue(id ?? "", out e);
        public static bool TryGetSword(string id, out SwordEntry e) => swords.TryGetValue(id ?? "", out e);
        public static bool TryGetRanged(string id, out RangedEntry e) => ranged.TryGetValue(id ?? "", out e);
        public static bool TryGetEntry(string id, out WeaponEntryBase e)
        {
            if (swords.TryGetValue(id ?? "", out var s)) { e = s; return true; }
            if (ranged.TryGetValue(id ?? "", out var r)) { e = r; return true; }
            e = null;
            return false;
        }

        public static bool HasEntry(string id) => swords.ContainsKey(id ?? "") || ranged.ContainsKey(id ?? "");

        /// <summary>Thông số vũ khí đã resolve (cache theo asset; Register mới xóa cache).</summary>
        public static WeaponSpec Resolve(WeaponData item)
        {
            if (item == null) return null;
            if (cache.TryGetValue(item, out var cached)) return cached;
            var spec = Build(item);
            cache[item] = spec;
            return spec;
        }

        /// <summary>Dựng spec mới (không cache) — dùng cho test và xem trước trong Editor.</summary>
        public static WeaponSpec Build(WeaponData item)
        {
            if (item == null) return null;
            if (swords.TryGetValue(item.id ?? "", out var se) && swordLib != null && swordLib.baseConfig != null)
                return Build(item, swordLib.baseConfig, se);
            if (ranged.TryGetValue(item.id ?? "", out var re) && rangedLib != null && rangedLib.baseConfig != null)
                return Build(item, rangedLib.baseConfig, re);
            Debug.LogWarning($"[Weapons] '{item.id}' chưa có entry trong Sword / Ranged Library — dùng thông số tối thiểu.");
            return WeaponSpec.Missing(item);
        }

        public static WeaponSpec Build(WeaponData item, WeaponBaseSO baseConfig, WeaponEntryBase entry)
        {
            var spec = new WeaponSpec { Item = item, fromLibrary = true };
            baseConfig.WriteDefaults(spec);
            // Đạn mặc định của base chỉ áp khi entry KHÔNG tự chọn đạn: nếu không, entry chọn đạn khác vẫn kế thừa
            // hiệu ứng trạng thái / VFX của đạn mặc định, và chọn trùng đạn mặc định thì hiệu ứng bị cộng hai lần.
            bool entryHasProjectile = entry is RangedEntry re && !string.IsNullOrEmpty(re.projectileId);
            if (!entryHasProjectile && baseConfig is RangedBaseSO rb && !string.IsNullOrEmpty(rb.defaultProjectileId)
                && TryGetProjectile(rb.defaultProjectileId, out var dp))
                dp.Apply(spec);
            entry.Apply(spec);
            return spec;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            swordLib = null; rangedLib = null;
            swords.Clear(); ranged.Clear(); projectiles.Clear(); cache.Clear();
        }
    }
}
