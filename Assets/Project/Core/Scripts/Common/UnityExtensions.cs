using UnityEngine;

namespace DungeonGathering.Core
{
    public static class UnityExtensions
    {
        /// <summary>GetComponent hoặc AddComponent. Không dùng "??" với UnityEngine.Object (fake-null trong Editor).</summary>
        public static T GetOrAdd<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        /// <summary>Trả null thật nếu object Unity đã hủy/không có (dùng được với ?? và ?.).</summary>
        public static T OrNull<T>(this T obj) where T : Object => obj != null ? obj : null;
    }
}
