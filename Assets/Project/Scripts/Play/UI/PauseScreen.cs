using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Tạm dừng (UI-Pause, ui/screen/pause). Mở → Game.Pause (timeScale 0), đóng → Resume.
    /// "Bỏ run" → LevelDirector.AbandonRun (→ màn Thua). Panel Settings (Shared) nhúng sẵn trong prefab.
    /// Hiển thị túi đồ run (runBag) tương tự như màn kết thúc run (SYS-01-INV).
    /// </summary>
    public sealed class PauseScreen : UIScreen
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button abandonButton;
        [SerializeField] Button settingsButton;   // tùy chọn: mở ui/popup/settings nếu không nhúng
        [SerializeField] Image characterPortrait;
        [SerializeField] TMPro.TMP_Text characterClassText;
        [SerializeField] TMPro.TMP_Text runTimeText;
        [SerializeField] TMPro.TMP_Text killsText;
        [SerializeField] TMPro.TMP_Text coinsText;
        [SerializeField] RectTransform lootGrid;
        [SerializeField] ItemTile lootTilePrefab;

        readonly List<ItemTile> tiles = new();

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
                BuildLoot(s);
            }
            var character = LevelDirector.Current != null && LevelDirector.Current.Character != null
                ? LevelDirector.Current.Character
                : (ItemCatalog.Current != null && s != null ? ItemCatalog.Current.GetCharacter(s.characterId) : null);
            if (character != null)
            {
                if (characterPortrait != null && character.portrait != null)
                    characterPortrait.sprite = character.portrait;
                if (characterClassText != null)
                {
                    string fallback = char.ToUpperInvariant(character.characterId[0]) + character.characterId.Substring(1);
                    string charName = !string.IsNullOrEmpty(character.nameKey)
                        ? DungeonGathering.Localization.LocalizationLookup.Get(character.nameKey, fallback)
                        : fallback;
                    characterClassText.text = $"{charName} · {DungeonGathering.Localization.LocalizationLookup.Role(character.role)}";
                }
            }
            if (abandonButton != null) abandonButton.interactable = true;
            return null;
        }

        void BuildLoot(SessionData s)
        {
            if (lootGrid == null || lootTilePrefab == null || s == null)
            {
                for (int i = 0; i < tiles.Count; i++)
                    if (tiles[i] != null) tiles[i].gameObject.SetActive(false);
                return;
            }
            var cat = ItemCatalog.Current;
            int idx = 0;
            foreach (var id in s.runBag)
            {
                var item = cat != null ? cat.GetItem(id) : null;
                if (item == null) continue;
                ItemTile tile;
                if (idx < tiles.Count && tiles[idx] != null)
                {
                    tile = tiles[idx];
                    tile.gameObject.SetActive(true);
                }
                else
                {
                    tile = Instantiate(lootTilePrefab, lootGrid);
                    if (idx < tiles.Count) tiles[idx] = tile;
                    else tiles.Add(tile);
                }
                tile.SetItem(item.Icon, item.Rank);
                idx++;
            }
            for (int i = idx; i < tiles.Count; i++)
                if (tiles[i] != null) tiles[i].gameObject.SetActive(false);
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
            await Services.UI.Open<UIScreen>(AddressKeys.UiPopupSettings);
        }
    }
}
