using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Canvas overlay duy nhất của game (UI_SYSTEM §2). Chỉ giữ tham chiếu tới các layer;
    /// logic mở/đóng nằm ở UIService.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRoot : MonoBehaviour
    {
        [SerializeField] RectTransform layerHud;
        [SerializeField] RectTransform layerScreen;
        [SerializeField] RectTransform layerPanel;
        [SerializeField] RectTransform layerPopup;
        [SerializeField] RectTransform layerOverlay;
        [SerializeField] RectTransform layerToast;
        [SerializeField] GameObject backdrop;
        [SerializeField] Graphic popupDimmer;
        [SerializeField] Graphic inputBlocker;

        public RectTransform GetLayer(UILayer layer)
        {
            RectTransform rt = layer switch
            {
                UILayer.Hud => layerHud,
                UILayer.Screen => layerScreen,
                UILayer.Panel => layerPanel,
                UILayer.Popup => layerPopup,
                UILayer.Overlay => layerOverlay,
                UILayer.Toast => layerToast,
                _ => layerScreen,
            };
            return rt != null ? rt : (RectTransform)transform;
        }

        public void SetBackdrop(bool visible) { if (backdrop != null) backdrop.SetActive(visible); }

        public void SetPopupDimmer(bool visible) { if (popupDimmer != null) popupDimmer.enabled = visible; }

        public void SetInputBlocker(bool blocked)
        {
            if (inputBlocker == null) return;
            inputBlocker.enabled = blocked;
            inputBlocker.raycastTarget = blocked;
            if (blocked) inputBlocker.transform.SetAsLastSibling();
        }

        void Awake()
        {
            SetPopupDimmer(false);
            SetInputBlocker(false);
        }
    }
}
