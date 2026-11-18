using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using DungeonGathering.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    public static partial class UIDesignBuilder
    {
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
            foreach (var (s, k) in new[] {
                (G("menu_hero_0"), 4), (G("menu_hero_1"), 4),
                (At(KnightIdle, 24, 24), 2),
                (G("menu_hero_2"), 4), (G("menu_hero_3"), 4) })
                Icon(row, "Hero", s, k, A.C, 0, 0);

            var play = TextButton(root, "Play", FrCrim, 24, null, 44, Gold, A.B, 0, 96, 260, 112, null, 0, "PLAY");
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
            var root = Root<Lobby.CharacterPanel>("CharacterPanel", UILayer.Screen); // Screen (kh├┤ng phß║úi Panel): Trang bß╗ï/V┼⌐ kh├¡ mß╗ƒ chß╗ông l├¬n, Back quay vß╗ü panel
            var close = IconButton(root, "Close", IcBack, 2, A.TL, 20, 14, 64, 64);
            var win = Frame(root, "Window", FrDark, 24, A.R, 24, -30, 470, 620);
            win.GetComponent<Image>().raycastTarget = true;
            var inner = Rt(win, "Inner", A.Stretch, 28, 24, 28, 24); // 414 ├ù 572

            var slot = Frame(inner, "AvatarSlot", SqF, 12, A.TL, 0, 0, 104, 104);
            var avatar = Img(slot, "Avatar", At(KnightIdle, 24, 24), A.C, 0, -2, 72, 72); avatar.preserveAspect = true;
            var name = Txt(inner, "Name", null, "Knight", 32, Paper, TextAlignmentOptions.Left, A.TL, 118, 2, 296, 36);
            var roleIcon = Img(inner, "RoleIcon", Item(5, 18), A.TL, 118, 44, 18, 18); roleIcon.preserveAspect = true;
            var role = Txt(inner, "Role", null, "WARRIOR", 12, Soft, TextAlignmentOptions.Left, A.TL, 142, 44, 200, 18);
            role.characterSpacing = 4;
            var skill = Txt(inner, "Skill", null, "Dual Wield · 12s cooldown", 13, Gold, TextAlignmentOptions.Left, A.TL, 118, 66, 296, 20);
            var desc = Txt(inner, "Desc", null, En("char.knight.desc"), 13, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 112, 414, 40);
            desc.textWrappingMode = TextWrappingModes.Normal;

            var bars = new (string key, string cell, string tf, string sf, int cur, int mx, int step, string val)[] {
                ("stat.hp", "bar_cell_hp", "hpText", "hpSegments", 7, 8, 1, "7"),
                ("stat.armor", "bar_cell_armor", "armorText", "armorSegments", 6, 8, 1, "6"),
                ("stat.mana", "bar_cell_mana", "manaText", "manaSegments", 120, 240, 20, "120"),
                ("stat.dmg", "bar_cell_dmg", "damageText", "damageSegments", 5, 8, 1, "18") };
            var refs = new List<(string, Object)>();
            for (int i = 0; i < bars.Length; i++)
            {
                float x = (i % 2) * 213, y = 160 + (i / 2) * 34;
                Txt(inner, "Label_" + i, bars[i].key, null, 12, Paper, TextAlignmentOptions.Left, A.TL, x, y + 6, 68, 16);
                var seg = SegmentedBarTrack(inner, "Bar_" + i, bars[i].cell, A.TL, x + 68, y + 2, 102, 24, 4, bars[i].cur, bars[i].mx, bars[i].step, 12, 2f);
                var value = Txt(inner, "Val_" + i, null, bars[i].val, 12, Gold, TextAlignmentOptions.Right, A.TL, x + 174, y + 6, 30, 16);
                refs.Add((bars[i].tf, value)); refs.Add((bars[i].sf, seg));
            }

            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTileSmall.prefab");
            var equipTiles = new Object[6]; var weaponTiles = new Object[2];
            for (int i = 0; i < 6; i++) equipTiles[i] = Tile(tilePrefab, inner, "Equip_" + i, A.TL, i * 52, 236, 52, 54, false);
            for (int i = 0; i < 2; i++) weaponTiles[i] = Tile(tilePrefab, inner, "Weapon_" + i, A.TL, 318 + i * 52, 236, 52, 54, false);

            var summary = Txt(inner, "ActiveSummary", null, "Active <color=#9FD0FF>2/2</color> · Frost Field · Arcane Storm", 13, Soft, TextAlignmentOptions.Left, A.TL, 0, 302, 414, 20);
            var skillBox = BuildCharacterSkillBox(inner);

            var playable = Rt(inner, "Playable", A.Stretch, 0, 0, 0, 0);
            var equip = TextButton(playable, "Equip", FrBtn, 24, "common.equip", 20, Paper, A.TL, 0, 410, 202, 76, Item(12, 11), 2);
            var weapons = TextButton(playable, "Weapons", FrBtn, 24, "common.weapons", 20, Paper, A.TL, 212, 410, 202, 76, At("Assets/Project/Art/Sprites/Items/Weapon/Sword/File.png", 16, 144), 1);
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

            // Ch╞░a ra mß║»t (HeroDefinitionSO.released = false): chß╗ë xem giß╗¢i thiß╗çu, kh├┤ng n├║t ch╞íi / mß╗ƒ kh├│a.
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
                ("skillNameText", skill), ("descriptionText", desc),
                ("skillIconImage", skillBox.icon), ("skillTitleText", skillBox.title), ("skillDescText", skillBox.desc), ("innatePassiveText", skillBox.passive),
                ("activeSkillsSummaryText", summary), ("playableGroup", playable.gameObject),
                ("equipButton", equip), ("weaponsButton", weapons), ("enterPortalButton", enter), ("lockedGroup", locked.gameObject),
                ("unlockButton", unlock), ("unlockPriceText", price), ("comingSoonGroup", soon.gameObject),
                ("warriorIcon", Item(5, 18)), ("rangerIcon", Item(5, 4)), ("mageIcon", Item(10, 2)), ("assassinIcon", Item(0, 21)) });
            Wire(panel, refs.ToArray());
            WireArray(panel, "equipTiles", equipTiles);
            WireArray(panel, "weaponTiles", weaponTiles);
            WireArray(panel, "slotSilhouettes", new Object[] { Item(12, 11), Item(1, 0), Item(12, 10), Item(0, 4), Item(3, 13), Item(0, 16) });
            Save(root, UiDir + "CharacterPanel.prefab", AddressKeys.UiPanelCharacter);
        }

        static (Image icon, TMP_Text title, TMP_Text desc, TMP_Text passive) BuildCharacterSkillBox(Transform inner)
        {
            var box = Frame(inner, "SkillBox", FrMid, 12, A.TL, 0, 326, 414, 78);
            var iconSlot = Frame(box, "SkillIconFrame", SqF, 8, A.TL, 10, 15, 48, 48);
            var icon = Img(iconSlot, "SkillIcon", null, A.C, 0, 0, 32, 32);
            icon.preserveAspect = true;
            var title = Txt(box, "SkillTitle", null, "Dual Wield · 12s", 12, Gold, TextAlignmentOptions.Left, A.TL, 68, 6, 336, 16);
            var desc = Txt(box, "SkillDesc", null, string.Empty, 11, Paper, TextAlignmentOptions.TopLeft, A.TL, 68, 23, 336, 26);
            desc.textWrappingMode = TextWrappingModes.Normal;
            var passive = Txt(box, "InnatePassive", null, string.Empty, 11, Green, TextAlignmentOptions.TopLeft, A.TL, 68, 51, 336, 22);
            passive.textWrappingMode = TextWrappingModes.Normal;
            return (icon, title, desc, passive);
        }

        /// <summary>Chèn hoặc dựng lại SkillBox trên CharacterPanel.prefab hiện có (giữ nguyên phần UI còn lại).</summary>
        [MenuItem("Tools/Dungeon Gathering/UI/Patch CharacterPanel Skill Box")]
        public static void PatchCharacterPanelSkillBox()
        {
            Prepare();
            string path = UiDir + "CharacterPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var inner = root.transform.Find("Window/Inner") as RectTransform;
                if (inner == null) { Debug.LogError("[UIDesign] CharacterPanel thiếu node 'Window/Inner'"); return; }

                var old = inner.Find("SkillBox");
                int sibling = old != null ? old.GetSiblingIndex() : -1;
                if (sibling < 0)
                {
                    var summary = inner.Find("ActiveSummary");
                    sibling = summary != null ? summary.GetSiblingIndex() + 1 : inner.childCount;
                }
                if (old != null) Object.DestroyImmediate(old.gameObject);

                var sb = BuildCharacterSkillBox(inner);
                var boxTransform = sb.title.transform.parent;
                boxTransform.SetSiblingIndex(sibling);
                SetLayerRecursive(boxTransform);

                Wire(root.GetComponent<Lobby.CharacterPanel>(),
                    ("skillIconImage", sb.icon),
                    ("skillTitleText", sb.title),
                    ("skillDescText", sb.desc),
                    ("innatePassiveText", sb.passive));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[UIDesign] Đã dựng SkillBox trên " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }


        static readonly string KnightIdle = "Assets/Project/Art/Sprites/Characters/FantasyKnights/01Knight.png";

        /// <summary>Khung chung 3 cß╗Öt cß╗ºa Equipment/Weapon. Trß║ú vß╗ü (root, inner tr├íi, inner giß╗»a, inner phß║úi).</summary>
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

        /// <summary>Cß╗Öt chi tiß║┐t (tr├íi) d├╣ng chung. Trß║ú c├íc tham chiß║┐u ─æß╗â Wire.</summary>
        static List<(string, Object)> DetailColumn(RectTransform li, string flatKey, string passiveKey, string activeKey, float actionWidth)
        {
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");
            var group = Rt(li, "DetailGroup", A.Stretch, 0, 0, 0, 0);
            var tile = Tile(tilePrefab, group, "Tile", A.TL, 0, 0, 104, 108, true);
            var name = Txt(group, "Name", null, "Item", 24, Paper, TextAlignmentOptions.Left, A.TL, 118, 6, 198, 30);
            var meta = Txt(group, "Meta", null, "Helmet · Rarity C", 13, Soft, TextAlignmentOptions.Left, A.TL, 118, 40, 198, 18);
            var state = Txt(group, "State", null, "", 12, Gold, TextAlignmentOptions.Left, A.TL, 118, 62, 198, 18);
            var box = Frame(group, "StatsBox", FrMid, 12, A.TL, 0, 124, 316, 104);
            if (flatKey != null) Txt(box, "Label", flatKey, null, 11, Soft, TextAlignmentOptions.Left, A.TL, 12, 8, 290, 16);
            var stats = Txt(box, "Stats", null, "+1 Armor", 18, Green, TextAlignmentOptions.TopLeft, A.TL, 12, flatKey != null ? 28 : 10, 292, 66);
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
            group.gameObject.SetActive(false);   // mß║╖c ─æß╗ïnh: ch╞░a chß╗ìn m├│n ΓåÆ hiß╗çn gß╗úi ├╜
            return new List<(string, Object)> {
                ("detailGroup", group.gameObject), ("emptyHint", hint.gameObject), ("detailTile", tile), ("detailName", name), ("detailMeta", meta),
                ("detailState", state), ("detailStats", stats), ("detailPassive", passive), ("detailActive", active), ("detailWarn", warn),
                ("actionButton", action), ("actionFrame", action.GetComponent<Image>()), ("actionLabel", action.transform.Find("Label").GetComponent<LocalizedTMPText>()),
                ("frameOn", FrLight), ("frameOff", FrDark), ("frameBlocked", FrDark) };
        }

        /// <summary>Cß╗Öt t├║i (phß║úi): tab lß╗ìc + l╞░ß╗¢i cuß╗Ön 3 cß╗Öt ItemTile ├ù4.</summary>
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

        /// <summary>L╞░ß╗¢i chß╗ë sß╗æ ß╗ƒ cß╗Öt giß╗»a: h├áng = khung f-mid-s 204├ù40, nh├ún tr├íi, gi├í trß╗ï + ch├¬nh lß╗çch phß║úi.</summary>
        static void StatGrid(RectTransform mi, float top, (string key, string raw)[] rows, Object target)
        {
            var so = new SerializedObject(target);
            var arr = so.FindProperty("statRows"); arr.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var cell = Frame(mi, "Stat_" + i, FrMid, 12, A.TL, (i % 2) * 220, top + (i / 2) * 46, 204, 40);
                Txt(cell, "Label", rows[i].key, rows[i].raw, 12, Soft, TextAlignmentOptions.Left, A.L, 10, 0, 100, 20);
                var v = Txt(cell, "Value", null, "0", 18, Paper, TextAlignmentOptions.Right, A.R, 54, 0, 70, 24);
                var d = Txt(cell, "Delta", null, "", 15, Paper, TextAlignmentOptions.Right, A.R, 10, 0, 44, 20);
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

            var portrait = Img(mi, "Portrait", At(KnightIdle, 24, 24), A.T, 0, 40, 128, 128); portrait.preserveAspect = true;
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
            var slot2 = TextButton(li.Find("DetailGroup"), "ToSlot2", FrBtn, 24, "weapon.to_slot2", 20, Paper, A.TL, 163, 496, 153, 76, null, 0);

            var portrait = Img(mi, "Portrait", At(KnightIdle, 24, 24), A.T, 0, 40, 128, 128); portrait.preserveAspect = true;
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
            var gi = Rt(grid, "Inner", A.Stretch, 24, 24, 24, 24); // 712 ├ù 552
            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");
            var screen = root.GetComponent<Play.ShopScreen>();
            var so = new SerializedObject(screen);
            var cards = so.FindProperty("cards"); cards.arraySize = 6;
            for (int i = 0; i < 6; i++)
            {
                bool selCard = i == 0;
                var c = Frame(gi, "Card_" + i, selCard ? FrLight : FrMid, 24, A.TL, (i % 3) * 241, (i / 3) * 282, 229, 270);
                var img = c.GetComponent<Image>(); img.raycastTarget = true;
                var b = c.gameObject.AddComponent<Button>(); b.targetGraphic = img;
                var t = Tile(tilePrefab, c, "Tile", A.T, 0, 24, 104, 108, true);
                var n = Txt(c, "Name", null, "Item", 20, selCard ? Ink : Paper, TextAlignmentOptions.Center, A.T, 0, 136, 180, 44); n.textWrappingMode = TextWrappingModes.Normal;
                var k = Txt(c, "Kind", null, "Weapon · A", 16, selCard ? Ink : Soft, TextAlignmentOptions.Center, A.T, 0, 180, 180, 22);
                Icon(c, "Coin", Coin, 2, A.B, -22, 30);
                var p = Txt(c, "Price", null, "120", 22, selCard ? Ink : Paper, TextAlignmentOptions.Left, A.B, 32, 28, 70, 28);
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
            var di = Rt(det, "Inner", A.Stretch, 24, 24, 24, 24); // 408 ├ù 552
            refs.Add(("detailTile", Tile(tilePrefab, di, "Tile", A.TL, 0, 0, 104, 108, true)));
            refs.Add(("detailName", Txt(di, "Name", null, "Item", 28, Paper, TextAlignmentOptions.Left, A.TL, 118, 14, 290, 36)));
            refs.Add(("detailKind", Txt(di, "Kind", null, "Weapon · A", 18, Soft, TextAlignmentOptions.Left, A.TL, 118, 52, 290, 24)));
            var box = Frame(di, "StatsBox", FrMid, 12, A.TL, 0, 124, 408, 92);
            var stats = Txt(box, "Stats", null, "22 damage", 20, Green, TextAlignmentOptions.TopLeft, A.TL, 12, 10, 380, 70);
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
            var close = IconButton(panel, "Close", IcClose, 2, A.TR, 14, 14, 48, 48);
            Wire(root.GetComponent<SettingsPopup>(), ("closeButton", close), ("panel", panel.GetComponent<SettingsPanel>()));
            Save(root, UiDir + "SettingsPopup.prefab", AddressKeys.UiPopupSettings);
        }

    }
}