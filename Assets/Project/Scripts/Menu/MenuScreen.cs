using DungeonGathering.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Menu
{
    /// <summary>
    /// Màn Menu (UI-Menu): coin vĩnh viễn, PLAY / "TAP TO PLAY" → Lobby, nút Settings mở popup dùng chung.
    /// Nút PLAY tự khóa khi bấm (R1). Cú chạm này cũng mở khóa audio trên WebGL.
    /// </summary>
    public sealed class MenuScreen : UIScreen
    {
        [SerializeField] Button playButton;
        [SerializeField] Button tapAnywhere;      // nút trong suốt phủ màn (tùy chọn)
        [SerializeField] Button settingsButton;
        [SerializeField] Button soundButton;
        [SerializeField] Button musicButton;
        [SerializeField] Image soundIcon;
        [SerializeField] Image musicIcon;
        [SerializeField] TMP_Text coinText;

        void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (tapAnywhere != null) tapAnywhere.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        }

        /// <summary>Màn gốc của module Menu: Esc/Back không được đóng (không có đường mở lại → kẹt màn trống).</summary>
        public override bool OnBack() => true;

        public override Awaitable OnOpen(object args)
        {
            SetInteractable(true);
            UpdateAudioVisuals();
            OnWallet(new WalletChangedEvent { Coins = Services.Save != null ? Services.Save.Data.coins : 0 });
            return null;
        }

        void OnEnable() => EventBus<WalletChangedEvent>.Subscribe(OnWallet);
        void OnDisable() => EventBus<WalletChangedEvent>.Unsubscribe(OnWallet);

        void OnWallet(WalletChangedEvent e)
        {
            if (coinText != null) coinText.text = e.Coins.ToString("N0");
        }

        void Play()
        {
            if (Services.Scenes != null && Services.Scenes.IsTransitioning) return;
            SetInteractable(false);
            _ = Services.Scenes?.Go(AddressKeys.SceneLobby);
        }

        async void OpenSettings()
        {
            if (Services.UI != null) await Services.UI.Open<UIScreen>(AddressKeys.UiPopupSettings);
        }

        void ToggleSound()
        {
            if (Services.Save?.Data?.settings == null) return;
            var settings = Services.Save.Data.settings;
            settings.sfxVolume = settings.sfxVolume > 0 ? 0 : 8;
            Services.Audio?.ApplySettings(settings);
            Services.Save.RequestSave();
            UpdateAudioVisuals();
        }

        void ToggleMusic()
        {
            if (Services.Save?.Data?.settings == null) return;
            var settings = Services.Save.Data.settings;
            settings.bgmVolume = settings.bgmVolume > 0 ? 0 : 7;
            Services.Audio?.ApplySettings(settings);
            Services.Save.RequestSave();
            UpdateAudioVisuals();
        }

        void UpdateAudioVisuals()
        {
            var settings = Services.Save?.Data?.settings;
            if (settings == null) return;
            if (soundIcon != null) soundIcon.color = settings.sfxVolume > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            if (musicIcon != null) musicIcon.color = settings.bgmVolume > 0 ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }

        void SetInteractable(bool on)
        {
            if (playButton != null) playButton.interactable = on;
            if (tapAnywhere != null) tapAnywhere.interactable = on;
        }
    }
}
