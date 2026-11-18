using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Results
{
    /// <summary>
    /// Màn Thắng (UI-Victory, ui/screen/victory) / Thua (UI-RunEnd, ui/screen/defeat) — cùng một script, cờ isVictory.
    /// Đọc SessionData đã chốt khi OnOpen. Nút: Về sảnh (quy đổi một lần — R11) · Tiếp tục (thắng) / Chơi lại (thua).
    /// Mọi nút tự khóa ngay khi bấm (R1).
    /// </summary>
    public sealed class RunResultScreen : UIScreen
    {
        [SerializeField] bool isVictory = true;
        [SerializeField] Button lobbyButton;
        [SerializeField] Button continueButton;   // Tiếp tục (thắng) hoặc Chơi lại (thua)
        [SerializeField] TMP_Text floorText;
        [Tooltip("Tên boss vừa hạ (chỉ màn Victory) — lấy từ SessionData.currentBossNameKey")]
        [SerializeField] TMP_Text bossNameText;
        [SerializeField] TMP_Text killsText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] TMP_Text coinsText;
        [SerializeField] RectTransform lootGrid;
        [SerializeField] ItemTile lootTilePrefab;     // ItemTileSmall
        [SerializeField] TMP_Text[] coinRowLabels;
        [SerializeField] TMP_Text[] coinRowValues;

        readonly List<ItemTile> tiles = new();

        void Awake()
        {
            if (lobbyButton != null) lobbyButton.onClick.AddListener(OnLobby);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        }

        public override Awaitable OnOpen(object args)
        {
            var s = Services.Session;
            SetButtons(true);
            if (s == null) return null;
            if (floorText != null) floorText.text = s.floor.ToString();
            if (bossNameText != null)
            {
                bool has = !string.IsNullOrEmpty(s.currentBossNameKey);
                bossNameText.gameObject.SetActive(has);
                if (has) bossNameText.text = DungeonGathering.Localization.LocalizationLookup.Get(s.currentBossNameKey, s.currentBossNameKey);
            }
            if (killsText != null) killsText.text = s.kills.ToString();
            if (timeText != null) timeText.text = $"{(int)s.runTime / 60:00}:{(int)s.runTime % 60:00}";

            // Cùng công thức với RunFlow.Settle (nguồn duy nhất tỉ lệ quy đổi).
            var reward = RunFlow.ComputeReward(s, isVictory);

            if (coinsText != null) coinsText.text = $"+{reward.Total}";

            if (coinRowValues != null && coinRowValues.Length >= 3)
            {
                if (coinRowValues[0] != null) coinRowValues[0].text = $"+{reward.RunCoins}";
                if (coinRowValues[1] != null) coinRowValues[1].text = $"+{reward.FloorBonus}";
                if (coinRowValues[2] != null) coinRowValues[2].text = $"+{reward.BossBonus}";
            }

            if (continueButton != null && isVictory) continueButton.gameObject.SetActive(s.floor < s.maxFloor);
            BuildLoot(s);
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

        void OnLobby()
        {
            SetButtons(false);
            if (!RunFlow.ReturnToLobby()) SetButtons(true); // đang chuyển scene → mở khóa lại để bấm lại
        }

        void OnContinue()
        {
            SetButtons(false);
            if (isVictory) RunFlow.ContinueNextFloor();
            else RunFlow.Retry();
        }

        void SetButtons(bool on)
        {
            if (lobbyButton != null) lobbyButton.interactable = on;
            if (continueButton != null) continueButton.interactable = on;
        }

        // Màn kết quả không đóng bằng Back/Esc.
        public override bool OnBack() => true;
    }
}
