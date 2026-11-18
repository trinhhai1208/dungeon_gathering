using System.Collections.Generic;
using DG.Tweening;
using DungeonGathering.Catalog;
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
        [SerializeField] SegmentedBar hpSegments;
        [SerializeField] SegmentedBar armorSegments;
        [SerializeField] SegmentedBar manaSegments;
        [SerializeField] int manaPerSegment = 20;
        [SerializeField] TMP_Text hpText;
        [SerializeField] TMP_Text armorText;
        [SerializeField] TMP_Text manaText;

        [Header("Run Stats (Top-Left: 10, -112)")]
        [SerializeField] TMP_Text coinText;
        [SerializeField] LocalizedTMPText floorLabel;
        [SerializeField] string floorKey = "hud.floor";

        [Header("Minimap & Pause (Top-Right)")]
        [SerializeField] RectTransform minimapFrame;
        [SerializeField] MinimapView minimapView;
        [SerializeField] Button pauseButton;

        [Header("Boss Bar")]
        [SerializeField] GameObject bossBar;
        [SerializeField] Image bossFill;
        [SerializeField] SegmentedBar bossSegments;
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

            if (coinText != null) coinText.transform.DOKill();

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
                if (floorLabel != null) floorLabel.SetKey(floorKey, s.floor); // "TẦNG {0}" — bắt đầu từ Tầng 1
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
                if (src1 != null) skillSlot1.SetItem(src1.Icon, src1.Rank);
                else skillSlot1.SetEmpty();
            }

            if (skillSlot2 != null)
            {
                var src2 = player.Skills.GetSource(2);
                if (src2 != null) skillSlot2.SetItem(src2.Icon, src2.Rank);
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
            int cost = cs != null ? Mathf.CeilToInt(SkillRegistry.ManaCost(cs)) : 0;
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

            if (hpSegments != null) hpSegments.SetValue(e.Hp, e.MaxHp, 1);
            if (armorSegments != null) armorSegments.SetValue(e.Armor, e.MaxArmor, 1);
            if (manaSegments != null) manaSegments.SetValue(e.Mana, e.MaxMana, manaPerSegment);

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
            if (coinText != null)
            {
                coinText.text = e.Coins.ToString();
                coinText.transform.DOKill();
                coinText.transform.localScale = Vector3.one;
                coinText.transform.DOPunchScale(new Vector3(0.35f, 0.35f, 0f), 0.2f, 1, 0.5f);
            }
        }

        public void OnWeaponSwapped(WeaponSwappedEvent e) => RefreshWeapon();

        void RefreshWeapon()
        {
            if (player == null || player.Weapons == null) return;
            var w = player.Weapons.Active;
            if (currentWeaponIcon != null)
            {
                currentWeaponIcon.enabled = w != null;
                if (w != null) currentWeaponIcon.sprite = w.Spec.weaponSprite != null ? w.Spec.weaponSprite : w.Icon;
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
            if (bossSegments != null)
                bossSegments.SetNormalized(e.Max > 0 ? (float)e.Current / e.Max : 0f, 10);
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
            if (item == null || string.IsNullOrEmpty(item.NameKey)) toast.SetRaw(e.ItemId);
            else toast.SetKey(item.NameKey);
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

        void EnsureMinimapView()
        {
            if (minimapView == null && minimapFrame != null)
                minimapView = minimapFrame.GetComponent<MinimapView>() ?? minimapFrame.gameObject.AddComponent<MinimapView>();
        }

        void BuildMinimap()
        {
            EnsureMinimapView();
            var dir = LevelDirector.Current;
            if (minimapView != null && dir != null)
                minimapView.Build(minimapFrame, dir.Layout);
        }

        void RefreshMinimapColors()
        {
            if (minimapView != null)
                minimapView.RefreshColors(LevelDirector.Current);
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
            float csMana = SkillRegistry.ManaCost(cs);
            bool hasMana = csMana <= 0f || player.Stats.Mana >= csMana;
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
