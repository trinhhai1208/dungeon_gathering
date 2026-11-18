using DungeonGathering.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Lobby HUD (ui/hud/lobby theo UI-Lobby.dc.html):
    /// Thường trực trên UILayer.Hud trong sảnh: thanh coin, nút Back về Menu, và khung gợi ý chọn nhân vật.
    /// Tự ẩn gợi ý khi CharacterPanel mở.
    /// </summary>
    public sealed class LobbyHud : UIScreen
    {
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text coinText;
        [SerializeField] GameObject hintPickHero;

        void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        public override Awaitable OnOpen(object args)
        {
            OnWallet(new WalletChangedEvent { Coins = Services.Save != null ? Services.Save.Data.coins : 0 });
            if (hintPickHero != null)
                hintPickHero.gameObject.SetActive(Services.UI == null || !Services.UI.IsOpen(AddressKeys.UiPanelCharacter));
            return null;
        }

        void OnEnable()
        {
            EventBus<WalletChangedEvent>.Subscribe(OnWallet);
            EventBus<CharacterPanelChangedEvent>.Subscribe(OnCharacterPanelChanged);
        }

        void OnDisable()
        {
            EventBus<WalletChangedEvent>.Unsubscribe(OnWallet);
            EventBus<CharacterPanelChangedEvent>.Unsubscribe(OnCharacterPanelChanged);
        }

        void OnWallet(WalletChangedEvent e)
        {
            if (coinText != null) coinText.text = e.Coins.ToString("N0");
        }

        void OnCharacterPanelChanged(CharacterPanelChangedEvent e)
        {
            if (hintPickHero != null)
                hintPickHero.gameObject.SetActive(!e.Open);
        }

        void OnBackClicked()
        {
            if (Services.Scenes != null && Services.Scenes.IsTransitioning) return;
            _ = Services.Scenes?.Go(AddressKeys.SceneMenu);
        }

        public override bool OnBack()
        {
            OnBackClicked();
            return true;
        }
    }
}
