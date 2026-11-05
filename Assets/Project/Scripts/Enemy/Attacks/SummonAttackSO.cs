using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Triệu hồi quái con. Quái con đăng ký vào phòng qua Enemy.SpawnContext.Summon (vào tập alive của RoomController).</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Enemy Attacks/Summon", fileName = "Atk_Summon")]
    public class SummonAttackSO : EnemyAttackSO
    {
        public Enemy minionPrefab;
        [Min(1)] public int count = 2;
        public float radius = 1.5f;
        [Tooltip("Số quái con tối đa còn sống cùng lúc do con này gọi")] public int maxAlive = 6;

        public override bool CanUse(Enemy self, Combat.ITargetable target, float distance)
            => minionPrefab != null && self.SummonCount < maxAlive && distance <= range;

        public override async Awaitable Execute(EnemyAttackContext ctx)
        {
            ctx.Self.Anim?.Play(Combat.AnimKey.Attack, true);
            await Wait(windupTime, ctx.Token);
            if (ctx.Self.IsDead) return;
            for (int i = 0; i < count && ctx.Self.SummonCount < maxAlive; i++)
            {
                Vector2 pos = ctx.Origin + Random.insideUnitCircle.normalized * radius;
                ctx.Self.SpawnMinion(minionPrefab, pos);
            }
        }
    }
}
