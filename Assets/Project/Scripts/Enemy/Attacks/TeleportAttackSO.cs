using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Biến mất rồi dịch chuyển tới quanh mục tiêu; có thể nối tiếp bằng một đòn đánh.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Teleport", fileName = "Atk_Teleport")]
    public class TeleportAttackSO : EnemyAttackSO
    {
        [Tooltip("Khoảng cách tối thiểu từ Player tới điểm xuất hiện (world-unit, tối thiểu 0.8).")] public float minAppearDistance = 1.5f;
        [Tooltip("Khoảng cách tối đa từ Player tới điểm xuất hiện (world-unit, không nhỏ hơn min).")] public float maxAppearDistance = 3.5f;
        [Tooltip("Thời gian biến mất trước khi hiện lại (giây); ≤ 0 = dùng windupTime.")] public float vanishDuration = 0.25f;
        [Tooltip("VFX phát tại điểm biến mất và điểm xuất hiện; null = không có.")] public GameObject teleportVfx;
        [Tooltip("Đòn tung ngay sau khi xuất hiện, chỉ khi Player trong range (và tầm nhìn nếu đòn yêu cầu); bỏ qua cooldown. Không được là Teleport. Null = chỉ dịch chuyển.")] public EnemyAttackSO followUpAttack;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            CombatFx.Play(teleportVfx, ctx.Self.transform.position, Quaternion.identity);
            ctx.Self.Motor?.Stop();

            var sr = ctx.Self.Anim?.Renderer;
            bool wasVisible = sr != null && sr.enabled;
            try
            {
                if (sr != null) sr.enabled = false;
                await Wait(Mathf.Max(0f, vanishDuration > 0f ? vanishDuration : windupTime), ctx.Token);
            }
            finally
            {
                if (sr != null && wasVisible) sr.enabled = true;
            }
            if (ctx.Self.IsDead) return;

            Vector2 dest = PickTeleportPoint(ctx.TargetPos, ctx.Self.transform.position);
            if (ctx.Self.Motor != null) ctx.Self.Motor.TeleportTo(dest);
            else ctx.Self.transform.position = new Vector3(dest.x, dest.y, ctx.Self.transform.position.z);
            CombatFx.Play(teleportVfx, dest, Quaternion.identity);
            ctx.Self.Anim?.Face(ctx.DirToTarget.x);

            if (CanFollowUp(ctx)) await followUpAttack.Execute(ctx);
        }

        /// <summary>Đòn nối chỉ chạy khi hợp lệ: không phải teleport (tránh vòng lặp), Player trong tầm và (nếu cần) không bị che.</summary>
        bool CanFollowUp(EnemyAttackContext ctx)
        {
            if (followUpAttack == null || followUpAttack is TeleportAttackSO || ctx.Self.IsDead) return false;
            Vector2 from = ctx.Origin; // đọc trực tiếp vị trí hiện tại (sau khi dịch chuyển)
            if (followUpAttack.range > 0f && Vector2.Distance(from, ctx.TargetPos) > followUpAttack.range) return false;
            if (followUpAttack.requireLineOfSight && Physics2D.Linecast(from, ctx.TargetPos, CombatLayers.Walls).collider != null) return false;
            return true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (followUpAttack is TeleportAttackSO)
            {
                Debug.LogWarning($"[{name}] followUpAttack không được là Teleport (gây vòng lặp) — đã bỏ.", this);
                followUpAttack = null;
            }
        }
#endif

        Vector2 PickTeleportPoint(Vector2 around, Vector2 fallback)
        {
            float minD = Mathf.Max(0.8f, minAppearDistance);
            float maxD = Mathf.Max(minD, maxAppearDistance);
            for (int i = 0; i < 16; i++)
            {
                float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float r = Random.Range(minD, maxD);
                Vector2 candidate = around + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Physics2D.OverlapCircle(candidate, 0.4f, CombatLayers.Walls) != null) continue;
                if (Physics2D.Linecast(around, candidate, CombatLayers.Walls).collider != null) continue;
                return candidate;
            }
            return fallback;
        }
    }
}
