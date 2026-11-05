using UnityEngine;

namespace DungeonGathering.Core
{
    public enum UILayer { Hud, Screen, Panel, Popup, Overlay, Toast }

    /// <summary>
    /// Base của mọi màn hình UI mở qua IUIService (MODULES §7.5).
    /// Màn hiển thị trạng thái game phải đọc trạng thái hiện tại trong OnOpen rồi mới nghe EventBus (R7).
    /// </summary>
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] UILayer layer = UILayer.Screen;
        [Tooltip("true cho Loadout, Shop, Results: đóng là Release bundle.")]
        [SerializeField] bool releaseOnClose;
        [Tooltip("Back()/Esc có được đóng màn này không.")]
        [SerializeField] bool closeOnBack = true;

        public UILayer Layer => layer;
        public bool ReleaseOnClose => releaseOnClose;
        public bool CloseOnBack => closeOnBack;

        /// <summary>Address đã dùng để mở (UIService gán).</summary>
        public string Address { get; internal set; }
        /// <summary>Scope của module đang active lúc mở.</summary>
        public ModuleScope Owner { get; internal set; }
        public bool IsOpen { get; internal set; }

        /// <summary>Nhận args (vd ShopContext). Có thể trả null.</summary>
        public virtual Awaitable OnOpen(object args) => null;
        public virtual void OnClose() { }
        /// <summary>Màn đã mở được Open lại (đưa lên trên cùng) — có thể nhận args mới.</summary>
        public virtual void OnReopen(object args) { }

        /// <summary>Back/Esc khi màn này ở trên cùng. Trả true nếu đã tự xử lý.</summary>
        public virtual bool OnBack() => false;

        protected void CloseSelf() => Services.UI?.Close(this);
    }
}
