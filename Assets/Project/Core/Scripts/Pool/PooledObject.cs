using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonGathering.Core
{
    /// <summary>Gắn tự động lên instance do PoolManager tạo.</summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        internal PoolManager.Pool Pool;
        internal IPoolable[] Poolables;
        public bool InPool { get; internal set; }
        /// <summary>Tăng mỗi lần Get — hẹn giờ tự trả phải so khớp (MODULES §7.7).</summary>
        public int SpawnVersion { get; internal set; }
        public bool IsAlive => !InPool && Pool != null;
    }
}
