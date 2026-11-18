using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Sét lan: xác suất giật thêm N mục tiêu kế cận.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Weapon Chain Lightning", fileName = "WP_Chain")]
    public class ChainLightningPassive : HookWeaponPassive
    {
        [Range(0, 1)] public float chance = 0.2f;
        [Min(1)] public int jumps = 2;
        [Tooltip("Bán kính (world unit) tìm quái gần nhất cho mỗi bước sét lan; không còn quái trong vòng = dừng lan.")]
        public float radius = 3f;
        [Range(0, 2)] public float damageFraction = 0.5f;
        [Tooltip("Prefab VFX phát tại mỗi quái bị sét lan trúng; null = không hiệu ứng.")]
        public GameObject vfx;

        // Buffer collider chỉ dùng trong lúc quét (xong trước khi gây sát thương) → an toàn khi lồng nhau.
        static readonly Collider2D[] buffer = new Collider2D[32];

        protected override void OnHit(IPlayerContext player, PassiveState state, in HitReport hit)
        {
            // state.Busy: tránh đệ quy — đòn lan không tự lan tiếp (theo từng Player, không nằm trên SO).
            if (state.Busy || hit.Target == null || chance <= 0f || UnityEngine.Random.value > chance) return;
            state.Busy = true;
            var visited = CombatUtil.RentList();
            try
            {
                var first = hit.Target.GetComponentInParent<IDamageable>();
                if (first != null) visited.Add(first);
                Vector2 from = hit.Target.transform.position;
                float dmg = hit.Damage * damageFraction;
                var filter = new ContactFilter2D { useLayerMask = true, layerMask = CombatLayers.EnemyTargets, useTriggers = true };
                for (int j = 0; j < jumps; j++)
                {
                    int n = Physics2D.OverlapCircle(from, radius, filter, buffer);
                    IDamageable next = null;
                    float best = float.MaxValue;
                    for (int i = 0; i < n; i++)
                    {
                        var d = CombatUtil.Resolve(buffer[i]);
                        buffer[i] = null;
                        if (d == null || d.IsDead || visited.Contains(d)) continue;
                        float sq = ((Vector2)d.transform.position - from).sqrMagnitude;
                        if (sq < best) { best = sq; next = d; }
                    }
                    if (next == null) break;
                    visited.Add(next);
                    Vector2 to = next.transform.position;
                    CombatFx.Play(vfx, to, Quaternion.identity);
                    if (next.TakeDamage(new DamageInfo(dmg, 0f, (to - from).normalized, 0f, false, player.GameObject, Team.Player)))
                        player.Hooks.RaiseHit(new HitReport { Target = (next as Component)?.gameObject, Point = to, Damage = dmg, Killed = next.IsDead, Weapon = hit.Weapon });
                    from = to;
                }
            }
            finally
            {
                CombatUtil.ReturnList(visited);
                state.Busy = false;
            }
        }
    }
}
