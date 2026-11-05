using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using DungeonGathering.UI;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Dựng prefab UI theo đúng bản thiết kế (Design/canvas/project/UI-*.dc.html), chỉ dùng sprite của dự án.
    /// Quy ước: 1 đơn vị UI = 1 px thiết kế; frame 9-slice 6px nguồn hiển thị 24/12/8 đơn vị như class f-*, f-*-s, f-*-xs.
    /// Ghi đè prefab cũ tại cùng đường dẫn (giữ GUID/address). Chạy lại bất cứ lúc nào: kết quả giống hệt.
    /// Docs: docs/UI_SYSTEM.md §4 (neo), §5 (luồng), §6 (thành phần).
    /// </summary>
    public static class UIDesignBuilder
    {
        // ---------- nguồn ----------
        const string Sheet = "Assets/Project/Art/Sprites/UI/HUD/Basic Buttons 3 Fv.png";
        const string Items = "Assets/Project/Art/Sprites/Items/spritesheet_16x16.png";
        const string Gen = "Assets/Project/Art/Sprites/UI/Generated/";
        const string UiDir = "Assets/Project/Prefabs/UI/";
        const string FontPath = "Assets/Project/Art/Fonts/PixelifySans-Regular SDF.asset";
        const float RefPpu = 100f;          // CanvasScaler.referencePixelsPerUnit của UIRoot
        const float SrcBorder = 6f;         // viền nguồn của frame (px)

        static readonly Color Ink = new Color32(0x1b, 0x14, 0x20, 0xff);     // chữ tối trên nút sáng
        static readonly Color Paper = new Color32(0xef, 0xe6, 0xdc, 0xff);   // chữ sáng
        static readonly Color Soft = new Color32(0xe6, 0xe9, 0xf2, 0xff);
        static readonly Color Gold = new Color32(0xff, 0xd2, 0x7a, 0xff);
        static readonly Color Title = new Color32(0xf2, 0xb3, 0x3d, 0xff);

        static TMP_FontAsset font;
        static Dictionary<string, string> en;

        // frame & icon (lấy theo tọa độ trên sheet gốc, gốc trên-trái — trùng tọa độ Design/tools/cut*.py)
        static Sprite FrDark => At(Sheet, 240, 70);
        static Sprite FrMid => At(Sheet, 240, 20);
        static Sprite FrLight => At(Sheet, 290, 20);
        static Sprite FrBtn => At(Sheet, 290, 70);
        static Sprite SqF => At(Sheet, 480, 230);
        static Sprite IcBack => At(Sheet, 315, 186);
        static Sprite IcNext => At(Sheet, 283, 186);
        static Sprite IcGear => At(Sheet, 150, 20);
        static Sprite IcPause => At(Sheet, 140, 226);
        static Sprite IcSound => At(Sheet, 25, 194);
        static Sprite IcMusic => At(Sheet, 25, 226);
        static Sprite IcPlay => At(Sheet, 265, 140);
        static Sprite IcSwordBtn => At(Sheet, 190, 190);
        static Sprite IcSwap => At(Sheet, 515, 229);
        static Sprite Coin => Single("Assets/Project/Art/Sprites/Items/Coins/coin_1.png");
        static Sprite Item(int c, int r) => At(Items, c * 16 + 8, r * 16 + 8);
        static Sprite G(string n) => Single(Gen + n + ".png");

        [MenuItem("Tools/Dungeon Gathering/UI/Build Design UI (Menu, Lobby, HUD, Pause)")]
        public static void BuildAll()
        {
            Prepare();
            BuildMenu();
            BuildLobbyHud();
            BuildCharacterPanel();
            BuildPlayHud();
            BuildPause();
            PatchUIRoot();
            AssetDatabase.SaveAssets();
            UIPreviewRenderer.RenderAll();
            Debug.Log("[UIDesign] Đã dựng lại Menu, LobbyHud, CharacterPanel, PlayHud, PauseScreen + Backdrop của UIRoot.");
        }

        // ================= màn =================

        static void BuildMenu()
        {
            var root = Root<Menu.MenuScreen>("MenuScreen", UILayer.Screen);
            Img(root, "Dim", null, A.Stretch, 0, 0, 0, 0).color = new Color(0.106f, 0.078f, 0.125f, 0.15f);

            var coin = Frame(root, "Coin", FrDark, 12, A.TL, 24, 24, 170, 64);
            Icon(coin, "Icon", Coin, 2, A.L, 14, 0);
            var coinText = Txt(coin, "Value", null, "1,240", 28, Paper, TextAlignmentOptions.Left, A.L, 50, 0, 110, 40);

            var settings = IconButton(root, "Settings", IcGear, 3, A.TR, 24, 24, 72, 76);

            var logo = Rt(root, "Logo", A.T, 0, 112, 760, 240);
            var dungeon = Txt(logo, "Dungeon", null, "DUNGEON", 104, Title, TextAlignmentOptions.Center, A.T, 0, 0, 620, 104);
            Shadow(dungeon, new Color32(0x6b, 0x3f, 0x1e, 0xff), 6);
            var torch = Single("Assets/Project/Art/Sprites/Environment/Props/torch/torch_1.png");
            Icon(logo, "TorchL", torch, 4, A.T, -340, 30);
            Icon(logo, "TorchR", torch, 4, A.T, 340, 30);
            var gathering = Txt(logo, "Gathering", null, "GATHERING", 64, Paper, TextAlignmentOptions.Center, A.T, 0, 124, 620, 70);
            gathering.characterSpacing = 18;
            Shadow(gathering, new Color32(0x4d, 0x3a, 0x52, 0xff), 5);

            var row = Rt(root, "Heroes", A.C, 0, -80, 620, 100);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.LowerCenter; h.spacing = 28; h.childControlWidth = h.childControlHeight = false; h.childForceExpandWidth = h.childForceExpandHeight = false;
            string chars = "Assets/Project/Art/Sprites/Characters/";
            foreach (var (s, k) in new[] {
                (G("menu_hero_0"), 4), (G("menu_hero_1"), 4),
                (At(chars + "Knight/Blue Knight idle Sprite-sheet 16x16.png", 8, 8), 6),
                (G("menu_hero_2"), 4), (G("menu_hero_3"), 4) })
                Icon(row, "Hero", s, k, A.C, 0, 0);

            var play = IconButton(root, "Play", IcPlay, 4, A.B, 0, 96, 248, 120);
            var tap = Txt(root, "Tap", "menu.tap", null, 22, new Color32(0xd9, 0xcd, 0xd6, 0xff), TextAlignmentOptions.Center, A.B, 0, 56, 420, 30);
            tap.characterSpacing = 8;

            var sound = IconButton(root, "Sound", IcSound, 2, A.BR, 82, 20, 52, 56);
            var music = IconButton(root, "Music", IcMusic, 2, A.BR, 24, 20, 52, 56);

            Wire(root.GetComponent<Menu.MenuScreen>(), ("playButton", play), ("settingsButton", settings), ("soundButton", sound), ("musicButton", music),
                ("soundIcon", sound.transform.GetChild(0).GetComponent<Image>()), ("musicIcon", music.transform.GetChild(0).GetComponent<Image>()), ("coinText", coinText));
            Save(root, UiDir + "MenuScreen.prefab", AddressKeys.UiScreenMenu);
        }

        static void BuildLobbyHud()
        {
            var root = Root<Lobby.LobbyHud>("LobbyHud", UILayer.Hud);
            var back = IconButton(root, "Back", IcBack, 2, A.TL, 20, 14, 64, 64);
            var coin = Frame(root, "Coin", FrDark, 12, A.TR, 20, 14, 150, 56);
            Icon(coin, "Icon", Coin, 2, A.L, 10, 0);
            var coinText = Txt(coin, "Value", null, "1,240", 22, Paper, TextAlignmentOptions.Left, A.L, 42, 0, 96, 32);
            var hint = Frame(root, "HintPickHero", FrDark, 12, A.B, 0, 22, 560, 60);
            var ht = Txt(hint, "Text", "lobby.pick_char", null, 26, Paper, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            ht.characterSpacing = 4;
            Wire(root.GetComponent<Lobby.LobbyHud>(), ("backButton", back), ("coinText", coinText), ("hintPickHero", hint.gameObject));
            Save(root, UiDir + "LobbyHud.prefab", AddressKeys.UiHudLobby);
        }

        static void BuildCharacterPanel()
        {
            var root = Root<Lobby.CharacterPanel>("CharacterPanel", UILayer.Screen); // Screen (không phải Panel): Trang bị/Vũ khí mở chồng lên, Back quay về panel
            var close = IconButton(root, "Close", IcBack, 2, A.TL, 20, 14, 64, 64);
            var win = Frame(root, "Window", FrDark, 24, A.R, 24, -30, 470, 620);
            win.GetComponent<Image>().raycastTarget = true;
            var inner = Rt(win, "Inner", A.Stretch, 28, 24, 28, 24); // 414 × 572

            var slot = Frame(inner, "AvatarSlot", SqF, 12, A.TL, 0, 0, 104, 104);
            var avatar = Img(slot, "Avatar", At("Assets/Project/Art/Sprites/Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png", 8, 8), A.C, 0, -2, 64, 64); avatar.preserveAspect = true;
            var name = Txt(inner, "Name", null, "Knight", 32, Paper, TextAlignmentOptions.Left, A.TL, 118, 2, 296, 36);
            var roleIcon = Img(inner, "RoleIcon", Item(5, 18), A.TL, 118, 44, 18, 18); roleIcon.preserveAspect = true;
            var role = Txt(inner, "Role", null, "WARRIOR", 12, Paper, TextAlignmentOptions.Left, A.TL, 142, 44, 200, 18);
            role.characterSpacing = 4;
            var skill = Txt(inner, "Skill", null, "Dual Wield · 12s cooldown", 13, Gold, TextAlignmentOptions.Left, A.TL, 118, 66, 296, 20);
            var desc = Txt(inner, "Desc", null, En("char.knight.desc"), 13, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 112, 414, 40);
            desc.textWrappingMode = TextWrappingModes.Normal;

            var bars = new (string key, string strip, string tf, string ff)[] {
                ("stat.hp", "bar_strip_red", "hpText", "hpFill"), ("stat.armor", "bar_strip_grey", "armorText", "armorFill"),
                ("stat.mana", "bar_strip_blue", "manaText", "manaFill"), ("stat.dmg", "bar_strip_green", "damageText", "damageFill") };
            var refs = new List<(string, Object)>();
            for (int i = 0; i < bars.Length; i++)
            {
                float x = (i % 2) * 213, y = 160 + (i / 2) * 34;
                Txt(inner, "Label_" + i, bars[i].key, null, 12, Paper, TextAlignmentOptions.Left, A.TL, x, y + 6, 70, 16);
                var (fill, value) = Bar(inner, "Bar_" + i, bars[i].strip, A.TL, x + 70, y, 131, 28, 8, 13, "80");
                refs.Add((bars[i].tf, value)); refs.Add((bars[i].ff, fill));
            }

            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTileSmall.prefab");
            var equipTiles = new Object[6]; var weaponTiles = new Object[2];
            for (int i = 0; i < 6; i++) equipTiles[i] = Tile(tilePrefab, inner, "Equip_" + i, A.TL, i * 52, 236, 52, 54, false);
            for (int i = 0; i < 2; i++) weaponTiles[i] = Tile(tilePrefab, inner, "Weapon_" + i, A.TL, 318 + i * 52, 236, 52, 54, false);

            var summary = Txt(inner, "ActiveSummary", null, "Active <color=#9FD0FF>2/2</color> · Frost Field · Arcane Storm", 13, Soft, TextAlignmentOptions.Left, A.TL, 0, 302, 414, 20);

            var playable = Rt(inner, "Playable", A.Stretch, 0, 0, 0, 0);
            var equip = TextButton(playable, "Equip", FrBtn, 24, "common.equip", 20, Ink, A.TL, 0, 410, 202, 76, Item(12, 11), 2);
            var weapons = TextButton(playable, "Weapons", FrBtn, 24, "common.weapons", 20, Ink, A.TL, 212, 410, 202, 76, At("Assets/Project/Art/Sprites/Items/Weapon/Sword/File.png", 16, 144), 1);
            var enter = TextButton(playable, "EnterPortal", FrLight, 24, "lobby.enter", 24, Ink, A.TL, 0, 496, 414, 76, null, 0);
            Icon(enter, "Arrow", IcNext, 1, A.R, 22, 0);

            var locked = Rt(inner, "Locked", A.Stretch, 0, 0, 0, 0);
            var lockedText = Txt(locked, "Text", "lobby.locked", null, 13, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 410, 414, 40);
            lockedText.textWrappingMode = TextWrappingModes.Normal;
            var unlock = TextButton(locked, "Unlock", FrLight, 24, "lobby.unlock", 24, Ink, A.TL, 0, 496, 414, 76, null, 0);
            ((RectTransform)unlock.transform.Find("Label")).anchoredPosition = new Vector2(-60, 0);
            Icon(unlock, "Coin", Coin, 2, A.C, 60, 0);
            var price = Txt(unlock, "Price", null, "800", 24, Ink, TextAlignmentOptions.Left, A.C, 140, 0, 100, 32);
            locked.gameObject.SetActive(false);

            // Chưa ra mắt (CharacterData.released = false): chỉ xem giới thiệu, không nút chơi / mở khóa.
            var soon = Rt(inner, "ComingSoon", A.Stretch, 0, 0, 0, 0);
            var soonHint = Txt(soon, "Hint", "char.coming_soon_hint", null, 13, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 410, 414, 60);
            soonHint.textWrappingMode = TextWrappingModes.Normal;
            var soonBadge = Frame(soon, "Badge", FrMid, 24, A.TL, 0, 496, 414, 76);
            var soonTitle = Txt(soonBadge, "Title", "char.coming_soon", null, 26, Gold, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            soonTitle.characterSpacing = 6;
            soon.gameObject.SetActive(false);

            var panel = root.GetComponent<Lobby.CharacterPanel>();
            refs.AddRange(new (string, Object)[] {
                ("contentWindow", win), ("closeButton", close), ("avatarImage", avatar), ("nameText", name), ("roleText", role), ("roleIcon", roleIcon),
                ("skillNameText", skill), ("descriptionText", desc), ("activeSkillsSummaryText", summary), ("playableGroup", playable.gameObject),
                ("equipButton", equip), ("weaponsButton", weapons), ("enterPortalButton", enter), ("lockedGroup", locked.gameObject),
                ("unlockButton", unlock), ("unlockPriceText", price), ("comingSoonGroup", soon.gameObject),
                ("warriorIcon", Item(5, 18)), ("rangerIcon", Item(5, 4)), ("mageIcon", Item(10, 2)), ("assassinIcon", Item(0, 21)) });
            Wire(panel, refs.ToArray());
            WireArray(panel, "equipTiles", equipTiles);
            WireArray(panel, "weaponTiles", weaponTiles);
            WireArray(panel, "slotSilhouettes", new Object[] { Item(12, 11), Item(1, 0), Item(12, 10), Item(0, 4), Item(3, 13), Item(0, 16) });
            Save(root, UiDir + "CharacterPanel.prefab", AddressKeys.UiPanelCharacter);
        }

        static void BuildPlayHud()
        {
            var root = Root<Play.PlayHud>("PlayHud", UILayer.Hud);
            SetBool(root.GetComponent<UIScreen>(), "closeOnBack", false);

            var stats = Frame(root, "Stats", FrDark, 12, A.TL, 10, 10, 260, 104);
            var aslot = Frame(stats, "AvatarSlot", SqF, 12, A.TL, 12, 22, 60, 60);
            var avatar = Img(aslot, "Avatar", At("Assets/Project/Art/Sprites/Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png", 8, 8), A.C, 0, -2, 32, 32);
            avatar.preserveAspect = true;
            var rows = new (string icon, string strip, string val)[] { ("icon_hp", "bar_strip_red", "72/90"), ("icon_armor", "bar_strip_grey", "3/9"), ("icon_mana", "bar_strip_blue", "118/150") };
            var fills = new Image[3]; var texts = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 14 + i * 26;
                Icon(stats, "Icon_" + i, G(rows[i].icon), 1, A.TL, 80, y + 4);
                (fills[i], texts[i]) = Bar(stats, "Bar_" + i, rows[i].strip, A.TL, 100, y, 148, 24, 8, 12, rows[i].val);
            }
            var coin = Frame(root, "Coin", FrDark, 8, A.TL, 10, 118, 84, 36);
            Icon(coin, "Icon", Coin, 2, A.L, 8, 0);
            var coinText = Txt(coin, "Value", null, "86", 16, Paper, TextAlignmentOptions.Left, A.L, 34, 0, 46, 24);
            var floor = Frame(root, "Floor", FrDark, 8, A.TL, 100, 118, 118, 36);
            var floorText = Txt(floor, "Label", "hud.floor", null, 16, Paper, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);

            var minimap = Frame(root, "Minimap", FrDark, 24, A.TR, 82, 10, 176, 130);
            var pause = IconButton(root, "Pause", IcPause, 2, A.TR, 10, 10, 64, 64);

            var joyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "VirtualJoystick.prefab");
            if (joyPrefab != null)
            {
                var joy = (GameObject)PrefabUtility.InstantiatePrefab(joyPrefab, root.transform);
                var jr = (RectTransform)joy.transform; Place(jr, A.BL, 48, 40, 160, 160);
            }

            var cluster = Rt(root, "Actions", A.BR, 28, 28, 344, 240);

            // Dưới - Phải (0, 0): Nút đánh thường
            var attack = IconButton(cluster, "Attack", IcSwordBtn, 4, A.BR, 0, 0, 104, 112);
            var vb = attack.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb, "action", (int)VirtualAction.Attack);
            Wire(vb, ("pressVisual", attack.transform.GetChild(0)));

            // Trên - Trái (114, 122): Ô đổi kiếm
            var swap = Rt(cluster, "WeaponSlot", A.BR, 114, 122, 104, 108);
            var sq = Img(swap, "Square", SqF, A.Stretch, 0, 0, 0, 0); sq.raycastTarget = true;
            var swapBtn = swap.gameObject.AddComponent<Button>(); swapBtn.targetGraphic = sq;
            var wicon = Img(swap, "Weapon", At("Assets/Project/Art/Sprites/Items/Weapon/Sword/File.png", 16, 144), A.C, 0, 2, 64, 64); wicon.preserveAspect = true;
            Icon(swap, "SwapIcon", IcSwap, 4, A.TL, -8, -8);
            var swapVb = swap.gameObject.AddComponent<VirtualButton>();
            SetEnum(swapVb, "action", (int)VirtualAction.SwapWeapon);
            Wire(swapVb, ("pressVisual", wicon.rectTransform));

            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");

            // Dưới - Trái (114, 0): Ô kích hoạt trang bị 1
            var s1 = Tile(tilePrefab, cluster, "ActiveSkill1", A.BR, 114, 0, 104, 108, false);
            var vb1 = s1.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb1, "action", (int)VirtualAction.ItemSkill1);

            // Trên - Phải (0, 122): Ô kích hoạt trang bị 2
            var s2 = Tile(tilePrefab, cluster, "ActiveSkill2", A.BR, 0, 122, 104, 108, false);
            var vb2 = s2.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb2, "action", (int)VirtualAction.ItemSkill2);

            // Bên - Trái (228, 60): Nút kỹ năng nhân vật (E / Character Skill) — khung tròn riêng, khác ô vật phẩm
            var cs = BuildCharacterSkillButton(cluster);

            // Nút tương tác (mở rương, cửa hàng...): chữ to, đọc rõ cả khi canvas bị làm tròn xuống ×0.5 trên màn nhỏ
            var interact = Frame(root, "InteractButton", FrLight, 12, A.B, 0, 128, 360, 76);
            var ibImg = interact.GetComponent<Image>(); ibImg.raycastTarget = true;
            var ibPrompt = Txt(interact, "Prompt", "interact.open_chest", null, 32, Ink, TextAlignmentOptions.Center, A.Stretch, 14, 6, 14, 6);
            ibPrompt.fontStyle = FontStyles.Bold;
            ibPrompt.enableAutoSizing = true; ibPrompt.fontSizeMin = 20; ibPrompt.fontSizeMax = 32; // câu dài (cửa hàng) không tràn khung
            var ibVb = interact.gameObject.AddComponent<VirtualButton>();
            SetEnum(ibVb, "action", (int)VirtualAction.Interact);
            Wire(ibVb, ("pressVisual", ibPrompt.rectTransform));
            interact.gameObject.SetActive(false);

            var boss = Frame(root, "BossBar", FrDark, 12, A.T, 0, 16, 480, 44);
            var bossFill = Img(boss, "Fill", G("bar_strip_red"), A.Stretch, 0, 0, 0, 0);
            MakeFilled(bossFill);
            var bossName = Txt(boss, "Name", null, "Vampire Lord", 14, Paper, TextAlignmentOptions.Center, A.Stretch, 0, -4, 0, -4);
            boss.gameObject.SetActive(false);

            var toastBg = Frame(root, "Toast", FrDark, 12, A.B, 0, 220, 600, 60);
            var toast = Txt(toastBg, "Text", null, "", 26, Paper, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            toastBg.gameObject.SetActive(false);

            Wire(root.GetComponent<Play.PlayHud>(),
                ("avatarImage", avatar), ("hpFill", fills[0]), ("armorFill", fills[1]), ("manaFill", fills[2]),
                ("hpText", texts[0]), ("armorText", texts[1]), ("manaText", texts[2]), ("coinText", coinText),
                ("floorLabel", floorText.GetComponent<LocalizedTMPText>()), ("minimapFrame", minimap), ("pauseButton", pause),
                ("bossBar", boss.gameObject), ("bossFill", bossFill), ("bossName", AddLoc(bossName, null)),
                ("attackButton", attack), ("swapWeaponButton", swapBtn), ("currentWeaponIcon", wicon),
                ("skillSlot1", s1), ("skillSlot2", s2),
                ("characterSkillIcon", cs.icon), ("characterSkillCooldown", cs.cooldown), ("characterSkillCooldownText", cs.cdText),
                ("characterSkillGlow", cs.glow), ("characterSkillManaBadge", cs.manaBadge), ("characterSkillManaText", cs.manaText),
                ("interactButton", interact.gameObject), ("interactPrompt", ibPrompt.GetComponent<LocalizedTMPText>()),
                ("toast", AddLoc(toast, null)));
            Save(root, UiDir + "PlayHud.prefab", AddressKeys.UiHudPlay);
        }

        /// <summary>
        /// Nút kỹ năng nhân vật: vòng sáng (pulse khi sẵn sàng) · đĩa nền · icon skill · lớp hồi chiêu radial ·
        /// khung tròn viền vàng 4 ngọc · số giây hồi · nhãn mana. Sprite pixel 26px hiển thị ×4 = 104.
        /// </summary>
        internal static (RectTransform root, Image icon, Image cooldown, TextMeshProUGUI cdText, Image glow, GameObject manaBadge, TextMeshProUGUI manaText)
            BuildCharacterSkillButton(RectTransform cluster)
        {
            const float K = 4f;
            var root = Rt(cluster, "CharacterSkill", A.BR, 228, 60, 104, 108);
            var body = Rt(root, "Body", A.Stretch, 0, 0, 0, 0); // nhún khi bấm (pressVisual)

            var glow = Img(body, "ReadyGlow", G("skill_btn_glow"), A.C, 0, 2, 32 * K, 32 * K);
            glow.color = new Color(1f, 1f, 1f, 0f);

            var disc = Img(body, "Disc", G("skill_btn_disc"), A.C, 0, 2, 26 * K, 26 * K);
            disc.raycastTarget = true; // vùng bấm hình tròn
            var btn = root.gameObject.AddComponent<Button>(); btn.targetGraphic = disc;
            btn.transition = Selectable.Transition.None;

            var icon = Img(body, "Icon", null, A.C, 0, 2, 16 * K, 16 * K); // icon chiêu 16px ×4
            icon.preserveAspect = true; icon.color = Color.white; icon.enabled = false;

            var cooldown = Img(body, "Cooldown", G("skill_btn_disc"), A.C, 0, 2, 26 * K, 26 * K);
            cooldown.color = new Color(0.05f, 0.04f, 0.09f, 0.72f);
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.enabled = false;

            Img(body, "Frame", G("skill_btn_frame"), A.C, 0, 2, 26 * K, 26 * K);

            var cdText = Txt(body, "CooldownText", null, "", 30, Paper, TextAlignmentOptions.Center, A.C, 0, 2, 104, 104);
            cdText.fontStyle = FontStyles.Bold;
            cdText.outlineWidth = 0.25f; cdText.outlineColor = Ink;
            cdText.enabled = false;

            // Nhãn mana: viên thuốc tối ở mép dưới, đè lên viền
            var badge = Frame(body, "ManaBadge", FrDark, 8, A.B, 0, -10, 56, 24);
            Img(badge, "ManaIcon", G("icon_mana"), A.L, 5, 0, 14, 14).preserveAspect = true;
            var manaText = Txt(badge, "Cost", null, "0", 16, new Color32(0x9c, 0xd8, 0xff, 0xff), TextAlignmentOptions.Center, A.Stretch, 18, 0, 4, 0);
            manaText.fontStyle = FontStyles.Bold;
            badge.gameObject.SetActive(false);

            var vb = root.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb, "action", (int)VirtualAction.CharacterSkill);
            Wire(vb, ("pressVisual", body));
            return (root, icon, cooldown, cdText, glow, badge.gameObject, manaText);
        }

        /// <summary>Chỉ dựng lại nút kỹ năng nhân vật trong PlayHud.prefab hiện có (giữ nguyên phần HUD khác).</summary>
        [MenuItem("Tools/Dungeon Gathering/UI/Rebuild Character Skill Button (PlayHud)")]
        public static void RebuildCharacterSkillButton()
        {
            Prepare();
            string path = UiDir + "PlayHud.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var cluster = root.transform.Find("Actions") as RectTransform;
                if (cluster == null) { Debug.LogError("[UIDesign] PlayHud thiếu node 'Actions'"); return; }
                var old = cluster.Find("CharacterSkill");
                int sibling = old != null ? old.GetSiblingIndex() : cluster.childCount;
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var cs = BuildCharacterSkillButton(cluster);
                cs.root.SetSiblingIndex(sibling);
                SetLayerRecursive(cs.root);
                Wire(root.GetComponent<Play.PlayHud>(),
                    ("characterSkillIcon", cs.icon), ("characterSkillCooldown", cs.cooldown), ("characterSkillCooldownText", cs.cdText),
                    ("characterSkillGlow", cs.glow), ("characterSkillManaBadge", cs.manaBadge), ("characterSkillManaText", cs.manaText));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[UIDesign] Đã dựng lại nút Character Skill trong " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void BuildPause()
        {
            var root = Root<Play.PauseScreen>("PauseScreen", UILayer.Screen);
            var dim = Img(root, "Dim", null, A.Stretch, 0, 0, 0, 0); dim.color = new Color(0.106f, 0.078f, 0.125f, 0.85f); dim.raycastTarget = true;
            var block = Rt(root, "Block", A.C, 0, 0, 1020, 600);

            // --- TẠM DỪNG
            var left = Frame(block, "PausePanel", FrDark, 24, A.TL, 0, 0, 470, 600);
            var li = Rt(left, "Inner", A.Stretch, 28, 24, 28, 24); // 414 × 552
            Txt(li, "Title", "pause.title", null, 44, Paper, TextAlignmentOptions.Left, A.TL, 0, 0, 414, 48);
            var info = Frame(li, "Info", FrMid, 12, A.TL, 0, 64, 414, 128);
            Icon(info, "Hero", At("Assets/Project/Art/Sprites/Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png", 8, 8), 4, A.L, 12, 0);
            var labels = new[] { "pause.class", "stat.time", "stat.kills", "common.coin" };
            var values = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                Txt(info, "L" + i, labels[i], null, 14, Ink, TextAlignmentOptions.Left, A.TL, 92, 4 + i * 24, 120, 20);
                values[i] = Txt(info, "V" + i, null, new[] { "Knight · Warrior", "06:42", "23", "86" }[i], 14, Ink, TextAlignmentOptions.Left, A.TL, 214, 4 + i * 24, 170, 20);
            }
            var note = Txt(li, "GearNote", "pause.gear_note", null, 13, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 208, 414, 40);
            note.textWrappingMode = TextWrappingModes.Normal;
            var resume = TextButton(li, "Resume", FrLight, 24, "common.continue", 26, Ink, A.TL, 0, 406, 414, 72, null, 0);
            var quit = TextButton(li, "Quit", FrBtn, 24, "pause.quit", 20, Ink, A.TL, 0, 488, 414, 64, null, 0);

            // --- CÀI ĐẶT (SettingsPanel dùng chung)
            BuildSettings(block, A.TL, 510, 0);

            Wire(root.GetComponent<Play.PauseScreen>(), ("resumeButton", resume), ("abandonButton", quit),
                ("runTimeText", values[1]), ("killsText", values[2]), ("coinsText", values[3]));
            Save(root, UiDir + "PauseScreen.prefab", AddressKeys.UiScreenPause);
        }

        /// <summary>Panel CÀI ĐẶT 510×600 (UI-Pause, cột phải).</summary>
        static RectTransform BuildSettings(Transform parent, A a, float x, float y)
        {
            var panel = Frame(parent, "SettingsPanel", FrDark, 24, a, x, y, 510, 600);
            var sp = panel.gameObject.AddComponent<SettingsPanel>();
            var si = Rt(panel, "Inner", A.Stretch, 28, 24, 28, 24); // 454 × 552
            Icon(si, "Gear", IcGear, 2, A.TL, 0, 0);
            Txt(si, "Title", "pause.settings", null, 34, Paper, TextAlignmentOptions.Left, A.TL, 56, 4, 300, 40);

            var so = new SerializedObject(sp);
            void Volume(string prop, string key, Sprite icon, float vy)
            {
                Icon(si, "Icon_" + prop, icon, 1, A.TL, 0, vy);
                Txt(si, "Label_" + prop, key, null, 15, Paper, TextAlignmentOptions.Left, A.TL, 32, vy + 4, 200, 20);
                var pct = Txt(si, "Pct_" + prop, null, "70%", 18, Paper, TextAlignmentOptions.Right, A.TR, 0, vy + 2, 80, 22);
                var dec = IconButton(si, "Dec_" + prop, IcBack, 2, A.TL, 0, vy + 30, 52, 56);
                var inc = IconButton(si, "Inc_" + prop, IcNext, 2, A.TR, 0, vy + 30, 52, 56);
                var cells = new Object[10];
                for (int i = 0; i < 10; i++)
                {
                    var c = Frame(si, $"Cell_{prop}_{i}", FrLight, 8, A.TL, 64 + i * 33, vy + 44, 30, 28);
                    cells[i] = c.GetComponent<Image>();
                }
                var row = so.FindProperty(prop);
                row.FindPropertyRelative("decrease").objectReferenceValue = dec;
                row.FindPropertyRelative("increase").objectReferenceValue = inc;
                row.FindPropertyRelative("percent").objectReferenceValue = pct;
                var arr = row.FindPropertyRelative("cells"); arr.arraySize = 10;
                for (int i = 0; i < 10; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
            }
            void Toggle(string prop, string key, string hintKey, float ty)
            {
                Txt(si, "Label_" + prop, key, null, 16, Paper, TextAlignmentOptions.Left, A.TL, 0, ty + 4, 320, 22);
                var hint = Txt(si, "Hint_" + prop, hintKey, null, 13, Soft, TextAlignmentOptions.Left, A.TL, 0, ty + 28, 320, 18);
                var btn = TextButton(si, "Toggle_" + prop, FrLight, 12, "common.on", 18, Ink, A.TR, 0, ty, 104, 56, null, 0);
                var row = so.FindProperty(prop);
                row.FindPropertyRelative("button").objectReferenceValue = btn;
                row.FindPropertyRelative("frame").objectReferenceValue = btn.GetComponent<Image>();
                row.FindPropertyRelative("label").objectReferenceValue = btn.transform.Find("Label").GetComponent<TMP_Text>();
            }
            Volume("bgm", "pause.bgm", IcMusic, 64);
            Volume("sfx", "pause.sfx", IcSound, 150);
            Toggle("autoAim", "pause.aim", "pause.aim_hint", 246);
            Toggle("vibration", "pause.vib", "pause.vib_hint", 318);
            so.FindProperty("cellOn").objectReferenceValue = FrLight;
            so.FindProperty("cellOff").objectReferenceValue = FrDark;
            so.FindProperty("toggleOnFrame").objectReferenceValue = FrLight;
            so.FindProperty("toggleOffFrame").objectReferenceValue = FrDark;
            so.ApplyModifiedPropertiesWithoutUndo();

            Txt(si, "Label_lang", "common.lang", null, 16, Paper, TextAlignmentOptions.Left, A.TL, 0, 420, 200, 22);
            foreach (var (code, label, flag, rx) in new[] { ("vi", "VI", "flag_vn", 114f), ("en", "EN", "flag_us", 0f) })
            {
                var b = Frame(si, "Lang_" + code, code == "en" ? FrLight : FrDark, 12, A.TR, rx, 400, 104, 64);
                var img = b.GetComponent<Image>(); img.raycastTarget = true;
                var button = b.gameObject.AddComponent<Button>(); button.targetGraphic = img;
                Icon(b, "Flag", Single($"Assets/Project/Art/Sprites/UI/Flags/{flag}.png"), 2, A.L, 12, 0);
                Txt(b, "Code", null, label, 16, code == "en" ? Ink : Paper, TextAlignmentOptions.Left, A.L, 60, 0, 34, 24);
                var lb = b.gameObject.AddComponent<LanguageButton>();
                var lso = new SerializedObject(lb);
                lso.FindProperty("languageCode").stringValue = code;
                lso.FindProperty("frame").objectReferenceValue = img;
                lso.FindProperty("selectedFrame").objectReferenceValue = FrLight;
                lso.FindProperty("normalFrame").objectReferenceValue = FrDark;
                lso.ApplyModifiedPropertiesWithoutUndo();
            }
            return panel;
        }

        static void PatchUIRoot()
        {
            const string path = "Assets/Project/Core/Prefabs/UIRoot.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var backdrop = root.transform.Find("Backdrop");
            if (backdrop != null)
            {
                var old = backdrop.Find("BackdropArt");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var art = Img(backdrop, "BackdropArt", G("bg_title_wide"), A.C, 0, 0, 1600, 720);
                art.transform.SetAsFirstSibling();
                var bimg = backdrop.GetComponent<Image>();
                if (bimg != null) bimg.color = Ink;
            }
            else Debug.LogWarning("[UIDesign] UIRoot không có node Backdrop.");

            // Node SafeArea bọc Hud, Screen, Panel, Popup, Toast (UI_SYSTEM §2, §3.3); Backdrop, Overlay, InputBlocker ở ngoài SafeArea.
            var safe = root.transform.Find("SafeArea") as RectTransform;
            if (safe == null)
            {
                var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
                go.layer = 5;
                safe = (RectTransform)go.transform;
                safe.SetParent(root.transform, false);
                safe.anchorMin = Vector2.zero;
                safe.anchorMax = Vector2.one;
                safe.offsetMin = safe.offsetMax = Vector2.zero;
            }
            else if (safe.GetComponent<SafeAreaFitter>() == null)
            {
                safe.gameObject.AddComponent<SafeAreaFitter>();
            }
            if (backdrop != null) safe.SetSiblingIndex(backdrop.GetSiblingIndex() + 1);

            foreach (var layerName in new[] { "Hud", "Screen", "Panel", "Popup", "Toast" })
            {
                var layer = root.transform.Find(layerName) ?? safe.Find(layerName);
                if (layer != null && layer.parent != safe)
                {
                    layer.SetParent(safe, false);
                    var lrt = layer as RectTransform;
                    if (lrt != null)
                    {
                        lrt.anchorMin = Vector2.zero;
                        lrt.anchorMax = Vector2.one;
                        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
                    }
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ================= đợt 2: Loadout, Shop, Settings popup, Kết thúc, Loading =================

        [MenuItem("Tools/Dungeon Gathering/UI/Build Design UI (Loadout, Shop, Results, Loading)")]
        public static void BuildStage2()
        {
            Prepare();
            BuildEquipment();
            BuildWeapon();
            BuildShop();
            BuildSettingsPopup();
            BuildResult(true);
            BuildResult(false);
            BuildLoading();
            AssetDatabase.SaveAssets();
            UIPreviewRenderer.RenderAll();
            Debug.Log("[UIDesign] Đã dựng Equipment, Weapon, Shop, SettingsPopup, Victory, Defeat, LoadingOverlay.");
        }

        static readonly string KnightIdle = "Assets/Project/Art/Sprites/Characters/Knight/Blue Knight idle Sprite-sheet 16x16.png";

        /// <summary>Khung chung 3 cột của Equipment/Weapon. Trả về (root, inner trái, inner giữa, inner phải).</summary>
        static (GameObject root, RectTransform left, RectTransform mid, RectTransform right) LoadoutShell<T>(string name, string titleKey) where T : UIScreen
        {
            var root = Root<T>(name, UILayer.Screen);
            SetBool(root.GetComponent<UIScreen>(), "releaseOnClose", true);
            var bg = Img(root, "Bg", null, A.Stretch, 0, 0, 0, 0); bg.color = Ink; bg.raycastTarget = true;
            Txt(root, "Title", titleKey, null, 34, Paper, TextAlignmentOptions.Left, A.TL, 100, 22, 320, 40);
            var block = Rt(root, "Block", A.C, 0, -34, 1240, 620);
            var l = Frame(block, "Detail", FrDark, 24, A.TL, 0, 0, 372, 620);
            var m = Frame(block, "Character", FrDark, 24, A.TL, 384, 0, 472, 620);
            var r = Frame(block, "Bag", FrDark, 24, A.TL, 868, 0, 372, 620);
            return (root, Rt(l, "Inner", A.Stretch, 28, 24, 28, 24), Rt(m, "Inner", A.Stretch, 24, 24, 24, 24), Rt(r, "Inner", A.Stretch, 24, 24, 24, 24));
        }

        /// <summary>Cột chi tiết (trái) dùng chung. Trả các tham chiếu để Wire.</summary>
        static List<(string, Object)> DetailColumn(RectTransform li, string flatKey, string passiveKey, string activeKey, float actionWidth)
        {
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");
            var group = Rt(li, "DetailGroup", A.Stretch, 0, 0, 0, 0);
            var tile = Tile(tilePrefab, group, "Tile", A.TL, 0, 0, 104, 108, true);
            var name = Txt(group, "Name", null, "Item", 24, Paper, TextAlignmentOptions.Left, A.TL, 118, 6, 198, 30);
            var meta = Txt(group, "Meta", null, "Helmet · Rarity C", 13, Soft, TextAlignmentOptions.Left, A.TL, 118, 40, 198, 18);
            var state = Txt(group, "State", null, "", 12, Gold, TextAlignmentOptions.Left, A.TL, 118, 62, 198, 18);
            var box = Frame(group, "StatsBox", FrMid, 12, A.TL, 0, 124, 316, 104);
            if (flatKey != null) Txt(box, "Label", flatKey, null, 11, Ink, TextAlignmentOptions.Left, A.TL, 12, 8, 290, 16);
            var stats = Txt(box, "Stats", null, "+1 Armor", 18, new Color32(0x14, 0x53, 0x2d, 0xff), TextAlignmentOptions.TopLeft, A.TL, 12, flatKey != null ? 28 : 10, 292, 66);
            stats.textWrappingMode = TextWrappingModes.Normal;
            Txt(group, "PassiveLabel", passiveKey, null, 11, Paper, TextAlignmentOptions.Left, A.TL, 0, 240, 316, 16);
            var passive = Txt(group, "Passive", null, "", 13, Color.white, TextAlignmentOptions.TopLeft, A.TL, 0, 260, 316, 84);
            passive.textWrappingMode = TextWrappingModes.Normal;
            Txt(group, "ActiveLabel", activeKey, null, 11, Paper, TextAlignmentOptions.Left, A.TL, 0, 352, 316, 16);
            var active = Txt(group, "Active", null, "", 13, Color.white, TextAlignmentOptions.TopLeft, A.TL, 0, 372, 316, 40);
            active.textWrappingMode = TextWrappingModes.Normal;
            var warn = Txt(group, "Warn", null, "", 12, new Color32(0xff, 0x8a, 0x84, 0xff), TextAlignmentOptions.TopLeft, A.TL, 0, 418, 316, 36);
            warn.textWrappingMode = TextWrappingModes.Normal;
            var action = TextButton(group, "Action", FrLight, 24, "common.equip", 20, Ink, A.TL, 0, 496, actionWidth, 76, null, 0);
            var hint = Txt(li, "EmptyHint", "equip.pick_hint", null, 14, Soft, TextAlignmentOptions.Center, A.C, 0, 0, 280, 80);
            hint.textWrappingMode = TextWrappingModes.Normal;
            group.gameObject.SetActive(false);   // mặc định: chưa chọn món → hiện gợi ý
            return new List<(string, Object)> {
                ("detailGroup", group.gameObject), ("emptyHint", hint.gameObject), ("detailTile", tile), ("detailName", name), ("detailMeta", meta),
                ("detailState", state), ("detailStats", stats), ("detailPassive", passive), ("detailActive", active), ("detailWarn", warn),
                ("actionButton", action), ("actionFrame", action.GetComponent<Image>()), ("actionLabel", action.transform.Find("Label").GetComponent<LocalizedTMPText>()),
                ("frameOn", FrLight), ("frameOff", FrDark), ("frameBlocked", FrDark) };
        }

        /// <summary>Cột túi (phải): tab lọc + lưới cuộn 3 cột ItemTile ×4.</summary>
        static List<(string, Object)> BagColumn(RectTransform ri, (string key, string raw, Sprite icon, float w)[] tabs, string noteKey, out Object[] tabButtons, out Object[] tabFrames)
        {
            tabButtons = new Object[tabs.Length]; tabFrames = new Object[tabs.Length];
            float x = 0;
            for (int i = 0; i < tabs.Length; i++)
            {
                var f = Frame(ri, "Tab_" + i, i == 0 ? FrLight : FrDark, 12, A.TL, x, 0, tabs[i].w, 44);
                var img = f.GetComponent<Image>(); img.raycastTarget = true;
                var b = f.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                if (tabs[i].icon != null) Icon(f, "Icon", tabs[i].icon, 1, A.C, 0, 0);
                else Txt(f, "Label", tabs[i].key, tabs[i].raw, 13, i == 0 ? Ink : Paper, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
                tabButtons[i] = b; tabFrames[i] = img; x += tabs[i].w + 4;
            }
            var count = Txt(ri, "Count", null, "5", 13, Soft, TextAlignmentOptions.Left, A.TL, 0, 54, 30, 18);
            Txt(ri, "Note", noteKey, null, 13, Soft, TextAlignmentOptions.Left, A.TL, 26, 54, 300, 18);
            var view = Rt(ri, "Viewport", A.TL, 0, 82, 324, 490);
            view.gameObject.AddComponent<RectMask2D>();
            var vimg = view.gameObject.AddComponent<Image>(); vimg.color = Color.clear; vimg.raycastTarget = true;
            var content = Rt(view, "Content", A.TL, 0, 0, 324, 490);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(104, 108); grid.spacing = new Vector2(6, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.horizontal = false; scroll.viewport = view; scroll.movementType = ScrollRect.MovementType.Clamped;
            return new List<(string, Object)> { ("bagCountText", count), ("bagContent", content),
                ("bagTilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab").GetComponent<ItemTile>()) };
        }

        /// <summary>Lưới chỉ số ở cột giữa: hàng = khung f-mid-s 204×40, nhãn trái, giá trị + chênh lệch phải.</summary>
        static void StatGrid(RectTransform mi, float top, (string key, string raw)[] rows, Object target)
        {
            var so = new SerializedObject(target);
            var arr = so.FindProperty("statRows"); arr.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var cell = Frame(mi, "Stat_" + i, FrMid, 12, A.TL, (i % 2) * 220, top + (i / 2) * 46, 204, 40);
                Txt(cell, "Label", rows[i].key, rows[i].raw, 12, Ink, TextAlignmentOptions.Left, A.L, 10, 0, 100, 20);
                var v = Txt(cell, "Value", null, "0", 18, Ink, TextAlignmentOptions.Right, A.R, 54, 0, 70, 24);
                var d = Txt(cell, "Delta", null, "", 15, Ink, TextAlignmentOptions.Right, A.R, 10, 0, 44, 20);
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("value").objectReferenceValue = v;
                e.FindPropertyRelative("delta").objectReferenceValue = d;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildEquipment()
        {
            var (root, li, mi, ri) = LoadoutShell<Lobby.EquipmentScreen>("EquipmentScreen", "equip.title");
            var refs = new List<(string, Object)>();
            refs.Add(("backButton", IconButton(root, "Back", IcBack, 2, A.TL, 24, 16, 64, 64)));
            var chip = Frame(root, "CharacterChip", FrLight, 12, A.TL, 430, 18, 230, 48);
            refs.Add(("characterChip", Txt(chip, "Text", null, "Knight · WARRIOR", 18, Ink, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0)));
            var act = Frame(root, "ActiveChip", FrDark, 12, A.TR, 254, 18, 180, 48);
            Txt(act, "Label", "common.active", null, 14, Paper, TextAlignmentOptions.Left, A.L, 10, 0, 90, 22);
            refs.Add(("activeCountText", Txt(act, "Value", null, "2 / 2", 22, new Color32(0x9f, 0xd0, 0xff, 0xff), TextAlignmentOptions.Right, A.R, 10, 0, 60, 28)));
            var syn = Frame(root, "SynergyChip", FrDark, 12, A.TR, 24, 18, 220, 48);
            Txt(syn, "Label", null, "Role Synergy", 14, Paper, TextAlignmentOptions.Left, A.L, 10, 0, 120, 22);
            var synText = Txt(syn, "Value", null, "3 / 6", 22, Gold, TextAlignmentOptions.Right, A.R, 10, 0, 60, 28);

            refs.AddRange(DetailColumn(li, "equip.flat_stats", "equip.passive_role", "equip.active_hdr", 316));

            var portrait = Img(mi, "Portrait", At(KnightIdle, 8, 8), A.T, 0, 40, 128, 128); portrait.preserveAspect = true;
            refs.Add(("portrait", portrait));
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");
            string[] labels = { "slot.helmet", "stat.armor", "slot.gloves", "slot.boots", "slot.neck", "slot.acc" };
            var slots = new Object[6];
            for (int i = 0; i < 6; i++)
            {
                bool right = i >= 3; float y = (i % 3) * 124;
                slots[i] = Tile(tilePrefab, mi, "Slot_" + i, right ? A.TR : A.TL, 0, y, 104, 108, true);
                Txt(mi, "SlotLabel_" + i, labels[i], null, 11, Paper, TextAlignmentOptions.Center, right ? A.TR : A.TL, 0, y + 108, 104, 14);
            }
            var tabs = new (string, string, Sprite, float)[] { ("common.all", null, null, 64f), (null, null, Item(12, 11), 38f), (null, null, Item(1, 0), 38f), (null, null, Item(12, 10), 38f), (null, null, Item(0, 4), 38f), (null, null, Item(3, 13), 38f), (null, null, Item(0, 16), 38f) };
            refs.AddRange(BagColumn(ri, tabs, "equip.bag_note", out var tb, out var tf));

            var screen = root.GetComponent<Lobby.EquipmentScreen>();
            Wire(screen, refs.ToArray());
            Wire(screen, ("synergyCountText", synText));
            WireArray(screen, "slotTiles", slots);
            WireArray(screen, "filterTabs", tb);
            WireArray(screen, "filterFrames", tf);
            WireArray(screen, "slotSilhouettes", new Object[] { Item(12, 11), Item(1, 0), Item(12, 10), Item(0, 4), Item(3, 13), Item(0, 16) });
            StatGrid(mi, 380, new (string, string)[] { ("stat.hp", null), ("stat.armor", null), ("stat.mana", null), ("stat.dmg", null), ("stat.aspd", null), ("stat.spd", null), ("stat.crit", null) }, screen);
            Save(root, UiDir + "EquipmentScreen.prefab", AddressKeys.UiScreenEquipment);
        }

        static void BuildWeapon()
        {
            var (root, li, mi, ri) = LoadoutShell<Lobby.WeaponScreen>("WeaponScreen", "common.weapons");
            var refs = new List<(string, Object)>();
            refs.Add(("backButton", IconButton(root, "Back", IcBack, 2, A.TL, 24, 16, 64, 64)));
            var act = Frame(root, "ActiveChip", FrDark, 12, A.TL, 290, 18, 180, 48);
            Txt(act, "Label", "common.active", null, 14, Paper, TextAlignmentOptions.Left, A.L, 10, 0, 90, 22);
            refs.Add(("activeCountText", Txt(act, "Value", null, "2 / 2", 22, new Color32(0x9f, 0xd0, 0xff, 0xff), TextAlignmentOptions.Right, A.R, 10, 0, 60, 28)));
            var hintChip = Frame(root, "CarryHint", FrDark, 12, A.TR, 24, 18, 470, 48);
            Icon(hintChip, "Swap", IcSwap, 4, A.L, 10, 0);
            Txt(hintChip, "Text", "weapon.carry_hint", null, 13, Paper, TextAlignmentOptions.Left, A.L, 44, 0, 410, 22);

            refs.AddRange(DetailColumn(li, null, "weapon.passive_hdr", "weapon.active_hdr", 153));
            var slot2 = TextButton(li.Find("DetailGroup"), "ToSlot2", FrBtn, 24, "weapon.to_slot2", 20, Ink, A.TL, 163, 496, 153, 76, null, 0);

            var portrait = Img(mi, "Portrait", At(KnightIdle, 8, 8), A.T, 0, 40, 128, 128); portrait.preserveAspect = true;
            refs.Add(("portrait", portrait));
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");
            var s0 = Tile(tilePrefab, mi, "Slot_0", A.TL, 0, 40, 104, 108, true);
            var s1 = Tile(tilePrefab, mi, "Slot_1", A.TR, 0, 40, 104, 108, true);
            Txt(mi, "SlotLabel_0", "weapon.slot1", null, 11, Paper, TextAlignmentOptions.Center, A.TL, 0, 152, 120, 30).textWrappingMode = TextWrappingModes.Normal;
            Txt(mi, "SlotLabel_1", "weapon.slot2", null, 11, Paper, TextAlignmentOptions.Center, A.TR, 0, 152, 104, 16);
            Icon(mi, "SwapIcon", IcSwap, 4, A.T, 0, 180);
            var tabs = new (string, string, Sprite, float)[] { ("common.all_lc", null, null, 72f), (null, "S", null, 44f), (null, "A", null, 44f), (null, "B", null, 44f), (null, "C", null, 44f), (null, "F", null, 44f) };
            refs.AddRange(BagColumn(ri, tabs, "weapon.armory", out var tb, out var tf));

            var screen = root.GetComponent<Lobby.WeaponScreen>();
            Wire(screen, refs.ToArray());
            Wire(screen, ("slot2Button", slot2), ("slot2Frame", slot2.GetComponent<Image>()), ("slot2Label", slot2.transform.Find("Label").GetComponent<LocalizedTMPText>()));
            WireArray(screen, "slotTiles", new Object[] { s0, s1 });
            WireArray(screen, "filterTabs", tb);
            WireArray(screen, "filterFrames", tf);
            StatGrid(mi, 380, new (string, string)[] { ("stat.dmg", null), ("stat.aspd", null), ("stat.range", null), (null, "DPS"), ("stat.crit", null), ("stat.mana_hit", null) }, screen);
            Save(root, UiDir + "WeaponScreen.prefab", AddressKeys.UiScreenWeapon);
        }

        static void BuildShop()
        {
            var root = Root<Play.ShopScreen>("ShopScreen", UILayer.Screen);
            SetBool(root.GetComponent<UIScreen>(), "releaseOnClose", true);
            var bg = Img(root, "Bg", null, A.Stretch, 0, 0, 0, 0); bg.color = Ink; bg.raycastTarget = true;
            var refs = new List<(string, Object)>();
            refs.Add(("closeButton", IconButton(root, "Close", IcBack, 2, A.TL, 24, 18, 64, 64)));
            Txt(root, "Title", "common.merchant", null, 36, Paper, TextAlignmentOptions.Left, A.TL, 100, 24, 300, 44);
            var ctx = Frame(root, "Context", FrDark, 12, A.TL, 380, 24, 300, 40);
            refs.Add(("contextChip", AddLoc(Txt(ctx, "Text", "shop.where_lobby", null, 13, Paper, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0), "shop.where_lobby")));
            var coin = Frame(root, "Coin", FrDark, 12, A.TR, 24, 18, 160, 56);
            Icon(coin, "Icon", Coin, 2, A.L, 12, 0);
            refs.Add(("coinText", Txt(coin, "Value", null, "1,240", 24, Paper, TextAlignmentOptions.Left, A.L, 46, 0, 100, 32)));

            var block = Rt(root, "Block", A.C, 0, -34, 1232, 600);
            var grid = Frame(block, "Grid", FrDark, 24, A.TL, 0, 0, 760, 600);
            var gi = Rt(grid, "Inner", A.Stretch, 24, 24, 24, 24); // 712 × 552
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");
            var screen = root.GetComponent<Play.ShopScreen>();
            var so = new SerializedObject(screen);
            var cards = so.FindProperty("cards"); cards.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                var c = Frame(gi, "Card_" + i, i == 0 ? FrLight : FrMid, 24, A.TL, (i % 3) * 241, (i / 3) * 282, 229, 270);
                var img = c.GetComponent<Image>(); img.raycastTarget = true;
                var b = c.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                var t = Tile(tilePrefab, c, "Tile", A.T, 0, 24, 104, 108, true);
                var n = Txt(c, "Name", null, "Item", 20, Ink, TextAlignmentOptions.Center, A.T, 0, 136, 180, 44); n.textWrappingMode = TextWrappingModes.Normal;
                var k = Txt(c, "Kind", null, "Weapon · A", 16, Ink, TextAlignmentOptions.Center, A.T, 0, 180, 180, 22);
                Icon(c, "Coin", Coin, 2, A.B, -22, 30);
                var p = Txt(c, "Price", null, "120", 22, Ink, TextAlignmentOptions.Left, A.B, 32, 28, 70, 28);
                var e = cards.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("button").objectReferenceValue = b;
                e.FindPropertyRelative("frame").objectReferenceValue = img;
                e.FindPropertyRelative("tile").objectReferenceValue = t;
                e.FindPropertyRelative("name").objectReferenceValue = n;
                e.FindPropertyRelative("kind").objectReferenceValue = k;
                e.FindPropertyRelative("price").objectReferenceValue = p;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var det = Frame(block, "Detail", FrDark, 24, A.TL, 776, 0, 456, 600);
            var di = Rt(det, "Inner", A.Stretch, 24, 24, 24, 24); // 408 × 552
            refs.Add(("detailTile", Tile(tilePrefab, di, "Tile", A.TL, 0, 0, 104, 108, true)));
            refs.Add(("detailName", Txt(di, "Name", null, "Item", 28, Paper, TextAlignmentOptions.Left, A.TL, 118, 14, 290, 36)));
            refs.Add(("detailKind", Txt(di, "Kind", null, "Weapon · A", 18, Soft, TextAlignmentOptions.Left, A.TL, 118, 52, 290, 24)));
            var box = Frame(di, "StatsBox", FrMid, 12, A.TL, 0, 124, 408, 92);
            var stats = Txt(box, "Stats", null, "22 damage", 20, new Color32(0x14, 0x53, 0x2d, 0xff), TextAlignmentOptions.TopLeft, A.TL, 12, 10, 380, 70);
            stats.textWrappingMode = TextWrappingModes.Normal; refs.Add(("detailStats", stats));
            var desc = Txt(di, "Desc", null, "", 18, Color.white, TextAlignmentOptions.TopLeft, A.TL, 0, 232, 408, 140);
            desc.textWrappingMode = TextWrappingModes.Normal; refs.Add(("detailDesc", desc));
            var msg = Txt(di, "Message", "shop.hint_lobby", null, 16, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 420, 408, 40);
            msg.textWrappingMode = TextWrappingModes.Normal; refs.Add(("message", msg.GetComponent<LocalizedTMPText>()));
            var buy = TextButton(di, "Buy", FrLight, 24, "shop.buy", 24, Ink, A.TL, 0, 476, 408, 76, null, 0);
            ((RectTransform)buy.transform.Find("Label")).anchoredPosition = new Vector2(-50, -2);
            Icon(buy, "Coin", Coin, 2, A.C, 46, 0);
            refs.Add(("buyButton", buy));
            refs.Add(("buyPrice", Txt(buy, "Price", null, "120", 24, Ink, TextAlignmentOptions.Left, A.C, 110, -2, 90, 32)));
            refs.Add(("cardOn", FrLight)); refs.Add(("cardOff", FrMid)); refs.Add(("potionIcon", Item(12, 14)));
            Wire(screen, refs.ToArray());
            Save(root, UiDir + "ShopScreen.prefab", AddressKeys.UiScreenShop);
        }

        static void BuildSettingsPopup()
        {
            var root = Root<SettingsPopup>("SettingsPopup", UILayer.Popup);
            var dim = Img(root, "Dim", null, A.Stretch, 0, 0, 0, 0); dim.color = new Color(0, 0, 0, 0.4f); dim.raycastTarget = true;
            var panel = BuildSettings(root.transform, A.C, 0, 0);
            var close = IconButton(panel, "Close", At(Sheet, 216, 56), 2, A.TR, 14, 14, 48, 48);
            Wire(root.GetComponent<SettingsPopup>(), ("closeButton", close), ("panel", panel.GetComponent<SettingsPanel>()));
            Save(root, UiDir + "SettingsPopup.prefab", AddressKeys.UiPopupSettings);
        }

        static void BuildResult(bool victory)
        {
            string name = victory ? "VictoryScreen" : "DefeatScreen";
            var root = Root<Results.RunResultScreen>(name, UILayer.Screen);
            SetBool(root.GetComponent<UIScreen>(), "releaseOnClose", true);
            SetBool(root.GetComponent<UIScreen>(), "closeOnBack", false);
            SetBool(root.GetComponent<UIScreen>(), "isVictory", victory);
            var bg = Img(root, "Bg", null, A.Stretch, 0, 0, 0, 0); bg.color = new Color(0.106f, 0.078f, 0.125f, victory ? 0.85f : 0.92f); bg.raycastTarget = true;

            var head = Rt(root, "Header", A.T, 0, 26, 900, 170);
            if (victory)
                Txt(head, "Sub", "win.sub", null, 18, Gold, TextAlignmentOptions.Center, A.T, 0, 0, 900, 24).characterSpacing = 8;
            var title = Txt(head, "Title", victory ? "win.title" : "end.fallen", null, 76, victory ? Gold : Paper, TextAlignmentOptions.Center, A.T, 0, victory ? 30 : 10, 900, 84);
            Shadow(title, victory ? new Color32(0x7a, 0x4a, 0x12, 0xff) : new Color32(0x5a, 0x1d, 0x24, 0xff), 5);
            if (victory)
            {
                Icon(head, "Boss", G("menu_hero_3"), 2, A.T, -230, 124);
                Txt(head, "By", "win.defeated", null, 16, Paper, TextAlignmentOptions.Right, A.T, -94, 132, 200, 22);
                Txt(head, "BossName", null, "Vampire Lord", 16, Gold, TextAlignmentOptions.Left, A.T, 112, 132, 200, 22);
            }

            var block = Rt(root, "Block", A.C, 0, -24, 1080, 320);
            var left = Frame(block, "Stats", FrDark, 24, A.TL, 0, 0, 580, 320);
            var lin = Rt(left, "Inner", A.Stretch, 26, 24, 26, 24); // 528 × 272
            string[] keys = { "stat.floor", "stat.time", "stat.kills", "stat.rooms" };
            var values = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var c = Frame(lin, "Stat_" + i, FrMid, 12, A.TL, i * 134, 0, 126, 72);
                Txt(c, "Label", keys[i], null, 12, Ink, TextAlignmentOptions.Left, A.TL, 12, 10, 104, 16);
                values[i] = Txt(c, "Value", null, new[] { "3-5", "14:32", "86", "15" }[i], 26, Ink, TextAlignmentOptions.Left, A.TL, 12, 28, 104, 30);
            }
            var badge = Frame(lin, "Badge", FrLight, 12, A.TL, 0, 86, 200, 32);
            Txt(badge, "Text", victory ? "win.first" : "end.record", null, 14, Ink, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            Txt(lin, "LootLabel", victory ? "win.chest" : "end.found", null, 13, Soft, TextAlignmentOptions.Left, A.TL, 0, 132, 528, 18);
            var loot = Rt(lin, "Loot", A.TL, 0, 160, 528, 56);
            var hl = loot.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childControlWidth = hl.childControlHeight = false; hl.childForceExpandWidth = hl.childForceExpandHeight = false;

            var right = Frame(block, "Coins", FrDark, 24, A.TL, 600, 0, 480, 320);
            var rin = Rt(right, "Inner", A.Stretch, 26, 24, 26, 24);
            Txt(rin, "Label", victory ? "win.coin_if" : "end.coin_back", null, 13, Soft, TextAlignmentOptions.Left, A.TL, 0, 0, 428, 18);
            var conv = victory ? new[] { "win.conv_run", "win.conv_floor", "win.conv_boss" } : new[] { "end.conv_run", "end.conv_floor", "end.conv_boss" };
            var rowLabels = new TMP_Text[3];
            var rowValues = new TMP_Text[3];
            for (int i = 0; i < conv.Length; i++)
            {
                rowLabels[i] = Txt(rin, "Row_" + i, conv[i], null, 15, Paper, TextAlignmentOptions.Left, A.TL, 0, 30 + i * 36, 320, 24);
                rowValues[i] = Txt(rin, "RowVal_" + i, null, "+0", 16, victory ? Gold : Paper, TextAlignmentOptions.Right, A.TR, 0, 30 + i * 36, 90, 24);
            }
            var total = Frame(rin, "Total", victory ? FrLight : FrMid, 12, A.BL, 0, 0, 428, 64);
            Txt(total, "Label", "common.total", null, 16, Ink, TextAlignmentOptions.Left, A.L, 10, 0, 160, 24);
            Icon(total, "Coin", Coin, 3, A.R, 120, 0);
            var coins = Txt(total, "Value", null, victory ? "+346" : "+57", 34, Ink, TextAlignmentOptions.Right, A.R, 10, 0, 100, 40);

            var lobby = TextButton(root, "Lobby", FrBtn, 24, victory ? "win.to_lobby" : "common.to_lobby", 22, Ink, A.B, -170, 30, 320, 76, null, 0);
            var next = TextButton(root, "Continue", FrLight, 24, victory ? "win.next" : "common.retry", 22, Ink, A.B, 170, 30, 320, 76, null, 0);

            var scr = root.GetComponent<Results.RunResultScreen>();
            Wire(scr, ("lobbyButton", lobby), ("continueButton", next), ("floorText", values[0]), ("timeText", values[1]),
                ("killsText", values[2]), ("coinsText", coins), ("lootGrid", loot),
                ("lootTilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTileSmall.prefab").GetComponent<ItemTile>()));
            WireArray(scr, "coinRowLabels", rowLabels);
            WireArray(scr, "coinRowValues", rowValues);
            Save(root, UiDir + name + ".prefab", victory ? AddressKeys.UiScreenVictory : AddressKeys.UiScreenDefeat);
        }

        /// <summary>Dựng lại con của LoadingOverlay trong UIRoot theo UI-Loading.dc.html (giữ component để tham chiếu từ Systems không gãy).</summary>
        static void BuildLoading()
        {
            const string path = "Assets/Project/Core/Prefabs/UIRoot.prefab";
            var uiRoot = PrefabUtility.LoadPrefabContents(path);
            var overlay = uiRoot.GetComponentInChildren<LoadingOverlay>(true);
            if (overlay == null) { Debug.LogWarning("[UIDesign] UIRoot không có LoadingOverlay."); PrefabUtility.UnloadPrefabContents(uiRoot); return; }
            var o = overlay.transform;
            for (int i = o.childCount - 1; i >= 0; i--) Object.DestroyImmediate(o.GetChild(i).gameObject);
            var ort = (RectTransform)o; ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = ort.offsetMax = Vector2.zero;
            var dim = Img(o, "Dim", null, A.Stretch, 0, 0, 0, 0); dim.color = new Color(0.106f, 0.078f, 0.125f, 0.25f); dim.raycastTarget = true;

            var logo = Rt(o, "Logo", A.T, 0, 108, 700, 200);
            var d = Txt(logo, "Dungeon", null, "DUNGEON", 88, Title, TextAlignmentOptions.Center, A.T, 0, 0, 560, 90);
            Shadow(d, new Color32(0x6b, 0x3f, 0x1e, 0xff), 6);
            var torch = Single("Assets/Project/Art/Sprites/Environment/Props/torch/torch_1.png");
            Icon(logo, "TorchL", torch, 4, A.T, -300, 24); Icon(logo, "TorchR", torch, 4, A.T, 300, 24);
            var g = Txt(logo, "Gathering", null, "GATHERING", 52, Paper, TextAlignmentOptions.Center, A.T, 0, 104, 560, 60); g.characterSpacing = 16;
            Shadow(g, new Color32(0x4d, 0x3a, 0x52, 0xff), 5);

            var pg = Rt(o, "ProgressGroup", A.C, 0, -110, 720, 190);
            var runner = Img(pg, "Runner", At(KnightIdle, 8, 8), A.TL, 0, 0, 64, 64);
            var bar = Frame(pg, "Bar", FrDark, 12, A.TL, 0, 72, 720, 44);
            var fill = Img(bar, "Fill", G("bar_strip_green"), A.Stretch, 6, 6, 6, 6); MakeFilled(fill); fill.fillAmount = 0.6f;
            var step = Txt(pg, "Step", "loading.step_core", null, 20, Paper, TextAlignmentOptions.Left, A.TL, 0, 128, 520, 26);
            var pct = Txt(pg, "Percent", null, "60%", 24, Title, TextAlignmentOptions.Right, A.TR, 0, 124, 140, 30);
            Txt(pg, "FirstTime", "loading.first_time", null, 14, new Color32(0xc9, 0xcd, 0xd8, 0xff), TextAlignmentOptions.Left, A.TL, 0, 162, 720, 20);

            var tip = Rt(o, "Tip", A.B, 0, 44, 840, 52);
            var tipFrame = Frame(tip, "Frame", FrDark, 12, A.Stretch, 0, 0, 0, 0);
            var tag = Frame(tip, "Tag", FrLight, 12, A.L, 8, 0, 64, 34);
            Txt(tag, "Text", "loading.tip_label", null, 14, Ink, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            var tipText = Txt(tip, "Text", "loading.tip1", null, 16, Paper, TextAlignmentOptions.Left, A.Stretch, 84, 0, 16, 0);

            var err = Rt(o, "ErrorGroup", A.C, 0, -90, 720, 170);
            var ef = Frame(err, "Message", FrDark, 12, A.T, 0, 0, 640, 56);
            var errText = Txt(ef, "Text", "loading.error", null, 18, new Color32(0xff, 0x8a, 0x84, 0xff), TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            var retry = TextButton(err, "Retry", FrLight, 24, "loading.retry", 24, Ink, A.T, 0, 80, 260, 72, null, 0);
            err.gameObject.SetActive(false);

            var cg = o.GetComponent<CanvasGroup>() ?? o.gameObject.AddComponent<CanvasGroup>();
            Wire(overlay, ("group", cg), ("progressGroup", pg.gameObject), ("progressFill", fill), ("runner", runner.rectTransform),
                ("stepLabel", step.GetComponent<LocalizedTMPText>()), ("percentLabel", pct), ("tipLabel", tipText.GetComponent<LocalizedTMPText>()),
                ("errorGroup", err.gameObject), ("errorLabel", errText.GetComponent<LocalizedTMPText>()), ("retryButton", retry));
            var so = new SerializedObject(overlay); var tr = so.FindProperty("runnerTravel"); if (tr != null) { tr.floatValue = 656f; so.ApplyModifiedPropertiesWithoutUndo(); }
            SetLayerRecursive(o);
            PrefabUtility.SaveAsPrefabAsset(uiRoot, path);
            PrefabUtility.UnloadPrefabContents(uiRoot);
        }

        // ================= kit =================

        enum A { TL, T, TR, L, C, R, BL, B, BR, Stretch }

        static void Prepare()
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            en = LoadEnglish();
            // import settings cho sprite sinh thêm
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Gen.TrimEnd('/') }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 16 || ti.filterMode != FilterMode.Point || ti.textureCompression != TextureImporterCompression.Uncompressed;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 16;
                ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false;
                ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
            }
            // viền 9-slice cho frame trên sheet: dark, mid, light, btn, 5 ô rarity
            SetBorders(Sheet, new[] { (240, 70), (240, 20), (290, 20), (290, 70), (480, 100), (480, 132), (480, 164), (480, 196), (480, 230) }, SrcBorder);
        }

        static Sprite At(string path, int x, int yTop)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) { Debug.LogWarning($"[UIDesign] Thiếu {path}"); return null; }
            int y = tex.height - 1 - yTop;
            foreach (var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            {
                var r = s.rect;
                if (x >= r.x && x < r.xMax && y >= r.y && y < r.yMax) return s;
            }
            Debug.LogWarning($"[UIDesign] Không có slice tại ({x},{yTop}) trong {path}");
            return null;
        }

        static Sprite Single(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();

        static void SetBorders(string path, (int x, int yTop)[] points, float border)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var so = new SerializedObject(importer);
            var sprites = so.FindProperty("m_SpriteSheet.m_Sprites");
            bool changed = false;
            for (int i = 0; i < sprites.arraySize; i++)
            {
                var e = sprites.GetArrayElementAtIndex(i);
                var r = e.FindPropertyRelative("m_Rect").rectValue;
                foreach (var (x, yTop) in points)
                {
                    int y = tex.height - 1 - yTop;
                    if (x < r.x || x >= r.xMax || y < r.y || y >= r.yMax) continue;
                    var b = new Vector4(border, border, border, border);
                    var bp = e.FindPropertyRelative("m_Border");
                    if (bp.vector4Value != b) { bp.vector4Value = b; changed = true; }
                }
            }
            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            importer.SaveAndReimport();
        }

        static Dictionary<string, string> LoadEnglish()
        {
            var map = new Dictionary<string, string>();
            string csv = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Design", "localization", "DG_Multilanguage.csv"));
            if (!File.Exists(csv)) return map;
            foreach (var row in ParseCsv(File.ReadAllText(csv, Encoding.UTF8)).Skip(1))
                if (row.Length >= 2) map[row[0]] = row[1];
            return map;
        }

        static string En(string key) => key != null && en != null && en.TryGetValue(key, out var v) ? v : key;

        static GameObject Root<T>(string name, UILayer layer) where T : UIScreen
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var screen = go.AddComponent<T>();
            SetEnum(screen, "layer", (int)layer);
            return go;
        }

        static RectTransform Rt(Component parent, string name, A a, float x, float y, float w, float h) => Rt(parent.transform, name, a, x, y, w, h);
        static RectTransform Rt(GameObject parent, string name, A a, float x, float y, float w, float h) => Rt(parent.transform, name, a, x, y, w, h);
        static RectTransform Rt(Transform parent, string name, A a, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; Place(rt, a, x, y, w, h);
            return rt;
        }

        /// <summary>Đặt theo tọa độ thiết kế. x,y = khoảng cách tới mép của góc neo (y tính từ trên với neo trên). Stretch: x,y,w,h = inset trái, trên, phải, dưới.</summary>
        static void Place(RectTransform rt, A a, float x, float y, float w, float h)
        {
            Vector2 p = a switch
            {
                A.TL => new(0, 1), A.T => new(0.5f, 1), A.TR => new(1, 1), A.L => new(0, 0.5f), A.C => new(0.5f, 0.5f),
                A.R => new(1, 0.5f), A.BL => new(0, 0), A.B => new(0.5f, 0), A.BR => new(1, 0), _ => new(0.5f, 0.5f)
            };
            if (a == A.Stretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new(0.5f, 0.5f);
                rt.offsetMin = new(x, h); rt.offsetMax = new(-w, -y);
                return;
            }
            rt.anchorMin = rt.anchorMax = rt.pivot = p;
            rt.sizeDelta = new(w, h);
            float px = a is A.TR or A.R or A.BR ? -x : x;
            float py = a is A.TL or A.T or A.TR ? -y : y;
            rt.anchoredPosition = new(px, py);
        }

        static Image Img(Component parent, string name, Sprite s, A a, float x, float y, float w, float h) => Img(parent.transform, name, s, a, x, y, w, h);
        static Image Img(GameObject parent, string name, Sprite s, A a, float x, float y, float w, float h) => Img(parent.transform, name, s, a, x, y, w, h);
        static Image Img(Transform parent, string name, Sprite s, A a, float x, float y, float w, float h)
        {
            var rt = Rt(parent, name, a, x, y, w, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = s; img.raycastTarget = false;
            if (s == null) img.color = Color.clear;
            return img;
        }

        /// <summary>Khung 9-slice: viền nguồn 6px hiển thị <paramref name="borderUnits"/> đơn vị (24 = f-*, 12 = f-*-s, 8 = f-*-xs).</summary>
        static RectTransform Frame(Component parent, string name, Sprite s, float borderUnits, A a, float x, float y, float w, float h) => Frame(parent.transform, name, s, borderUnits, a, x, y, w, h);
        static RectTransform Frame(GameObject parent, string name, Sprite s, float borderUnits, A a, float x, float y, float w, float h) => Frame(parent.transform, name, s, borderUnits, a, x, y, w, h);
        static RectTransform Frame(Transform parent, string name, Sprite s, float borderUnits, A a, float x, float y, float w, float h)
        {
            var img = Img(parent, name, s, a, x, y, w, h);
            img.type = Image.Type.Sliced; img.fillCenter = true;
            img.pixelsPerUnitMultiplier = SrcBorder / (borderUnits * (s != null ? s.pixelsPerUnit : 16f) / RefPpu);
            return img.rectTransform;
        }

        /// <summary>Icon ở bội số nguyên <paramref name="k"/> (§3.2 UI_SYSTEM: ưu tiên ×2/×4).</summary>
        static Image Icon(Component parent, string name, Sprite s, float k, A a, float x, float y)
        {
            Vector2 size = s != null ? s.rect.size * k : new Vector2(16, 16) * k;
            var img = Img(parent.transform, name, s, a, x, y, size.x, size.y);
            img.preserveAspect = true;
            return img;
        }
        static Image Icon(GameObject parent, string name, Sprite s, float k, A a, float x, float y) => Icon(parent.transform, name, s, k, a, x, y);

        static TextMeshProUGUI Txt(Component parent, string name, string key, string raw, float size, Color color, TextAlignmentOptions align, A a, float x, float y, float w, float h)
        {
            var rt = Rt(parent.transform, name, a, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = size; t.color = color; t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.raycastTarget = false; t.richText = true;
            t.text = key != null ? En(key) : raw ?? string.Empty;
            if (key != null) AddLoc(t, key);
            return t;
        }
        static TextMeshProUGUI Txt(GameObject parent, string name, string key, string raw, float size, Color color, TextAlignmentOptions align, A a, float x, float y, float w, float h)
            => Txt(parent.transform, name, key, raw, size, color, align, a, x, y, w, h);

        static LocalizedTMPText AddLoc(TMP_Text t, string key)
        {
            var loc = t.GetComponent<LocalizedTMPText>() ?? t.gameObject.AddComponent<LocalizedTMPText>();
            var f = typeof(LocalizedTMPText).GetField("localizedString", BindingFlags.NonPublic | BindingFlags.Instance);
            var ls = new LocalizedString();
            if (key != null) ls.SetReference(LocalizationFontConfig.TableName, key);
            else ls.TableReference = LocalizationFontConfig.TableName;
            f.SetValue(loc, ls);
            EditorUtility.SetDirty(loc);
            return loc;
        }

        static void Shadow(TMP_Text t, Color c, float dy)
        {
            var copy = Object.Instantiate(t.gameObject, t.transform.parent);
            copy.name = t.name + "_Shadow";
            copy.transform.SetSiblingIndex(t.transform.GetSiblingIndex());
            var ct = copy.GetComponent<TMP_Text>(); ct.color = c;
            ((RectTransform)copy.transform).anchoredPosition += new Vector2(0, -dy);
            var loc = copy.GetComponent<LocalizedTMPText>(); if (loc != null) Object.DestroyImmediate(loc);
        }

        static Button IconButton(Component parent, string name, Sprite icon, float k, A a, float x, float y, float w, float h)
        {
            var rt = Rt(parent.transform, name, a, x, y, w, h);
            var hit = rt.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var ic = Icon(rt, "Icon", icon, k, A.C, 0, 0);
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = ic;
            return b;
        }
        static Button IconButton(GameObject parent, string name, Sprite icon, float k, A a, float x, float y, float w, float h) => IconButton(parent.transform, name, icon, k, a, x, y, w, h);

        static Button TextButton(GameObject parent, string name, Sprite frame, float border, string key, float size, Color color, A a, float x, float y, float w, float h, Sprite icon, float k)
            => TextButton(parent.transform, name, frame, border, key, size, color, a, x, y, w, h, icon, k);
        static Button TextButton(Component parent, string name, Sprite frame, float border, string key, float size, Color color, A a, float x, float y, float w, float h, Sprite icon, float k)
        {
            var rt = Frame(parent.transform, name, frame, border, a, x, y, w, h);
            var img = rt.GetComponent<Image>(); img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = img;
            float shift = 0;
            if (icon != null) { Icon(rt, "Icon", icon, k, A.L, 22, -2); shift = icon.rect.width * k * 0.5f + 6; }
            var t = Txt(rt, "Label", key, null, size, color, TextAlignmentOptions.Center, A.C, shift, -2, w - 24, h - 16);
            return b;
        }

        /// <summary>Thanh: rãnh khung tối 9-slice + dải màu Filled Horizontal (driver: Image.fillAmount) + chữ giá trị ở giữa.</summary>
        static (Image fill, TMP_Text value) Bar(Component parent, string name, string strip, A a, float x, float y, float w, float h, float border, float fontSize, string val)
        {
            var track = Frame(parent.transform, name, FrDark, border, a, x, y, w, h);
            var fill = Img(track, "Fill", G(strip), A.Stretch, border, border, border, border);
            MakeFilled(fill);
            var t = Txt(track, "Value", null, val, fontSize, Paper, TextAlignmentOptions.Center, A.Stretch, 0, -6, 0, -6);
            return (fill, t);
        }

        static void MakeFilled(Image img)
        {
            img.type = Image.Type.Filled; img.fillMethod = Image.FillMethod.Horizontal; img.fillOrigin = 0; img.fillAmount = 0.8f;
        }

        static ItemTile Tile(GameObject prefab, Component parent, string name, A a, float x, float y, float w, float h, bool letter)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            go.name = name;
            Place((RectTransform)go.transform, a, x, y, w, h);
            var tile = go.GetComponent<ItemTile>();
            SetBool(tile, "showRarityLetter", letter);
            tile.SetEmpty(null);   // trạng thái mặc định của prefab = ô trống (runtime SetItem/SetEmpty ghi đè)
            tile.SetSelected(false);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        static void Wire(Object target, params (string field, Object value)[] pairs)
        {
            var so = new SerializedObject(target);
            foreach (var (f, v) in pairs)
            {
                var p = so.FindProperty(f);
                if (p == null) { Debug.LogWarning($"[UIDesign] {target.GetType().Name} không có field '{f}'"); continue; }
                p.objectReferenceValue = v;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[UIDesign] {target.GetType().Name} không có field '{field}'"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool v)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p != null) { p.boolValue = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetEnum(Object target, string field, int v)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p != null) { p.enumValueIndex = v; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void Save(GameObject root, string path, string address)
        {
            SetLayerRecursive(root.transform);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || string.IsNullOrEmpty(address)) return;
            string guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            entry.address = address;
        }

        static void SetLayerRecursive(Transform t)
        {
            t.gameObject.layer = 5;
            foreach (Transform c in t) SetLayerRecursive(c);
        }

        static List<string[]> ParseCsv(string text)
        {
            var rows = new List<string[]>(); var row = new List<string>(); var cell = new StringBuilder(); bool q = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (q) { if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else if (ch == '"') q = false; else cell.Append(ch); continue; }
                switch (ch)
                {
                    case '"': q = true; break;
                    case ',': row.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n': row.Add(cell.ToString()); cell.Clear(); rows.Add(row.ToArray()); row.Clear(); break;
                    default: cell.Append(ch); break;
                }
            }
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row.ToArray()); }
            return rows;
        }
    }
}
