using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Thư viện đạn dùng chung cho súng và kiếm khí.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Weapons/Projectile Library", fileName = "ProjectileLibrary")]
    public sealed class ProjectileLibrarySO : ScriptableObject
    {
        [Tooltip("Các loại đạn; vũ khí chọn theo id (projectileId, waveProjectileId, defaultProjectileId)")] public List<ProjectileEntry> projectiles = new();
    }
}
