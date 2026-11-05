using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.Lobby
{
    /// <summary>
    /// Character Panel (ui/panel/character theo UI-Lobby.dc.html):
    /// Mở trên UILayer.Panel khi người chơi click nhân vật trong sảnh.
    /// Vị trí ContentWindow tự động bám mép đối diện nhân vật (adaptive left/right docking):
    /// - heroScreenX &lt; 640: neo mép phải (1, 0.5), pos (-24, -30)
    /// - heroScreenX &gt;= 640: neo mép trái (0, 0.5), pos (24, -30)
    /// Hiển thị chỉ số, trang bị, vũ khí, kỹ năng, và các nút Thao tác/Mở khóa.
    /// </summary>
    public sealed class CharacterPanel : UIScreen
    {
        [Header("Window & Docking")]
        [SerializeField] RectTransform contentWindow;
        [SerializeField] Button closeButton;

        [Header("Header")]
        [SerializeField] Image avatarImage;
        [Tooltip("Khung cho phần có hình của portrait (đơn vị UI) — portrait mọi sheet hiển thị cùng cỡ")]
        [SerializeField] Vector2 avatarBox = new(80f, 80f);
        [SerializeField] Vector2 avatarCenter = new(0f, -2f);
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text roleText;
        [SerializeField] Image roleIcon;
        [SerializeField] TMP_Text skillNameText;

        [Header("Description")]
        [SerializeField] TMP_Text descriptionText;

        [Header("Stats Bars")]
        [SerializeField] TMP_Text hpText;
        [SerializeField] Image hpFill;
        [SerializeField] TMP_Text armorText;
        [SerializeField] Image armorFill;
        [SerializeField] TMP_Text manaText;
        [SerializeField] Image manaFill;
        [SerializeField] TMP_Text damageText;
        [SerializeField] Image damageFill;

        [Header("Active Skills Summary")]
        [SerializeField] TMP_Text activeSkillsSummaryText;

        // Hàng ô xem trước loadout (thiết kế: 6 ô trang bị + 2 ô vũ khí, ItemTileSmall, không chữ rarity)
        [SerializeField] ItemTile[] equipTiles = new ItemTile[LoadoutData.EquipmentSlots];
        [SerializeField] ItemTile[] weaponTiles = new ItemTile[LoadoutData.WeaponSlots];
        [SerializeField] Sprite[] slotSilhouettes = new Sprite[LoadoutData.EquipmentSlots];

        [Header("Playable Group")]
        [SerializeField] GameObject playableGroup;
        [SerializeField] Button equipButton;
        [SerializeField] Button weaponsButton;
        [SerializeField] Button enterPortalButton;

        [Header("Locked Group")]
        [SerializeField] GameObject lockedGroup;
        [SerializeField] Button unlockButton;
        [SerializeField] TMP_Text unlockPriceText;

        [Header("Coming Soon Group (CharacterData.released = false)")]
        [SerializeField] GameObject comingSoonGroup;

        [Header("Role Icons")]
        [SerializeField] Sprite warriorIcon;
        [SerializeField] Sprite rangerIcon;
        [SerializeField] Sprite mageIcon;
        [SerializeField] Sprite assassinIcon;

        string currentCharacterId = "knight";

        public string CurrentCharacterId => currentCharacterId;

        void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (equipButton != null) equipButton.onClick.AddListener(OnOpenEquip);
            if (weaponsButton != null) weaponsButton.onClick.AddListener(OnOpenWeapons);
            if (enterPortalButton != null) enterPortalButton.onClick.AddListener(OnEnterPortal);
            if (unlockButton != null) unlockButton.onClick.AddListener(OnUnlock);
        }

        void OnEnable()
        {
            EventBus<EquipmentChangedEvent>.Subscribe(OnEquipmentChanged);
        }

        void OnDisable()
        {
            EventBus<EquipmentChangedEvent>.Unsubscribe(OnEquipmentChanged);
        }

        void OnEquipmentChanged(EquipmentChangedEvent e)
        {
            if (e.CharacterId == currentCharacterId)
            {
                RefreshDisplay();
            }
        }

        public override Awaitable OnOpen(object args)
        {
            if (args is string id && !string.IsNullOrEmpty(id))
            {
                currentCharacterId = id;
            }
            else if (LobbyEntry.Current != null && !string.IsNullOrEmpty(LobbyEntry.Current.SelectedCharacterId))
            {
                currentCharacterId = LobbyEntry.Current.SelectedCharacterId;
            }

            ComputeAdaptivePlacement();
            RefreshDisplay();
            return null;
        }

        public override void OnReopen(object args)
        {
            if (args is string id && !string.IsNullOrEmpty(id))
            {
                currentCharacterId = id;
            }
            ComputeAdaptivePlacement();
            RefreshDisplay();
        }

        public override bool OnBack()
        {
            CloseSelf();
            return true;
        }

        public void ComputeAdaptivePlacement()
        {
            float heroScreenX = -1f;

            // 1. Tìm vị trí nhân vật trong thế giới qua CharacterSpot
            var spots = FindObjectsByType<CharacterSpot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in spots)
            {
                if (s.CharacterId == currentCharacterId)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 screenPos = cam.WorldToScreenPoint(s.transform.position);
                        heroScreenX = (screenPos.x / Screen.width) * 1280f;
                    }
                    break;
                }
            }

            // 2. Fallback nếu không có camera hoặc trong unit test: lấy tọa độ thiết kế canvas
            if (heroScreenX < 0f)
            {
                heroScreenX = currentCharacterId switch
                {
                    "knight" => 352f,
                    "ranger" => 520f,
                    "rogue" => 200f,
                    "wizard" => 900f,
                    "paladin" => 1070f,
                    _ => 352f
                };
            }

            UpdateAdaptivePlacement(heroScreenX);
        }

        public void UpdateAdaptivePlacement(float heroScreenX)
        {
            if (contentWindow == null) return;

            if (heroScreenX < 640f)
            {
                // Tướng ở nửa trái -> Panel neo ở nửa phải
                contentWindow.anchorMin = new Vector2(1f, 0.5f);
                contentWindow.anchorMax = new Vector2(1f, 0.5f);
                contentWindow.pivot = new Vector2(1f, 0.5f);
                contentWindow.anchoredPosition = new Vector2(-24f, -30f);
            }
            else
            {
                // Tướng ở nửa phải -> Panel neo ở nửa trái
                contentWindow.anchorMin = new Vector2(0f, 0.5f);
                contentWindow.anchorMax = new Vector2(0f, 0.5f);
                contentWindow.pivot = new Vector2(0f, 0.5f);
                contentWindow.anchoredPosition = new Vector2(24f, -30f);
            }
        }

        public CharacterData GetCharacterData()
        {
            if (LobbyEntry.Current != null)
            {
                var cd = LobbyEntry.Current.GetCharacter(currentCharacterId);
                if (cd != null) return cd;
            }
            if (ItemCatalog.Current != null)
            {
                return ItemCatalog.Current.GetCharacter(currentCharacterId);
            }
            return null;
        }

        public void RefreshDisplay()
        {
            var character = GetCharacterData();
            bool isUnlocked = Services.Save != null ? Services.Save.Data.IsUnlocked(currentCharacterId) : true;
            bool released = character == null || character.released;

            if (character != null)
            {
                if (avatarImage != null && character.portrait != null)
                    SpriteContentFit.Fit(avatarImage, character.portrait, avatarBox, avatarCenter); // canh theo phần có hình, không theo cả ô sprite

                if (nameText != null)
                {
                    string fallback = char.ToUpperInvariant(character.characterId[0]) + character.characterId.Substring(1);
                    nameText.text = !string.IsNullOrEmpty(character.nameKey)
                        ? DungeonGathering.Localization.LocalizationLookup.Get(character.nameKey, fallback) : fallback;
                }

                if (roleText != null)
                    roleText.text = character.role.ToString().ToUpperInvariant();

                if (roleIcon != null)
                {
                    var icon = GetRoleIcon(character.role);
                    roleIcon.sprite = icon;
                    roleIcon.enabled = icon != null;
                }

                if (skillNameText != null)
                    skillNameText.text = character.activeSkill != null ? (!string.IsNullOrEmpty(character.activeSkill.nameKey) ? DungeonGathering.Localization.LocalizationLookup.Get(character.activeSkill.nameKey) : character.activeSkill.skillId) : string.Empty;

                if (descriptionText != null)
                    descriptionText.text = !string.IsNullOrEmpty(character.descKey) ? DungeonGathering.Localization.LocalizationLookup.Get(character.descKey) : string.Empty;

                // Chỉ số — thang của code (Player ~4–8 máu kiểu Soul Knight). Sát thương = vũ khí khởi đầu × hệ số nhân vật.
                var stats = character.baseStats;
                float dmg = character.startingWeapon != null ? character.startingWeapon.damage * stats.attackDamage : stats.attackDamage * 13f;
                UpdateBar(hpText, hpFill, stats.maxHp, 10f);
                UpdateBar(armorText, armorFill, stats.maxArmor, 10f);
                UpdateBar(manaText, manaFill, stats.maxMana, 250f);
                UpdateBar(damageText, damageFill, dmg, 40f);

                if (activeSkillsSummaryText != null)
                {
                    int activeCount = character.activeSkill != null ? 1 : 0;
                    string skillName = character.activeSkill != null ? (!string.IsNullOrEmpty(character.activeSkill.nameKey) ? DungeonGathering.Localization.LocalizationLookup.Get(character.activeSkill.nameKey) : character.activeSkill.skillId) : DungeonGathering.Localization.LocalizationLookup.Get("lobby.no_active", "-");
                    activeSkillsSummaryText.text = $"{DungeonGathering.Localization.LocalizationLookup.Get("common.active", "Active")} <color=#9FD0FF>{activeCount}/2</color> · {skillName}";
                }

                if (unlockPriceText != null)
                    unlockPriceText.text = character.unlockPrice.ToString("N0");
            }

            RefreshLoadoutTiles();
            // Chưa ra mắt: chỉ xem giới thiệu — ẩn cả nút chơi lẫn nút mở khóa.
            if (playableGroup != null) playableGroup.SetActive(released && isUnlocked);
            if (lockedGroup != null) lockedGroup.SetActive(released && !isUnlocked);
            if (comingSoonGroup != null) comingSoonGroup.SetActive(!released);
        }

        void RefreshLoadoutTiles()
        {
            var loadout = Services.Save != null ? Services.Save.Data.GetCharacter(currentCharacterId, false)?.loadout : null;
            var catalog = ItemCatalog.Current;
            var character = GetCharacterData();
            for (int i = 0; i < equipTiles.Length; i++)
                Fill(equipTiles[i], loadout?.GetEquipment((EquipmentSlotType)i), i < slotSilhouettes.Length ? slotSilhouettes[i] : null, catalog);

            string w0 = loadout?.GetWeapon(0);
            if (string.IsNullOrEmpty(w0) && character != null && character.startingWeapon != null)
                w0 = character.startingWeapon.id;

            string w1 = loadout?.GetWeapon(1);
            // Khử trùng lặp: nếu ô 1 có id trùng với ô 0 thì coi như ô 1 trống
            if (w1 == w0) w1 = null;

            if (weaponTiles != null)
            {
                if (weaponTiles.Length > 0) Fill(weaponTiles[0], w0, null, catalog);
                if (weaponTiles.Length > 1) Fill(weaponTiles[1], w1, null, catalog);
            }
        }

        static void Fill(ItemTile tile, string id, Sprite silhouette, ItemCatalog catalog)
        {
            if (tile == null) return;
            var item = !string.IsNullOrEmpty(id) && catalog != null ? catalog.GetItem(id) : null;
            if (item != null) tile.SetItem(item.icon, item.rarity);
            else tile.SetEmpty(silhouette);
        }

        static void UpdateBar(TMP_Text text, Image fill, float value, float maxScale)
        {
            if (text != null) text.text = value.ToString("0");
            if (fill != null) fill.fillAmount = Mathf.Clamp01(value / maxScale);
        }

        Sprite GetRoleIcon(CharacterRole role) => role switch
        {
            CharacterRole.Warrior => warriorIcon,
            CharacterRole.Ranger => rangerIcon,
            CharacterRole.Mage => mageIcon,
            CharacterRole.Assassin => assassinIcon,
            _ => warriorIcon
        };

        void OnCloseClicked() => CloseSelf();

        void OnOpenEquip()
        {
            if (Services.UI != null && AddressablesUtil.KeyExists(AddressKeys.UiScreenEquipment))
                _ = Services.UI.Open<UIScreen>(AddressKeys.UiScreenEquipment, currentCharacterId);
        }

        void OnOpenWeapons()
        {
            if (Services.UI != null && AddressablesUtil.KeyExists(AddressKeys.UiScreenWeapon))
                _ = Services.UI.Open<UIScreen>(AddressKeys.UiScreenWeapon, currentCharacterId);
        }

        void OnEnterPortal()
        {
            if (Services.Scenes != null && Services.Scenes.IsTransitioning) return;
            if (LobbyEntry.Current != null)
            {
                LobbyEntry.Current.StartRun();
            }
        }

        void OnUnlock()
        {
            var character = GetCharacterData();
            if (character == null) return;
            var res = Economy.UnlockCharacter(character);
            Sfx.Play(res == PurchaseResult.Ok ? SfxId.UiPurchase : SfxId.UiError);
            if (res == PurchaseResult.Ok)
            {
                RefreshDisplay();
            }
        }
    }
}
