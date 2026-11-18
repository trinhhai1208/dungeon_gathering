using System.Threading;
using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Thông tin một lượt tấn công.</summary>
    public readonly struct EnemyAttackContext
    {
        public readonly Enemy Self;
        public readonly ITargetable Target;
        public readonly CancellationToken Token;   // bị hủy khi quái chết / về pool / run kết thúc
        public Vector2 Origin => Self.AttackOrigin.position;
        public Vector2 TargetPos => Target != null && Target.Transform != null ? (Vector2)Target.Transform.position : Origin;
        public Vector2 DirToTarget { get { var d = TargetPos - Origin; return d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.right; } }

        public EnemyAttackContext(Enemy self, ITargetable target, CancellationToken token)
        {
            Self = self; Target = target; Token = token;
        }
    }

    /// <summary>
    /// Đòn tấn công dạng module (ARCHITECTURE §6). Execute là async: windup bị hủy khi quái chết (DeadState)
    /// → không tung đòn sau khi chết.
    /// </summary>
    public abstract class EnemyAttackSO : Catalog.SkillBaseSO
    {
        [Tooltip("Khoảng cách tối đa (world-unit) từ điểm ra đòn tới Player để đòn này được chọn.")] public float range = 1.2f;
        [Tooltip("Thời gian gồng/cảnh báo trước khi ra đòn (giây); bị hủy nếu quái chết.")] public float windupTime = 0.4f;
        [Tooltip("Trọng số chọn khi nhiều đòn sẵn sàng")] [Min(0)] public float weight = 1f;
        [Tooltip("Phải nhìn thấy Player (không bị tường chắn)")] public bool requireLineOfSight = true;
        [Tooltip("Đứng yên khi ra đòn")] public bool stopWhileAttacking = true;
        [Tooltip("Hiệu ứng bất lợi kèm theo khi trúng mục tiêu (tùy chọn).")]
        public StatusPayload status;

        public virtual bool CanUse(Enemy self, ITargetable target, float distance) => distance <= range;

        public abstract Awaitable Execute(EnemyAttackContext ctx);

        /// <summary>Windup có thể bị hủy (OperationCanceledException).</summary>
        protected static Awaitable Wait(float seconds, CancellationToken token) => Awaitable.WaitForSecondsAsync(Mathf.Max(0f, seconds), token);
    }
}
