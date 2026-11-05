using DungeonGathering.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Tạm dừng (UI-Pause, ui/screen/pause). Mở → Game.Pause (timeScale 0), đóng → Resume.
    /// "Bỏ run" → LevelDirector.AbandonRun (→ màn Thua). Panel Settings (Shared) nhúng sẵn trong prefab.
    /// </summary>
    public sealed class PauseScreen : UIScreen
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button abandonButton;
        [SerializeField] Button settingsButton;   // tùy chọn: mở ui/popup/settings nếu không nhúng
        [SerializeField] TMPro.TMP_Text runTimeText;
        [SerializeField] TMPro.TMP_Text killsText;
        [SerializeField] TMPro.TMP_Text coinsText;

        void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(CloseSelf);
            if (abandonButton != null) abandonButton.onClick.AddListener(Abandon);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        }

        public override Awaitable OnOpen(object args)
        {
            Services.Game?.Pause();
            var s = Services.Session;
            if (s != null)
            {
                if (runTimeText != null) runTimeText.text = $"{(int)s.runTime / 60:00}:{(int)s.runTime % 60:00}";
                if (killsText != null) killsText.text = s.kills.ToString();
                if (coinsText != null) coinsText.text = s.runCoins.ToString();
            }
            if (abandonButton != null) abandonButton.interactable = true;
            return null;
        }

        public override void OnClose() => Services.Game?.Resume();

        void Abandon()
        {
            if (abandonButton != null) abandonButton.interactable = false; // khóa ngay (R11)
            CloseSelf();
            LevelDirector.Current?.AbandonRun();
        }

        async void OpenSettings()
        {
            if (!AddressablesUtil.KeyExists(AddressKeys.UiPopupSettings)) return;
            await Services.UI.Open<UIScreen>(AddressKeys.UiPopupSettings);
        }
    }
}
