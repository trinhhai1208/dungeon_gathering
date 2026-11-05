using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using DungeonGathering.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Play HUD Screen controller (UI-HUD, address ui/hud/play, layer Hud).
    /// Displays player stats, run stats (coins, floor), minimap, pause button, and combat action controls.
    /// Interacts with the game through EventBus and VirtualInput.
    /// </summary>
    public class PlayHud : UIScreen
    {
        [Header("Player Status (Top-Left: 10, -10)")]
        [SerializeField] Image avatarImage;
        [SerializeField] Image hpFill;
        [SerializeField] Image armorFill;
        [SerializeField] Image manaFill;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text armorText;
        [SerializeField] TMP_Text manaText;

        [Header("Run Stats (Top-Left: 10, -112)")]
        [SerializeField] TMP_Text coinText;
        [SerializeField] LocalizedTMPText floorLabel;
        [SerializeField] string floorKey = "hud.floor";

        [Header("Minimap & Pause (Top-Right)")]
        [SerializeField] RectTransform minimapFrame;
        [SerializeField] Button pauseButton;

        [Header("Boss Bar")]
        [SerializeField] GameObject bossBar;
        [SerializeField] Image bossFill;
        [SerializeField] LocalizedTMPText bossName;

        [Header("Action Cluster (Bottom-Right: -28, 28)")]
        [SerializeField] Button attackButton;
        [SerializeField] Button swapWeaponButton;
        [SerializeField] Image currentWeaponIcon;
        [SerializeField] ItemTile skillSlot1;
        [SerializeField] ItemTile skillSlot2;
        [SerializeField] Image characterSkillIcon;
        [SerializeField] Image characterSkillCooldown;
        [SerializeField] TMP_Text characterSkillCooldownText;
        [SerializeField] Image characterSkillGlow;
        [SerializeField] GameObject characterSkillManaBadge;
        [SerializeField] TMP_Text characterSkillManaText;
        [SerializeField] Color skillNoManaTint = new(0.45f, 0.5f, 0.75f, 1f);

        [Header("Interact")]
        [SerializeField] GameObject interactButton;
        [SerializeField] LocalizedTMPText interactPrompt;

        [Header("Toast / Pickup Feedback")]
        [SerializeField] LocalizedTMPText toast;
        [SerializeField] float toastDuration = 2f;

        PlayerController player;
        float toastUntil;
        int lastCooldownSec = int.MinValue;
        string lastBossKey;
        IInteractable lastInteract;
        readonly Dictionary<int, Image> minimapRooms = new();
        RectTransform minimapContainer;

        public override Awaitable OnOpen(object args)
        {
            player = LevelDirector.Current != null ? LevelDirector.Current.Player : null;
            ReadAll();
            return null;
        }

        void OnEnable()
        {
            EventBus<PlayerStatsChangedEvent>.Subscribe(OnStats);
            EventBus<RunCoinsChangedEvent>.Subscribe(OnCoins);
            EventBus<WeaponSwappedEvent>.Subscribe(OnWeaponSwapped);
            EventBus<BossHealthChangedEvent>.Subscribe(OnBossHealth);
            EventBus<BossDefeatedEvent>.Subscribe(OnBossDefeated);
            EventBus<ActiveSkillUsedEvent>.Subscribe(OnSkillUsed);
            EventBus<ItemPickedUpEvent>.Subscribe(OnItemPicked);
            EventBus<RunFinishedEvent>.Subscribe(OnRunFinished);
            EventBus<RoomEnteredEvent>.Subscribe(OnRoomEvent);
            EventBus<RoomClearedEvent>.Subscribe(OnRoomClearedEvent);
            if (LevelDirector.Current != null) LevelDirector.Current.RoomEntered += OnDirectorRoomEntered;

            if (pauseButton != null) pauseButton.onClick.AddListener(OnPauseClicked);
            // swapWeaponButton đã được VirtualButton xử lý trên PointerDown, không gán thêm onClick để tránh gọi 2 lần/lượt bấm
            if (skillSlot1 != null) skillSlot1.Clicked += OnSkill1Clicked;
            if (skillSlot2 != null) skillSlot2.Clicked += OnSkill2Clicked;
        }

        void OnDisable()
        {
            EventBus<PlayerStatsChangedEvent>.Unsubscribe(OnStats);
            EventBus<RunCoinsChangedEvent>.Unsubscribe(OnCoins);
            EventBus<WeaponSwappedEvent>.Unsubscribe(OnWeaponSwapped);
            EventBus<BossHealthChangedEvent>.Unsubscribe(OnBossHealth);
            EventBus<BossDefeatedEvent>.Unsubscribe(OnBossDefeated);
            EventBus<ActiveSkillUsedEvent>.Unsubscribe(OnSkillUsed);
            EventBus<ItemPickedUpEvent>.Unsubscribe(OnItemPicked);
            EventBus<RunFinishedEvent>.Unsubscribe(OnRunFinished);
            EventBus<RoomEnteredEvent>.Unsubscribe(OnRoomEvent);
            EventBus<RoomClearedEvent>.Unsubscribe(OnRoomClearedEvent);
            if (LevelDirector.Current != null) LevelDirector.Current.RoomEntered -= OnDirectorRoomEntered;

            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPauseClicked);
            if (skillSlot1 != null) skillSlot1.Clicked -= OnSkill1Clicked;
            if (skillSlot2 != null) skillSlot2.Clicked -= OnSkill2Clicked;

            VirtualInput.ResetState();
        }

        void ReadAll()
        {
            if (bossBar != null) bossBar.SetActive(false);
            if (toast != null) toast.gameObject.SetActive(false);
            if (interactButton != null) interactButton.SetActive(false);
            lastInteract = null;

            var s = Services.Session;
            if (s != null)
            {
                OnCoins(new RunCoinsChangedEvent { Coins = s.runCoins });
                if (floorLabel != null) floorLabel.SetKey(floorKey, s.floor, s.maxFloor); // "TẦNG {0}/{1}"
            }

            if (skillSlot1 != null) skillSlot1.SetShowRarityLetter(false);
            if (skillSlot2 != null) skillSlot2.SetShowRarityLetter(false);

            BuildMinimap();

            if (player == null) return;

            OnStats(player.Stats.Current);
            if (avatarImage != null && player.Character != null && player.Character.portrait != null)
                avatarImage.sprite = player.Character.portrait;

            RefreshWeapon();

            if (skillSlot1 != null)
            {
                var src1 = player.Skills.GetSource(1);
                if (src1 != null) skillSlot1.SetItem(src1.icon, src1.rarity);
                else skillSlot1.SetEmpty();
            }

            if (skillSlot2 != null)
            {
                var src2 = player.Skills.GetSource(2);
                if (src2 != null) skillSlot2.SetItem(src2.icon, src2.rarity);
                else skillSlot2.SetEmpty();
            }

            var cs = player.Skills.GetSkill(0);
            if (characterSkillIcon != null)
            {
                characterSkillIcon.enabled = cs != null && cs.icon != null;
                if (cs != null && cs.icon != null) characterSkillIcon.sprite = cs.icon;
            }
            if (characterSkillCooldown != null) characterSkillCooldown.enabled = false;
            if (characterSkillCooldownText != null) characterSkillCooldownText.enabled = false;
            int cost = cs != null ? Mathf.CeilToInt(cs.manaCost) : 0;
            if (characterSkillManaBadge != null) characterSkillManaBadge.SetActive(cost > 0);
            if (characterSkillManaText != null) characterSkillManaText.text = cost.ToString();
            if (characterSkillGlow != null) characterSkillGlow.color = new Color(1f, 1f, 1f, 0f);
            lastCooldownSec = int.MinValue;
        }

        public void OnStats(PlayerStatsChangedEvent e)
        {
            Fill(hpFill, e.Hp, e.MaxHp);
            Fill(armorFill, e.Armor, e.MaxArmor);
            Fill(manaFill, e.Mana, e.MaxMana);

            if (hpText != null) hpText.text = $"{e.Hp}/{e.MaxHp}";
            if (armorText != null) armorText.text = $"{e.Armor}/{e.MaxArmor}";
            if (manaText != null) manaText.text = $"{e.Mana}/{e.MaxMana}";
        }

        static void Fill(Image img, int cur, int max)
        {
            if (img != null) img.fillAmount = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
        }

        public void OnCoins(RunCoinsChangedEvent e)
        {
            if (coinText != null) coinText.text = e.Coins.ToString();
        }

        public void OnWeaponSwapped(WeaponSwappedEvent e) => RefreshWeapon();

        void RefreshWeapon()
        {
            if (player == null || player.Weapons == null) return;
            var w = player.Weapons.Active;
            if (currentWeaponIcon != null)
            {
                currentWeaponIcon.enabled = w != null;
                if (w != null) currentWeaponIcon.sprite = w.weaponSprite != null ? w.weaponSprite : w.icon;
            }
            if (swapWeaponButton != null)
            {
                bool canSwap = player.Weapons.Get(0) != null && player.Weapons.Get(1) != null;
                swapWeaponButton.interactable = canSwap;
            }
        }

        public void OnBossHealth(BossHealthChangedEvent e)
        {
            if (bossBar == null) return;
            bossBar.SetActive(e.Current > 0);
            Fill(bossFill, e.Current, e.Max);
            if (bossName != null && e.BossName != lastBossKey)
            {
                lastBossKey = e.BossName;
                bossName.SetKey(e.BossName);
            }
        }

        public void OnBossDefeated(BossDefeatedEvent e)
        {
            if (bossBar != null) bossBar.SetActive(false);
        }

        public void OnSkillUsed(ActiveSkillUsedEvent e)
        {
            if (e.SlotIndex == 1 && skillSlot1 != null)
                skillSlot1.SetCooldown(e.Cooldown, e.Cooldown);
            else if (e.SlotIndex == 2 && skillSlot2 != null)
                skillSlot2.SetCooldown(e.Cooldown, e.Cooldown);
        }

        public void OnItemPicked(ItemPickedUpEvent e)
        {
            if (toast == null) return;
            var item = Catalog.ItemCatalog.Current != null ? Catalog.ItemCatalog.Current.GetItem(e.ItemId) : null;
            if (item == null || string.IsNullOrEmpty(item.nameKey)) toast.SetRaw(e.ItemId);
            else toast.SetKey(item.nameKey);
            toast.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + toastDuration;
        }

        public void OnRunFinished(RunFinishedEvent e)
        {
            if (bossBar != null && e.Victory) bossBar.SetActive(false);
        }

        void OnRoomEvent(RoomEnteredEvent e) => RefreshMinimapColors();
        void OnRoomClearedEvent(RoomClearedEvent e) => RefreshMinimapColors();
        void OnDirectorRoomEntered(RoomController r) => RefreshMinimapColors();

        void BuildMinimap()
        {
            minimapRooms.Clear();
            if (minimapFrame == null) return;
            if (minimapContainer != null) Destroy(minimapContainer.gameObject);

            var dir = LevelDirector.Current;
            var layout = dir != null ? dir.Layout : null;
            if (layout == null || layout.Rooms.Count == 0) return;

            var go = new GameObject("MapContent", typeof(RectTransform));
            go.layer = minimapFrame.gameObject.layer;
            minimapContainer = (RectTransform)go.transform;
            minimapContainer.SetParent(minimapFrame, false);
            minimapContainer.anchorMin = minimapContainer.anchorMax = minimapContainer.pivot = new Vector2(0.5f, 0.5f);
            minimapContainer.anchoredPosition = Vector2.zero;
            minimapContainer.sizeDelta = minimapFrame.sizeDelta - new Vector2(24f, 24f);

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var r in layout.Rooms)
            {
                if (r.Cell.x < minX) minX = r.Cell.x;
                if (r.Cell.x > maxX) maxX = r.Cell.x;
                if (r.Cell.y < minY) minY = r.Cell.y;
                if (r.Cell.y > maxY) maxY = r.Cell.y;
            }
            float spanX = Mathf.Max(1, maxX - minX + 1);
            float spanY = Mathf.Max(1, maxY - minY + 1);
            Vector2 area = minimapContainer.sizeDelta;
            float step = Mathf.Min(area.x / spanX, area.y / spanY);
            step = Mathf.Clamp(step, 12f, 26f);
            float roomW = step * 0.72f;
            float roomH = step * 0.62f;
            Vector2 centerCell = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            // Vẽ đường nối giữa các phòng
            foreach (var (a, b, _) in layout.Links())
            {
                Vector2 pa = ((Vector2)a.Cell - centerCell) * step;
                Vector2 pb = ((Vector2)b.Cell - centerCell) * step;
                var lgo = new GameObject($"Link_{a.Id}_{b.Id}", typeof(RectTransform), typeof(Image));
                lgo.layer = go.layer;
                var lrt = (RectTransform)lgo.transform;
                lrt.SetParent(minimapContainer, false);
                lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.anchoredPosition = (pa + pb) * 0.5f;
                bool horiz = a.Cell.y == b.Cell.y;
                lrt.sizeDelta = horiz ? new Vector2(step, 3f) : new Vector2(3f, step);
                var limg = lgo.GetComponent<Image>();
                limg.color = new Color32(0x6e, 0x66, 0x7c, 0xc8);
                limg.raycastTarget = false;
            }

            // Vẽ từng phòng
            foreach (var r in layout.Rooms)
            {
                Vector2 pos = ((Vector2)r.Cell - centerCell) * step;
                var rgo = new GameObject($"Room_{r.Id}_{r.Type}", typeof(RectTransform), typeof(Image));
                rgo.layer = go.layer;
                var rrt = (RectTransform)rgo.transform;
                rrt.SetParent(minimapContainer, false);
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
                rrt.anchoredPosition = pos;
                rrt.sizeDelta = new Vector2(roomW, roomH);
                var rimg = rgo.GetComponent<Image>();
                rimg.raycastTarget = false;
                minimapRooms[r.Id] = rimg;
            }

            RefreshMinimapColors();
        }

        void RefreshMinimapColors()
        {
            var dir = LevelDirector.Current;
            if (dir == null || dir.Builder == null) return;
            foreach (var rc in dir.Builder.Rooms)
            {
                if (rc == null || !minimapRooms.TryGetValue(rc.RoomId, out var img) || img == null) continue;
                bool isCurrent = dir.CurrentRoom == rc;
                if (isCurrent)
                {
                    img.color = new Color32(0xff, 0xd2, 0x7a, 0xff); // Vàng sáng: phòng hiện tại
                    img.rectTransform.localScale = new Vector3(1.15f, 1.15f, 1f);
                }
                else
                {
                    img.rectTransform.localScale = Vector3.one;
                    img.color = rc.Type switch
                    {
                        RoomType.Boss => new Color32(0xd6, 0x45, 0x50, 0xff),     // Đỏ: Boss
                        RoomType.Treasure => new Color32(0xe6, 0xa8, 0x32, 0xff), // Cam vàng: Kho báu
                        RoomType.Shop => new Color32(0x4e, 0xb8, 0x82, 0xff),     // Xanh ngọc: Shop
                        RoomType.Start => new Color32(0x7a, 0x9e, 0xc9, 0xff),    // Xanh dương: Start
                        _ => rc.IsCleared || rc.Visited
                            ? new Color32(0x9a, 0x94, 0xa8, 0xff)                 // Xám sáng: Đã dọn
                            : new Color32(0x4a, 0x42, 0x58, 0xcc)                 // Tím tối: Chưa vào
                    };
                }
            }
        }

        void Update()
        {
            if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime > toastUntil)
                toast.gameObject.SetActive(false);

            if (player == null) return;

            if (skillSlot1 != null)
                skillSlot1.SetCooldown(player.Skills.GetRemaining(1), player.Skills.GetCooldown(1));
            if (skillSlot2 != null)
                skillSlot2.SetCooldown(player.Skills.GetRemaining(2), player.Skills.GetCooldown(2));

            if (characterSkillCooldown != null)
            {
                float rem = player.Skills.GetRemaining(0), total = player.Skills.GetCooldown(0);
                bool on = rem > 0f && total > 0f;
                characterSkillCooldown.enabled = on;
                characterSkillCooldown.fillAmount = on ? Mathf.Clamp01(rem / total) : 0f;
                if (characterSkillCooldownText != null)
                {
                    characterSkillCooldownText.enabled = on;
                    // Chỉ dựng lại chuỗi khi số giây hiển thị đổi (tránh cấp phát mỗi frame).
                    int sec = on ? Mathf.CeilToInt(rem) : -1;
                    if (sec != lastCooldownSec)
                    {
                        lastCooldownSec = sec;
                        characterSkillCooldownText.text = on ? sec.ToString() : string.Empty;
                    }
                }
            }

            UpdateCharacterSkillState();

            if (interactButton != null && player.Interactor != null)
            {
                var cur = player.Interactor.Current;
                if (!ReferenceEquals(cur, lastInteract))
                {
                    lastInteract = cur;
                    interactButton.SetActive(cur != null);
                    if (cur != null && interactPrompt != null) interactPrompt.SetKey(cur.GetInteractPrompt());
                }
            }
        }

        /// <summary>Vòng sáng nhấp nháy khi skill sẵn sàng; icon ngả xanh xám khi thiếu mana.</summary>
        void UpdateCharacterSkillState()
        {
            var cs = player.Skills.GetSkill(0);
            if (cs == null) return;
            bool cooling = player.Skills.GetRemaining(0) > 0f;
            bool hasMana = cs.manaCost <= 0f || player.Stats.Mana >= cs.manaCost;
            if (characterSkillIcon != null)
                characterSkillIcon.color = hasMana ? Color.white : skillNoManaTint;
            if (characterSkillManaText != null)
                characterSkillManaText.color = hasMana ? new Color32(0x9c, 0xd8, 0xff, 0xff) : new Color32(0xff, 0x6b, 0x6b, 0xff);
            if (characterSkillGlow != null)
            {
                float a = !cooling && hasMana ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
                var c = characterSkillGlow.color;
                if (!Mathf.Approximately(c.a, a)) characterSkillGlow.color = new Color(c.r, c.g, c.b, a);
            }
        }

        // --- Action Triggers (VirtualInput integration) ---
        public void OnPauseClicked() => VirtualInput.PressPause();
        public void OnSwapWeaponClicked() => VirtualInput.PressSwapWeapon();
        public void OnAttackPointerDown() => VirtualInput.AttackHeld = true;
        public void OnAttackPointerUp() => VirtualInput.AttackHeld = false;
        public void OnSkillClicked(int slotIndex) => VirtualInput.PressSkill(slotIndex);
        void OnSkill1Clicked(ItemTile tile) => VirtualInput.PressSkill(1);
        void OnSkill2Clicked(ItemTile tile) => VirtualInput.PressSkill(2);
    }
}
