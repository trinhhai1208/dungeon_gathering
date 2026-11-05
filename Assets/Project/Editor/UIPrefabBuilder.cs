using System.IO;
using System.Linq;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    public static class UIPrefabBuilder
    {
        const string PrefabDir = "Assets/Project/Prefabs/UI";
        const string ButtonsSheet = "Assets/Project/Art/Sprites/UI/HUD/Basic Buttons 3 Fv.png";
        const string FontPath = "Assets/Project/Art/Fonts/PixelifySans-Regular SDF.asset";
        const string CoinPath = "Assets/Project/Art/Sprites/Items/Coins/coin_1.png";
        const string ArrowPath = "Assets/Project/Art/Sprites/UI/Indicators/arrow_4.png";

        [MenuItem("Tools/Dungeon Gathering/Build All UI Prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(PrefabDir);
            BuildMenuScreen();
            BuildLobbyHud();
            BuildCharacterPanel();
            RegisterAddressables();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIPrefabBuilder] All UI prefabs built & registered successfully!");
        }

        public static void BuildMenuScreen()
        {
            string path = $"{PrefabDir}/MenuScreen.prefab";
            var sheet = AssetDatabase.LoadAllAssetsAtPath(ButtonsSheet).OfType<Sprite>().ToDictionary(s => s.name);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinPath);
            var playSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/ui_btn_play.png");
            var gearSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/ui_gear_btn.png");
            var soundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/ui_btn_sound.png");
            var musicSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/ui_btn_music.png");

            Sprite darkFrame = sheet["Basic Buttons 3 Fv_141"];

            var root = CreateRoot("MenuScreen", typeof(Menu.MenuScreen));
            var menuComp = root.GetComponent<Menu.MenuScreen>();

            // 1. Tap anywhere
            var tapGo = new GameObject("TapAnywhereButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tapGo.transform.SetParent(root.transform, false);
            Stretch(tapGo.GetComponent<RectTransform>());
            var tapImg = tapGo.GetComponent<Image>();
            tapImg.color = new Color(0, 0, 0, 0);
            tapImg.raycastTarget = true;
            var tapBtn = tapGo.GetComponent<Button>();

            // 2. CoinGroup (Rule C: Top-Left)
            var coinGroup = CreateUIObject("CoinGroup", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(170f, 64f), new Vector2(24f, -24f));
            var coinBg = coinGroup.AddComponent<Image>();
            coinBg.sprite = darkFrame;
            coinBg.type = Image.Type.Sliced;
            var coinIcon = AddImage(coinGroup.transform, "CoinIcon", coinSprite, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(32f, 32f), new Vector2(16f, 0f));
            var coinText = AddText(coinGroup.transform, "CoinText", "1,240", font, 28f, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100f, 36f), new Vector2(56f, 0f), TextAlignmentOptions.MidlineLeft);

            // 3. SettingsButton (Rule C: Top-Right)
            var settingsBtnGo = AddButton(root.transform, "SettingsButton", gearSprite ?? darkFrame, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(66f, 66f), new Vector2(-24f, -24f));
            var settingsBtn = settingsBtnGo.GetComponent<Button>();

            // 4. LogoGroup (Rule D: Center-Top)
            var logoGroup = CreateUIObject("LogoGroup", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(720f, 220f), new Vector2(0f, -112f));
            var dungeonText = AddText(logoGroup.transform, "DungeonText", "DUNGEON", font, 104f, new Color(0.95f, 0.70f, 0.24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640f, 110f), new Vector2(0f, 40f));
            dungeonText.fontStyle = FontStyles.Bold;
            var gatheringText = AddText(logoGroup.transform, "GatheringText", "GATHERING", font, 64f, new Color(0.94f, 0.90f, 0.86f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(640f, 70f), new Vector2(0f, -40f));
            gatheringText.characterSpacing = 10f;

            // 5. CharactersRow (Rule D: Center)
            var charsRow = CreateUIObject("CharactersRow", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(620f, 100f), new Vector2(0f, -72f));

            // 6. PlayButtonGroup (Rule D: Center-Bottom)
            var playGroup = CreateUIObject("PlayButtonGroup", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(300f, 150f), new Vector2(0f, 100f));
            var playBtnGo = AddButton(playGroup.transform, "PlayButton", playSprite ?? darkFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(104f, 104f), new Vector2(0f, 20f));
            var playBtn = playBtnGo.GetComponent<Button>();
            var tapText = AddText(playGroup.transform, "TapToPlayText", "TAP TO PLAY", font, 22f, new Color(0.85f, 0.80f, 0.84f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280f, 30f), new Vector2(0f, -46f));
            tapText.characterSpacing = 4f;
            var locTap = tapText.gameObject.AddComponent<LocalizedTMPText>();
            locTap.SetKey("menu.tap");

            // 7. AudioButtonsGroup (Rule C: Bottom-Right)
            var audioGroup = CreateUIObject("AudioButtonsGroup", root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(122f, 56f), new Vector2(-24f, 20f));
            var soundBtnGo = AddButton(audioGroup.transform, "SoundButton", soundSprite ?? darkFrame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(56f, 56f), new Vector2(0f, 0f));
            var musicBtnGo = AddButton(audioGroup.transform, "MusicButton", musicSprite ?? darkFrame, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(56f, 56f), new Vector2(0f, 0f));

            // Wire Serialized Properties
            var so = new SerializedObject(menuComp);
            so.FindProperty("playButton").objectReferenceValue = playBtn;
            so.FindProperty("tapAnywhere").objectReferenceValue = tapBtn;
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            so.FindProperty("soundButton").objectReferenceValue = soundBtnGo.GetComponent<Button>();
            so.FindProperty("musicButton").objectReferenceValue = musicBtnGo.GetComponent<Button>();
            so.FindProperty("soundIcon").objectReferenceValue = soundBtnGo.GetComponent<Image>();
            so.FindProperty("musicIcon").objectReferenceValue = musicBtnGo.GetComponent<Image>();
            so.FindProperty("coinText").objectReferenceValue = coinText;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[UIPrefabBuilder] Saved {path}");
        }

        public static void BuildLobbyHud()
        {
            string path = $"{PrefabDir}/LobbyHud.prefab";
            var sheet = AssetDatabase.LoadAllAssetsAtPath(ButtonsSheet).OfType<Sprite>().ToDictionary(s => s.name);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinPath);
            var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
            Sprite darkFrame = sheet["Basic Buttons 3 Fv_141"];

            var root = CreateRoot("LobbyHud", typeof(Lobby.LobbyHud));
            var hudComp = root.GetComponent<Lobby.LobbyHud>();

            // Set UIScreen layer to Hud
            var soScreen = new SerializedObject(hudComp);
            soScreen.FindProperty("layer").enumValueIndex = (int)UILayer.Hud;
            soScreen.FindProperty("closeOnBack").boolValue = false;
            soScreen.ApplyModifiedPropertiesWithoutUndo();

            // 1. BackButton (Rule C: Top-Left)
            var backBtnGo = AddButton(root.transform, "BackButton", darkFrame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(52f, 52f), new Vector2(20f, -14f));
            backBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
            if (arrowSprite != null)
            {
                var arrowImg = AddImage(backBtnGo.transform, "ArrowIcon", arrowSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f), Vector2.zero);
                arrowImg.rectTransform.localEulerAngles = new Vector3(0, 0, 180f); // point left
            }

            // 2. CoinGroup (Rule C: Top-Right)
            var coinGroup = CreateUIObject("CoinGroup", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(150f, 52f), new Vector2(-20f, -14f));
            var coinBg = coinGroup.AddComponent<Image>();
            coinBg.sprite = darkFrame;
            coinBg.type = Image.Type.Sliced;
            var coinIcon = AddImage(coinGroup.transform, "CoinIcon", coinSprite, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 26f), new Vector2(12f, 0f));
            var coinText = AddText(coinGroup.transform, "CoinText", "1,240", font, 22f, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(90f, 30f), new Vector2(46f, 0f), TextAlignmentOptions.MidlineLeft);

            // 3. HintPickHero (Rule C: Bottom-Center)
            var hintGroup = CreateUIObject("HintPickHero", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(480f, 48f), new Vector2(0f, 22f));
            var hintBg = hintGroup.AddComponent<Image>();
            hintBg.sprite = darkFrame;
            hintBg.type = Image.Type.Sliced;
            var hintText = AddText(hintGroup.transform, "HintText", "CHOOSE A HERO TO START", font, 24f, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 36f), Vector2.zero);
            hintText.characterSpacing = 2f;
            var locHint = hintText.gameObject.AddComponent<LocalizedTMPText>();
            locHint.SetKey("lobby.pick_char");

            // Wire Serialized Properties
            var so = new SerializedObject(hudComp);
            so.FindProperty("backButton").objectReferenceValue = backBtnGo.GetComponent<Button>();
            so.FindProperty("coinText").objectReferenceValue = coinText;
            so.FindProperty("hintPickHero").objectReferenceValue = hintGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[UIPrefabBuilder] Saved {path}");
        }

        public static void BuildCharacterPanel()
        {
            string path = $"{PrefabDir}/CharacterPanel.prefab";
            var sheet = AssetDatabase.LoadAllAssetsAtPath(ButtonsSheet).OfType<Sprite>().ToDictionary(s => s.name);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
            var knightSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Characters/FantasyKnights/01Knight.png");

            var roleWarrior = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/role_warrior.png");
            var roleRanger = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/role_ranger.png");
            var roleMage = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/role_mage.png");
            var roleAssassin = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/role_assassin.png");

            var barRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/bar_red.png");
            var barGrey = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/bar_grey.png");
            var barBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/bar_blue.png");
            var barGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/UI/bar_green.png");

            Sprite darkFrame = sheet["Basic Buttons 3 Fv_141"];
            Sprite slotFrame = sheet["Basic Buttons 3 Fv_109"];
            Sprite btnFrame = sheet["Basic Buttons 3 Fv_124"];
            Sprite lightFrame = sheet["Basic Buttons 3 Fv_75"];

            var root = CreateRoot("CharacterPanel", typeof(Lobby.CharacterPanel));
            var panelComp = root.GetComponent<Lobby.CharacterPanel>();

            // Set UIScreen layer to Panel
            var soScreen = new SerializedObject(panelComp);
            soScreen.FindProperty("layer").enumValueIndex = (int)UILayer.Panel;
            soScreen.FindProperty("closeOnBack").boolValue = true;
            soScreen.ApplyModifiedPropertiesWithoutUndo();

            // 1. CloseButton (Top-Left)
            var closeBtnGo = AddButton(root.transform, "CloseButton", darkFrame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(52f, 52f), new Vector2(20f, -14f));
            closeBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
            if (arrowSprite != null)
            {
                var arrowImg = AddImage(closeBtnGo.transform, "ArrowIcon", arrowSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f), Vector2.zero);
                arrowImg.rectTransform.localEulerAngles = new Vector3(0, 0, 180f);
            }

            // 2. ContentWindow (470 x 620, initial dock right)
            var winGo = CreateUIObject("ContentWindow", root.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(470f, 620f), new Vector2(-24f, -30f));
            var winBg = winGo.AddComponent<Image>();
            winBg.sprite = darkFrame;
            winBg.type = Image.Type.Sliced;
            var winRt = winGo.GetComponent<RectTransform>();

            // Header (438 x 104)
            var headerGo = CreateUIObject("Header", winGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(438f, 104f), new Vector2(16f, -16f));
            var avatarFrame = AddImage(headerGo.transform, "AvatarFrame", slotFrame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 104f), new Vector2(0f, 0f));
            avatarFrame.type = Image.Type.Sliced;
            var avatarImg = AddImage(avatarFrame.transform, "AvatarImage", knightSprite, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(64f, 64f), Vector2.zero);
            avatarImg.preserveAspect = true;

            var nameText = AddText(headerGo.transform, "NameText", "Knight", font, 32f, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 36f), new Vector2(118f, 0f), TextAlignmentOptions.MidlineLeft);
            var roleRow = CreateUIObject("RoleRow", headerGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 24f), new Vector2(118f, -40f));
            var roleIconImg = AddImage(roleRow.transform, "RoleIcon", roleWarrior, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 18f), Vector2.zero);
            var roleText = AddText(roleRow.transform, "RoleText", "WARRIOR", font, 14f, new Color(0.9f, 0.9f, 0.9f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 20f), new Vector2(24f, 0f), TextAlignmentOptions.MidlineLeft);
            var skillText = AddText(headerGo.transform, "SkillNameText", "Dual Wield · 12s cooldown", font, 14f, new Color(1f, 0.82f, 0.48f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 24f), new Vector2(118f, -68f), TextAlignmentOptions.MidlineLeft);

            // Description
            var descText = AddText(winGo.transform, "DescriptionText", "Melee, heavy armor, slashes cut through basic bullets.", font, 13f, new Color(0.9f, 0.91f, 0.95f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(438f, 48f), new Vector2(16f, -128f), TextAlignmentOptions.TopLeft);

            // Stats Grid (4 bars)
            var statsGrid = CreateUIObject("StatsGrid", winGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(438f, 64f), new Vector2(16f, -184f));
            CreateStatBar(statsGrid.transform, "HpBar", "HP", font, barRed, new Vector2(0f, 0f), out var hpText, out var hpFill);
            CreateStatBar(statsGrid.transform, "ArmorBar", "Armor", font, barGrey, new Vector2(220f, 0f), out var armorText, out var armorFill);
            CreateStatBar(statsGrid.transform, "ManaBar", "Mana", font, barBlue, new Vector2(0f, -32f), out var manaText, out var manaFill);
            CreateStatBar(statsGrid.transform, "DamageBar", "Damage", font, barGreen, new Vector2(220f, -32f), out var dmgText, out var dmgFill);

            // Equipment Row
            var eqRow = CreateUIObject("EquipmentRow", winGo.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(438f, 54f), new Vector2(16f, -256f));
            for (int i = 0; i < 6; i++)
            {
                var slot = AddImage(eqRow.transform, $"EqSlot_{i}", slotFrame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 48f), new Vector2(i * 50f, 0f));
                slot.type = Image.Type.Sliced;
            }
            for (int i = 0; i < 2; i++)
            {
                var slot = AddImage(eqRow.transform, $"WpSlot_{i}", btnFrame, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(48f, 48f), new Vector2(310f + i * 50f, 0f));
                slot.type = Image.Type.Sliced;
            }

            // Active Skill Summary
            var activeText = AddText(winGo.transform, "ActiveSkillsSummaryText", "Active <color=#9FD0FF>1/2</color> · Frost Field", font, 13f, new Color(0.9f, 0.91f, 0.95f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(438f, 30f), new Vector2(16f, -320f), TextAlignmentOptions.MidlineLeft);

            // Playable Group
            var playGroup = CreateUIObject("PlayableGroup", winGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(438f, 165f), new Vector2(0f, 16f));
            var equipBtnGo = AddButton(playGroup.transform, "EquipButton", btnFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 76f), new Vector2(-110f, 36f));
            equipBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
            AddText(equipBtnGo.transform, "Text", "TRANG BỊ", font, 20f, new Color(0.11f, 0.08f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, 40f), Vector2.zero);

            var weaponBtnGo = AddButton(playGroup.transform, "WeaponButton", btnFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 76f), new Vector2(110f, 36f));
            weaponBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
            AddText(weaponBtnGo.transform, "Text", "VŨ KHÍ", font, 20f, new Color(0.11f, 0.08f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180f, 40f), Vector2.zero);

            var portalBtnGo = AddButton(playGroup.transform, "EnterPortalButton", lightFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, 76f), new Vector2(0f, -44f));
            portalBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
            AddText(portalBtnGo.transform, "Text", "CHỌN · VÀO PORTAL", font, 24f, new Color(0.11f, 0.08f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 40f), Vector2.zero);

            // Locked Group
            var lockGroup = CreateUIObject("LockedGroup", winGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(438f, 165f), new Vector2(0f, 16f));
            AddText(lockGroup.transform, "LockedNote", "Chưa mở khóa. Mở bằng coin mang về từ các run.", font, 13f, new Color(0.9f, 0.91f, 0.95f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 40f), new Vector2(0f, 36f));
            var unlockBtnGo = AddButton(lockGroup.transform, "UnlockButton", lightFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(430f, 76f), new Vector2(0f, -44f));
            unlockBtnGo.GetComponent<Image>().type = Image.Type.Sliced;
            var unlockText = AddText(unlockBtnGo.transform, "Text", "MỞ KHÓA ·", font, 24f, new Color(0.11f, 0.08f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(160f, 40f), new Vector2(-40f, 0f));
            var priceText = AddText(unlockBtnGo.transform, "PriceText", "800", font, 24f, new Color(0.11f, 0.08f, 0.13f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100f, 40f), new Vector2(50f, 0f));
            lockGroup.SetActive(false);

            // Wire Serialized Properties
            var so = new SerializedObject(panelComp);
            so.FindProperty("contentWindow").objectReferenceValue = winRt;
            so.FindProperty("closeButton").objectReferenceValue = closeBtnGo.GetComponent<Button>();
            so.FindProperty("avatarImage").objectReferenceValue = avatarImg;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("roleText").objectReferenceValue = roleText;
            so.FindProperty("roleIcon").objectReferenceValue = roleIconImg;
            so.FindProperty("skillNameText").objectReferenceValue = skillText;
            so.FindProperty("descriptionText").objectReferenceValue = descText;
            so.FindProperty("hpText").objectReferenceValue = hpText;
            so.FindProperty("hpFill").objectReferenceValue = hpFill;
            so.FindProperty("armorText").objectReferenceValue = armorText;
            so.FindProperty("armorFill").objectReferenceValue = armorFill;
            so.FindProperty("manaText").objectReferenceValue = manaText;
            so.FindProperty("manaFill").objectReferenceValue = manaFill;
            so.FindProperty("damageText").objectReferenceValue = dmgText;
            so.FindProperty("damageFill").objectReferenceValue = dmgFill;
            so.FindProperty("activeSkillsSummaryText").objectReferenceValue = activeText;
            so.FindProperty("playableGroup").objectReferenceValue = playGroup;
            so.FindProperty("equipButton").objectReferenceValue = equipBtnGo.GetComponent<Button>();
            so.FindProperty("weaponsButton").objectReferenceValue = weaponBtnGo.GetComponent<Button>();
            so.FindProperty("enterPortalButton").objectReferenceValue = portalBtnGo.GetComponent<Button>();
            so.FindProperty("lockedGroup").objectReferenceValue = lockGroup;
            so.FindProperty("unlockButton").objectReferenceValue = unlockBtnGo.GetComponent<Button>();
            so.FindProperty("unlockPriceText").objectReferenceValue = priceText;
            so.FindProperty("warriorIcon").objectReferenceValue = roleWarrior;
            so.FindProperty("rangerIcon").objectReferenceValue = roleRanger;
            so.FindProperty("mageIcon").objectReferenceValue = roleMage;
            so.FindProperty("assassinIcon").objectReferenceValue = roleAssassin;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[UIPrefabBuilder] Saved {path}");
        }

        static void CreateStatBar(Transform parent, string name, string label, TMP_FontAsset font, Sprite barSprite, Vector2 pos, out TMP_Text valText, out Image fillImg)
        {
            var go = CreateUIObject(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, 28f), pos);
            AddText(go.transform, "Label", label, font, 12f, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, 20f), Vector2.zero, TextAlignmentOptions.MidlineLeft);
            var barBg = CreateUIObject("BarBg", go.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(130f, 20f), new Vector2(70f, 0f));
            var fill = barBg.AddComponent<Image>();
            fill.sprite = barSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.8f;
            fillImg = fill;
            valText = AddText(barBg.transform, "ValText", "80", font, 13f, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 20f), Vector2.zero);
        }

        public static void RegisterAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            var group = settings.DefaultGroup;
            Register(settings, group, $"{PrefabDir}/MenuScreen.prefab", AddressKeys.UiScreenMenu);
            Register(settings, group, $"{PrefabDir}/LobbyHud.prefab", AddressKeys.UiHudLobby);
            Register(settings, group, $"{PrefabDir}/CharacterPanel.prefab", AddressKeys.UiPanelCharacter);
            Register(settings, group, $"{PrefabDir}/ItemTile.prefab", AddressKeys.UiSharedItemTile);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[UIPrefabBuilder] Addressables registered successfully.");
        }

        static void Register(AddressableAssetSettings settings, AddressableAssetGroup group, string path, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            if (entry != null)
            {
                entry.address = address;
            }
        }

        static GameObject CreateRoot(string name, System.Type screenType)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), screenType);
            Stretch(go.GetComponent<RectTransform>());
            return go;
        }

        static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return go;
        }

        static Image AddImage(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            var go = CreateUIObject(name, parent, anchorMin, anchorMax, pivot, size, pos);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        static GameObject AddButton(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            var go = CreateUIObject(name, parent, anchorMin, anchorMax, pivot, size, pos);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = true;
            go.AddComponent<Button>();
            return go;
        }

        static TextMeshProUGUI AddText(Transform parent, string name, string text, TMP_FontAsset font, float fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = CreateUIObject(name, parent, anchorMin, anchorMax, pivot, size, pos);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
