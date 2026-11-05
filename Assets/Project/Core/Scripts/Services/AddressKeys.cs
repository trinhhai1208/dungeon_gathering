namespace DungeonGathering.Core
{
    /// <summary>Address là API (MODULES §3.9, §6.2): mọi chuỗi address/label khai báo ở đây.</summary>
    public static class AddressKeys
    {
        // Scenes
        public const string SceneMenu = "scene/menu";
        public const string SceneLobby = "scene/lobby";
        public const string ScenePlay = "scene/play";

        // UI
        public const string UiScreenMenu = "ui/screen/menu";
        public const string UiHudLobby = "ui/hud/lobby";
        public const string UiPanelCharacter = "ui/panel/character";
        public const string UiScreenEquipment = "ui/screen/equipment";
        public const string UiScreenWeapon = "ui/screen/weapon";
        public const string UiScreenShop = "ui/screen/shop";
        public const string UiHudPlay = "ui/hud/play";
        public const string UiScreenPause = "ui/screen/pause";
        public const string UiPopupSettings = "ui/popup/settings";
        public const string UiScreenVictory = "ui/screen/victory";
        public const string UiScreenDefeat = "ui/screen/defeat";
        public const string UiSharedItemTile = "ui/shared/item-tile";
        public const string UiPopupConfirm = "ui/popup/confirm";
        public const string UiToast = "ui/toast";

        // Data
        public const string DataCatalog = "data/catalog";
        public const string CoreModuleRegistry = "core/module-registry";
        public const string CoreFontConfig = "core/localization/font-config";

        // Gameplay
        public const string PlayerShell = "char/player-shell";
        public const string PlayPrefabSet = "play/prefab-set";
        public static string LobbyCharacter(string id) => $"char/{id}";
        public static string DungeonConfig(string theme, int floor) => $"theme/{theme}/config/floor-{floor}";
        public static string Boss(string id) => $"boss/{id}";

        // Audio
        public static string Bgm(string id) => $"audio/bgm/{id}";
        public static string Sfx(string id) => $"audio/sfx/{id}";

        // Labels
        public const string LabelCore = "core";
        public const string LabelSharedUi = "shared-ui";
        public const string LabelLocaleVi = "locale-vi";
    }
}
