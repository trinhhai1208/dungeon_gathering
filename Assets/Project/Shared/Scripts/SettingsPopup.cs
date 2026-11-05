using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.UI
{
    /// <summary>
    /// Popup cài đặt mở từ nút bánh răng ở Menu (<c>ui/popup/settings</c>). Bọc <see cref="SettingsPanel"/> dùng chung
    /// (cùng panel nhúng trong PauseScreen) + nút đóng. Layer Popup.
    /// </summary>
    public sealed class SettingsPopup : UIScreen
    {
        [SerializeField] Button closeButton;
        [SerializeField] SettingsPanel panel;

        // Đóng đúng popup này (Back() có thể hủy một Open đang chờ hoặc đóng popup khác ở trên).
        void Awake() { if (closeButton != null) closeButton.onClick.AddListener(CloseSelf); }

        public override Awaitable OnOpen(object args)
        {
            panel?.Refresh();
            return base.OnOpen(args);
        }
    }
}
