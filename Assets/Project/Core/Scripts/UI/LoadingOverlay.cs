using DungeonGathering.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Màn loading trong Bootstrap (UI-Loading.dc.html): bước + % + mẹo xoay vòng + trạng thái lỗi/THỬ LẠI.
    /// Mọi tham chiếu đều tùy chọn để prefab dựng dần vẫn chạy được.
    /// </summary>
    public sealed class LoadingOverlay : MonoBehaviour, ILoadingView
    {
        [SerializeField] CanvasGroup group;
        [Header("Progress")]
        [SerializeField] GameObject progressGroup;
        [SerializeField] Image progressFill;               // Image Filled Horizontal
        [SerializeField] RectTransform runner;             // Knight chạy dọc thanh
        [SerializeField] float runnerTravel = 656f;
        [SerializeField] LocalizedTMPText stepLabel;
        [SerializeField] TMP_Text percentLabel;
        [Header("Tips")]
        [SerializeField] LocalizedTMPText tipLabel;
        [SerializeField] string[] tipKeys = { "loading.tip1", "loading.tip2", "loading.tip3" };
        [SerializeField] float tipInterval = 3.5f;
        [Header("Error")]
        [SerializeField] GameObject errorGroup;
        [SerializeField] LocalizedTMPText errorLabel;
        [SerializeField] Button retryButton;

        float shown;     // % đang hiển thị (không nhảy lùi)
        float target;
        float tipTimer;
        int tipIndex;
        int shownPercent = -1;   // % đã ghi vào percentLabel
        AwaitableCompletionSource retry;

        public bool Visible => gameObject.activeSelf;

        void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        }

        public void Show(string stepKey)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; }
            if (errorGroup != null) errorGroup.SetActive(false);
            if (progressGroup != null) progressGroup.SetActive(true);
            shown = target = 0f;
            SetStep(stepKey);
            Apply();
        }

        public void SetStep(string stepKey)
        {
            if (stepLabel != null && !string.IsNullOrEmpty(stepKey)) stepLabel.SetKey(stepKey);
        }

        public void SetProgress(float progress01) => target = Mathf.Max(target, Mathf.Clamp01(progress01));

        public Awaitable WaitRetry(string messageKey)
        {
            gameObject.SetActive(true);
            if (progressGroup != null) progressGroup.SetActive(false);
            if (errorGroup != null) errorGroup.SetActive(true);
            if (errorLabel != null && !string.IsNullOrEmpty(messageKey)) errorLabel.SetKey(messageKey);
            if (retryButton != null) retryButton.interactable = true;
            retry = new AwaitableCompletionSource();
            if (retryButton == null) retry.TrySetResult(); // không có nút → thử lại ngay
            return retry.Awaitable;
        }

        void OnRetryClicked()
        {
            if (retryButton != null) retryButton.interactable = false; // khóa khi đang thử (§7.4)
            retry?.TrySetResult();
            retry = null;
        }

        public void Hide()
        {
            if (group != null) { group.alpha = 0f; group.blocksRaycasts = false; }
            gameObject.SetActive(false);
        }

        void Update()
        {
            shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 1.5f);
            Apply();
            if (tipLabel != null && tipKeys != null && tipKeys.Length > 0)
            {
                tipTimer -= Time.unscaledDeltaTime;
                if (tipTimer <= 0f)
                {
                    tipTimer = tipInterval;
                    tipIndex = (tipIndex + 1) % tipKeys.Length;
                    tipLabel.SetKey(tipKeys[tipIndex]);
                }
            }
        }

        void Apply()
        {
            if (progressFill != null) progressFill.fillAmount = shown;
            if (percentLabel != null)
            {
                // Apply chạy mỗi frame: chỉ tạo chuỗi mới khi % nguyên đổi.
                int percent = Mathf.RoundToInt(shown * 100f);
                if (percent != shownPercent)
                {
                    shownPercent = percent;
                    percentLabel.text = percent + "%";
                }
            }
            if (runner != null) runner.anchoredPosition = new Vector2(runnerTravel * shown, runner.anchoredPosition.y);
        }
    }
}
