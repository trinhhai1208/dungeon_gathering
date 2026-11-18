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
        static void BuildPlayHud()
        {
            var root = Root<Play.PlayHud>("PlayHud", UILayer.Hud);
            SetBool(root.GetComponent<UIScreen>(), "closeOnBack", false);

            var stats = Frame(root, "Stats", FrDark, 12, A.TL, 10, 10, 324, 114);
            var aslot = Frame(stats, "AvatarSlot", SqF, 12, A.TL, 12, 24, 64, 66);
            var avatar = Img(aslot, "Avatar", At(KnightIdle, 24, 24), A.C, 0, -2, 48, 48);
            avatar.preserveAspect = true;
            var rows = new (string icon, string cell, int cur, int mx, int step, string val)[] {
                ("icon_hp", "bar_cell_hp", 6, 7, 1, "6/7"),
                ("icon_armor", "bar_cell_armor", 3, 6, 1, "3/6"),
                ("icon_mana", "bar_cell_mana", 100, 120, 20, "100/120") };
            var segs = new SegmentedBar[3]; var texts = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float y = 14 + i * 30;
                Icon(stats, "Icon_" + i, G(rows[i].icon), 1.4f, A.TL, 84, y + 3);
                segs[i] = SegmentedBarTrack(stats, "Bar_" + i, rows[i].cell, A.TL, 108, y, 152, 22, 4, rows[i].cur, rows[i].mx, rows[i].step, 14, 2f);
                texts[i] = Txt(stats, "Val_" + i, null, rows[i].val, 12, Paper, TextAlignmentOptions.Left, A.TL, 266, y + 3, 54, 16);
            }
            var coin = Frame(root, "Coin", FrDark, 8, A.TL, 10, 130, 88, 38);
            Icon(coin, "Icon", Coin, 2, A.L, 8, 0);
            var coinText = Txt(coin, "Value", null, "86", 16, Paper, TextAlignmentOptions.Left, A.L, 34, 0, 50, 24);
            var floor = Frame(root, "Floor", FrDark, 8, A.TL, 106, 130, 122, 38);
            var floorText = Txt(floor, "Label", "hud.floor", null, 14, Gold, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);

            var minimap = Frame(root, "Minimap", FrDark, 24, A.TR, 82, 10, 176, 130);
            var pause = IconButton(root, "Pause", IcPause, 2, A.TR, 10, 10, 64, 64);

            var joyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "VirtualJoystick.prefab");
            if (joyPrefab != null)
            {
                var joy = (GameObject)PrefabUtility.InstantiatePrefab(joyPrefab, root.transform);
                var jr = (RectTransform)joy.transform; Place(jr, A.BL, 48, 40, 160, 160);
            }

            var cluster = Rt(root, "Actions", A.BR, 28, 28, 344, 240);

            // D╞░ß╗¢i - Phß║úi (0, 0): N├║t ─æ├ính th╞░ß╗¥ng
            var attack = IconButton(cluster, "Attack", IcSwordBtn, 4, A.BR, 0, 0, 104, 112);
            var vb = attack.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb, "action", (int)VirtualAction.Attack);
            Wire(vb, ("pressVisual", attack.transform.GetChild(0)));

            // Tr├¬n - Tr├íi (114, 122): ├ö ─æß╗òi kiß║┐m
            var swap = Rt(cluster, "WeaponSlot", A.BR, 114, 122, 104, 108);
            var sq = Img(swap, "Square", SqF, A.Stretch, 0, 0, 0, 0); sq.raycastTarget = true;
            var swapBtn = swap.gameObject.AddComponent<Button>(); swapBtn.targetGraphic = sq;
            var wicon = Img(swap, "Weapon", At("Assets/Project/Art/Sprites/Items/Weapon/Sword/File.png", 16, 144), A.C, 0, 2, 64, 64); wicon.preserveAspect = true;
            Icon(swap, "SwapIcon", IcSwap, 4, A.TL, -8, -8);
            var swapVb = swap.gameObject.AddComponent<VirtualButton>();
            SetEnum(swapVb, "action", (int)VirtualAction.SwapWeapon);
            Wire(swapVb, ("pressVisual", wicon.rectTransform));

            var tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTile.prefab");

            // D╞░ß╗¢i - Tr├íi (114, 0): ├ö k├¡ch hoß║ít trang bß╗ï 1
            var s1 = Tile(tilePrefab, cluster, "ActiveSkill1", A.BR, 114, 0, 104, 108, false);
            var vb1 = s1.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb1, "action", (int)VirtualAction.ItemSkill1);

            // Tr├¬n - Phß║úi (0, 122): ├ö k├¡ch hoß║ít trang bß╗ï 2
            var s2 = Tile(tilePrefab, cluster, "ActiveSkill2", A.BR, 0, 122, 104, 108, false);
            var vb2 = s2.gameObject.AddComponent<VirtualButton>();
            SetEnum(vb2, "action", (int)VirtualAction.ItemSkill2);

            // B├¬n - Tr├íi (228, 60): N├║t kß╗╣ n─âng nh├ón vß║¡t (E / Character Skill) ΓÇö khung tr├▓n ri├¬ng, kh├íc ├┤ vß║¡t phß║⌐m
            var cs = BuildCharacterSkillButton(cluster);

            // N├║t t╞░╞íng t├íc (mß╗ƒ r╞░╞íng, cß╗¡a h├áng...): chß╗» to, ─æß╗ìc r├╡ cß║ú khi canvas bß╗ï l├ám tr├▓n xuß╗æng ├ù0.5 tr├¬n m├án nhß╗Å
            var interact = Frame(root, "InteractButton", FrLight, 12, A.B, 0, 128, 360, 76);
            var ibImg = interact.GetComponent<Image>(); ibImg.raycastTarget = true;
            var ibPrompt = Txt(interact, "Prompt", "interact.open_chest", null, 32, Ink, TextAlignmentOptions.Center, A.Stretch, 14, 6, 14, 6);
            ibPrompt.fontStyle = FontStyles.Bold;
            ibPrompt.enableAutoSizing = true; ibPrompt.fontSizeMin = 20; ibPrompt.fontSizeMax = 32; // c├óu d├ái (cß╗¡a h├áng) kh├┤ng tr├án khung
            var ibVb = interact.gameObject.AddComponent<VirtualButton>();
            SetEnum(ibVb, "action", (int)VirtualAction.Interact);
            Wire(ibVb, ("pressVisual", ibPrompt.rectTransform));
            interact.gameObject.SetActive(false);

            var boss = Frame(root, "BossBar", FrDark, 12, A.T, 0, 16, 480, 44);
            var bossSeg = SegmentedBarTrack(boss, "Segments", "bar_cell_hp", A.Stretch, 8, 8, 8, 8, 4, 8, 10, 1, 10, 3f);
            bossSeg.SetNormalized(0.8f, 10);
            var bossName = Txt(boss, "Name", null, "Vampire Lord", 14, Paper, TextAlignmentOptions.Center, A.Stretch, 0, -4, 0, -4);
            boss.gameObject.SetActive(false);

            var toastBg = Frame(root, "Toast", FrDark, 12, A.B, 0, 220, 600, 60);
            var toast = Txt(toastBg, "Text", null, "", 26, Paper, TextAlignmentOptions.Center, A.Stretch, 0, 0, 0, 0);
            toastBg.gameObject.SetActive(false);

            Wire(root.GetComponent<Play.PlayHud>(),
                ("avatarImage", avatar), ("hpSegments", segs[0]), ("armorSegments", segs[1]), ("manaSegments", segs[2]),
                ("hpText", texts[0]), ("armorText", texts[1]), ("manaText", texts[2]), ("coinText", coinText),
                ("floorLabel", floorText.GetComponent<LocalizedTMPText>()), ("minimapFrame", minimap), ("pauseButton", pause),
                ("bossBar", boss.gameObject), ("bossSegments", bossSeg), ("bossName", AddLoc(bossName, null)),
                ("attackButton", attack), ("swapWeaponButton", swapBtn), ("currentWeaponIcon", wicon),
                ("skillSlot1", s1), ("skillSlot2", s2),
                ("characterSkillIcon", cs.icon), ("characterSkillCooldown", cs.cooldown), ("characterSkillCooldownText", cs.cdText),
                ("characterSkillGlow", cs.glow), ("characterSkillManaBadge", cs.manaBadge), ("characterSkillManaText", cs.manaText),
                ("interactButton", interact.gameObject), ("interactPrompt", ibPrompt.GetComponent<LocalizedTMPText>()),
                ("toast", AddLoc(toast, null)));
            Save(root, UiDir + "PlayHud.prefab", AddressKeys.UiHudPlay);
        }

        /// <summary>
        /// N├║t kß╗╣ n─âng nh├ón vß║¡t: v├▓ng s├íng (pulse khi sß║╡n s├áng) ┬╖ ─æ─⌐a nß╗ün ┬╖ icon skill ┬╖ lß╗¢p hß╗ôi chi├¬u radial ┬╖
        /// khung tr├▓n viß╗ün v├áng 4 ngß╗ìc ┬╖ sß╗æ gi├óy hß╗ôi ┬╖ nh├ún mana. Sprite pixel 26px hiß╗ân thß╗ï ├ù4 = 104.
        /// </summary>
        internal static (RectTransform root, Image icon, Image cooldown, TextMeshProUGUI cdText, Image glow, GameObject manaBadge, TextMeshProUGUI manaText)
            BuildCharacterSkillButton(RectTransform cluster)
        {
            const float K = 4f;
            var root = Rt(cluster, "CharacterSkill", A.BR, 228, 60, 104, 108);
            var body = Rt(root, "Body", A.Stretch, 0, 0, 0, 0); // nh├║n khi bß║Ñm (pressVisual)

            var glow = Img(body, "ReadyGlow", G("skill_btn_glow"), A.C, 0, 2, 32 * K, 32 * K);
            glow.color = new Color(1f, 1f, 1f, 0f);

            var disc = Img(body, "Disc", G("skill_btn_disc"), A.C, 0, 2, 26 * K, 26 * K);
            disc.raycastTarget = true; // v├╣ng bß║Ñm h├¼nh tr├▓n
            var btn = root.gameObject.AddComponent<Button>(); btn.targetGraphic = disc;
            btn.transition = Selectable.Transition.None;

            var icon = Img(body, "Icon", null, A.C, 0, 2, 16 * K, 16 * K); // icon chi├¬u 16px ├ù4
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

            // Nh├ún mana: vi├¬n thuß╗æc tß╗æi ß╗ƒ m├⌐p d╞░ß╗¢i, ─æ├¿ l├¬n viß╗ün
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

        /// <summary>Chß╗ë dß╗▒ng lß║íi n├║t kß╗╣ n─âng nh├ón vß║¡t trong PlayHud.prefab hiß╗çn c├│ (giß╗» nguy├¬n phß║ºn HUD kh├íc).</summary>
        [MenuItem("Tools/Dungeon Gathering/UI/Rebuild Character Skill Button (PlayHud)")]
        public static void RebuildCharacterSkillButton()
        {
            Prepare();
            string path = UiDir + "PlayHud.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var cluster = root.transform.Find("Actions") as RectTransform;
                if (cluster == null) { Debug.LogError("[UIDesign] PlayHud thiß║┐u node 'Actions'"); return; }
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
                Debug.Log("[UIDesign] ─É├ú dß╗▒ng lß║íi n├║t Character Skill trong " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static void BuildPause()
        {
            var root = Root<Play.PauseScreen>("PauseScreen", UILayer.Screen);
            var dim = Img(root, "Dim", null, A.Stretch, 0, 0, 0, 0); dim.color = new Color(0.106f, 0.078f, 0.125f, 0.85f); dim.raycastTarget = true;
            var block = Rt(root, "Block", A.C, 0, 0, 1020, 600);

            // --- Tß║áM Dß╗¬NG
            var left = Frame(block, "PausePanel", FrDark, 24, A.TL, 0, 0, 470, 600);
            var li = Rt(left, "Inner", A.Stretch, 28, 24, 28, 24); // 414 ├ù 552
            Txt(li, "Title", "pause.title", null, 44, Paper, TextAlignmentOptions.Left, A.TL, 0, 0, 414, 48);
            var info = Frame(li, "Info", FrMid, 12, A.TL, 0, 64, 414, 128);
            var hero = Icon(info, "Hero", At(KnightIdle, 24, 24), 1.8f, A.L, 12, 0);
            var labels = new[] { "pause.class", "stat.time", "stat.kills", "common.coin" };
            var values = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                Txt(info, "L" + i, labels[i], null, 14, Soft, TextAlignmentOptions.Left, A.TL, 98, 6 + i * 24, 114, 20);
                values[i] = Txt(info, "V" + i, null, new[] { "Knight · Warrior", "06:42", "23", "86" }[i], 14, Paper, TextAlignmentOptions.Left, A.TL, 214, 6 + i * 24, 170, 20);
            }
            var note = Txt(li, "GearNote", "pause.gear_note", null, 13, Soft, TextAlignmentOptions.TopLeft, A.TL, 0, 204, 414, 38);
            note.textWrappingMode = TextWrappingModes.Normal;

            var lootBox = Frame(li, "LootBox", FrMid, 12, A.TL, 0, 248, 414, 144);
            var lootView = Rt(lootBox, "Viewport", A.Stretch, 14, 12, 14, 12);
            lootView.gameObject.AddComponent<RectMask2D>();
            var lvImg = lootView.gameObject.AddComponent<Image>(); lvImg.color = Color.clear; lvImg.raycastTarget = true;
            var loot = Rt(lootView, "Loot", A.TL, 0, 0, 386, 120);
            var lg = loot.gameObject.AddComponent<GridLayoutGroup>();
            lg.cellSize = new Vector2(52, 54); lg.spacing = new Vector2(12, 6);
            lg.padding = new RectOffset(7, 7, 2, 2);
            lg.constraint = GridLayoutGroup.Constraint.FixedColumnCount; lg.constraintCount = 6;
            var lfit = loot.gameObject.AddComponent<ContentSizeFitter>(); lfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lscroll = lootView.gameObject.AddComponent<ScrollRect>();
            lscroll.content = loot; lscroll.horizontal = false; lscroll.viewport = lootView; lscroll.movementType = ScrollRect.MovementType.Clamped;

            var resume = TextButton(li, "Resume", FrLight, 24, "common.continue", 26, Ink, A.TL, 0, 406, 414, 72, null, 0);
            var quit = TextButton(li, "Quit", FrBtn, 24, "pause.quit", 20, Paper, A.TL, 0, 488, 414, 64, null, 0);

            // --- C├ÇI ─Éß║╢T (SettingsPanel d├╣ng chung)
            BuildSettings(block, A.TL, 510, 0);

            Wire(root.GetComponent<Play.PauseScreen>(),
                ("resumeButton", resume), ("abandonButton", quit),
                ("characterPortrait", hero), ("characterClassText", values[0]),
                ("runTimeText", values[1]), ("killsText", values[2]), ("coinsText", values[3]),
                ("lootGrid", loot),
                ("lootTilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTileSmall.prefab").GetComponent<ItemTile>()));
            Save(root, UiDir + "PauseScreen.prefab", AddressKeys.UiScreenPause);
        }

        /// <summary>Panel C├ÇI ─Éß║╢T 510├ù600 (UI-Pause, cß╗Öt phß║úi).</summary>
        static RectTransform BuildSettings(Transform parent, A a, float x, float y)
        {
            var panel = Frame(parent, "SettingsPanel", FrDark, 24, a, x, y, 510, 600);
            var sp = panel.gameObject.AddComponent<SettingsPanel>();
            var si = Rt(panel, "Inner", A.Stretch, 28, 24, 28, 24); // 454 ├ù 552
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
                var codeTxt = Txt(b, "Code", null, label, 16, code == "en" ? Ink : Paper, TextAlignmentOptions.Left, A.L, 60, 0, 34, 24);
                var lb = b.gameObject.AddComponent<LanguageButton>();
                var lso = new SerializedObject(lb);
                lso.FindProperty("languageCode").stringValue = code;
                lso.FindProperty("frame").objectReferenceValue = img;
                lso.FindProperty("selectedFrame").objectReferenceValue = FrLight;
                lso.FindProperty("normalFrame").objectReferenceValue = FrDark;
                lso.FindProperty("label").objectReferenceValue = codeTxt;
                lso.FindProperty("selectedTextColor").colorValue = Ink;
                lso.FindProperty("normalTextColor").colorValue = Paper;
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
            else Debug.LogWarning("[UIDesign] UIRoot kh├┤ng c├│ node Backdrop.");

            // Node SafeArea bß╗ìc Hud, Screen, Panel, Popup, Toast (UI_SYSTEM ┬º2, ┬º3.3); Backdrop, Overlay, InputBlocker ß╗ƒ ngo├ái SafeArea.
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
            var lin = Rt(left, "Inner", A.Stretch, 26, 24, 26, 24); // 528 ├ù 272
            string[] keys = { "stat.floor", "stat.time", "stat.kills", "stat.rooms" };
            var values = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                var c = Frame(lin, "Stat_" + i, FrMid, 12, A.TL, i * 134, 0, 126, 72);
                Txt(c, "Label", keys[i], null, 12, Soft, TextAlignmentOptions.Left, A.TL, 12, 10, 104, 16);
                values[i] = Txt(c, "Value", null, new[] { "3-5", "14:32", "86", "15" }[i], 26, Paper, TextAlignmentOptions.Left, A.TL, 12, 28, 104, 30);
            }
            var badge = Frame(lin, "Badge", FrLight, 8, A.TL, 0, 84, 210, 34);
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
            var totalColor = victory ? Ink : Paper;
            Txt(total, "Label", "common.total", null, 16, totalColor, TextAlignmentOptions.Left, A.L, 10, 0, 160, 24);
            Icon(total, "Coin", Coin, 3, A.R, 120, 0);
            var coins = Txt(total, "Value", null, victory ? "+346" : "+57", 34, victory ? Ink : Gold, TextAlignmentOptions.Right, A.R, 10, 0, 100, 40);

            var lobby = TextButton(root, "Lobby", FrBtn, 24, victory ? "win.to_lobby" : "common.to_lobby", 22, Paper, A.B, -170, 30, 320, 76, null, 0);
            var next = TextButton(root, "Continue", FrLight, 24, victory ? "win.next" : "common.retry", 22, Ink, A.B, 170, 30, 320, 76, null, 0);

            var scr = root.GetComponent<Results.RunResultScreen>();
            Wire(scr, ("lobbyButton", lobby), ("continueButton", next), ("floorText", values[0]), ("timeText", values[1]),
                ("killsText", values[2]), ("coinsText", coins), ("lootGrid", loot),
                ("lootTilePrefab", AssetDatabase.LoadAssetAtPath<GameObject>(UiDir + "ItemTileSmall.prefab").GetComponent<ItemTile>()));
            WireArray(scr, "coinRowLabels", rowLabels);
            WireArray(scr, "coinRowValues", rowValues);
            Save(root, UiDir + name + ".prefab", victory ? AddressKeys.UiScreenVictory : AddressKeys.UiScreenDefeat);
        }

        /// <summary>Dß╗▒ng lß║íi con cß╗ºa LoadingOverlay trong UIRoot theo UI-Loading.dc.html (giß╗» component ─æß╗â tham chiß║┐u tß╗½ Systems kh├┤ng g├úy).</summary>
        static void BuildLoading()
        {
            const string path = "Assets/Project/Core/Prefabs/UIRoot.prefab";
            var uiRoot = PrefabUtility.LoadPrefabContents(path);
            var overlay = uiRoot.GetComponentInChildren<LoadingOverlay>(true);
            if (overlay == null) { Debug.LogWarning("[UIDesign] UIRoot kh├┤ng c├│ LoadingOverlay."); PrefabUtility.UnloadPrefabContents(uiRoot); return; }
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
            var runner = Img(pg, "Runner", At(KnightIdle, 24, 24), A.TL, 0, 0, 64, 64);
            var bar = Frame(pg, "Bar", FrDark, 12, A.TL, 0, 72, 720, 44);
            var fill = Img(bar, "Fill", G("bar_strip_green"), A.Stretch, 6, 6, 6, 6); MakeFilled(fill); fill.fillAmount = 0.6f;
            var step = Txt(pg, "Step", "loading.step_core", null, 20, Paper, TextAlignmentOptions.Left, A.TL, 0, 128, 520, 26);
            var pct = Txt(pg, "Percent", null, "60%", 24, Title, TextAlignmentOptions.Right, A.TR, 0, 124, 140, 30);
            Txt(pg, "FirstTime", "loading.first_time", null, 14, new Color32(0xc9, 0xcd, 0xd8, 0xff), TextAlignmentOptions.Left, A.TL, 0, 162, 720, 20);

            var tip = Rt(o, "Tip", A.B, 0, 44, 840, 52);
            var tipFrame = Frame(tip, "Frame", FrDark, 12, A.Stretch, 0, 0, 0, 0);
            var tag = Frame(tip, "Tag", FrLight, 8, A.L, 8, 0, 68, 36);
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

    }
}