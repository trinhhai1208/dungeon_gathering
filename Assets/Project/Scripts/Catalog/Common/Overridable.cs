using System;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Tham số "ghi đè tùy chọn": tắt → dùng giá trị mặc định của SO gốc; bật → dùng <see cref="value"/>.
    /// Inspector vẽ thành checkbox + ô giá trị (OverridableDrawer).
    /// </summary>
    [Serializable]
    public struct Overridable<T>
    {
        public bool enabled;
        public T value;

        public Overridable(T value) { enabled = true; this.value = value; }

        public T Resolve(T defaultValue) => enabled ? value : defaultValue;

        public static Overridable<T> Off => default;
    }
}
