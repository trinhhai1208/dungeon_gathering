using System.IO;
using System.Linq;
using DungeonGathering.Core;
using DungeonGathering.Localization;
using DungeonGathering.Play;
using DungeonGathering.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Builds and updates Play HUD prefabs (PlayHud.prefab, HUD.prefab, VirtualJoystick.prefab)
    /// compliant with UIRoot architecture (UI_SYSTEM §3, §6 and UI-HUD.dc.html).
    /// </summary>
    public static class PlayHudPrefabBuilder
    {
        const string PlayHudPrefabPath = "Assets/Project/Prefabs/UI/PlayHud.prefab";
        const string HudPrefabPath = "Assets/Project/Prefabs/UI/HUD.prefab";
        const string JoystickPrefabPath = "Assets/Project/Prefabs/UI/VirtualJoystick.prefab";
        const string ItemTilePrefabPath = "Assets/Project/Prefabs/UI/ItemTile.prefab";

        const string ButtonsSheetPath = "Assets/Project/Art/Sprites/UI/HUD/Basic Buttons 3 Fv.png";
        const string FontPath = "Assets/Project/Art/Fonts/Pixelta SDF.asset";
        const string JoyBasePath = "Assets/Project/Art/Sprites/UI/joy_base.png";
        const string JoyKnobPath = "Assets/Project/Art/Sprites/UI/joy_knob.png";

        [MenuItem("Tools/Dungeon Gathering/Build Play HUD Prefabs")]
        public static void BuildAll()
        {
            BuildVirtualJoystickPrefab();
            BuildHudPrefab(PlayHudPrefabPath, "PlayHud");
            RegisterAddressables();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayHudPrefabBuilder] Successfully built Play HUD prefabs.");
        }

        public static void BuildVirtualJoystickPrefab()
        {
            Directory.CreateDirectory("Assets/Project/Prefabs/UI");
            var joyBase = AssetDatabase.LoadAssetAtPath<Sprite>(JoyBasePath);
            var joyKnob = AssetDatabase.LoadAssetAtPath<Sprite>(JoyKnobPath);

            var root = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(160f, 160f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            var img = root.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // Transparent touch detection area
            img.raycastTarget = true;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(root.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.sizeDelta = new Vector2(160f, 160f);
            bgRt.anchorMin = bgRt.anchorMax = bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = joyBase;
            bgImg.color = new Color(1f, 1f, 1f, 0.9f);
            bgImg.raycastTarget = false;

            var knobGo = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            knobGo.transform.SetParent(bgGo.transform, false);
            var knobRt = (RectTransform)knobGo.transform;
            knobRt.sizeDelta = new Vector2(80f, 80f);
            knobRt.anchorMin = knobRt.anchorMax = knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.anchoredPosition = Vector2.zero;
            var knobImg = knobGo.GetComponent<Image>();
            knobImg.sprite = joyKnob;
            knobImg.color = Color.white;
            knobImg.raycastTarget = false;

            var uiJoy = root.AddComponent<UI.VirtualJoystick>();
            var soUi = new SerializedObject(uiJoy);
            soUi.FindProperty("background").objectReferenceValue = bgRt;
            soUi.FindProperty("knob").objectReferenceValue = knobRt;
            soUi.FindProperty("radius").floatValue = 64f;
            soUi.FindProperty("floating").boolValue = true;
            soUi.FindProperty("deadZone").floatValue = 0.12f;
            soUi.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, JoystickPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[PlayHudPrefabBuilder] Saved {JoystickPrefabPath}");
        }

        static void BuildHudPrefab(string targetPath, string rootName)
        {
            Directory.CreateDirectory("Assets/Project/Prefabs/UI");
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var joyBase = AssetDatabase.LoadAssetAtPath<Sprite>(JoyBasePath);
            var joyKnob = AssetDatabase.LoadAssetAtPath<Sprite>(JoyKnobPath);
            var itemTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemTilePrefabPath);

            var sheet = AssetDatabase.LoadAllAssetsAtPath(ButtonsSheetPath).OfType<Sprite>().ToDictionary(s => s.name);
            sheet.TryGetValue("Basic Buttons 3 Fv_5", out var frameSprite);
            sheet.TryGetValue("Basic Buttons 3 Fv_47", out var buttonSquareSprite);
            sheet.TryGetValue("Basic Buttons 3 Fv_131", out var pauseSprite);
            sheet.TryGetValue("Basic Buttons 3 Fv_109", out var slotSprite);
            sheet.TryGetValue("Basic Buttons 3 Fv_116", out var swordSprite);
            sheet.TryGetValue("Basic Buttons 3 Fv_9", out var arrowSprite);

            var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Items/Potions/Potion 2.png");
            var manaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Items/Potions/Potion 4.png");
            var coinSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Project/Art/Sprites/Items/Coins/coin_1.png");

            var root = new GameObject(rootName, typeof(RectTransform), typeof(CanvasGroup));
            var rtRoot = (RectTransform)root.transform;
            rtRoot.anchorMin = Vector2.zero;
            rtRoot.anchorMax = Vector2.one;
            rtRoot.pivot = new Vector2(0.5f, 0.5f);
            rtRoot.sizeDelta = Vector2.zero;
            rtRoot.anchoredPosition = Vector2.zero;
            rtRoot.localScale = Vector3.one;

            // 1. PlayerStatusPanel (top-left 10, -10, size 252x96)
            var statusPanel = new GameObject("PlayerStatusPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            statusPanel.transform.SetParent(root.transform, false);
            var rtStatus = (RectTransform)statusPanel.transform;
            rtStatus.anchorMin = rtStatus.anchorMax = rtStatus.pivot = new Vector2(0f, 1f);
            rtStatus.anchoredPosition = new Vector2(10f, -10f);
            rtStatus.sizeDelta = new Vector2(252f, 96f);
            var imgStatus = statusPanel.GetComponent<Image>();
            imgStatus.sprite = frameSprite;
            imgStatus.type = Image.Type.Sliced;
            imgStatus.color = new Color(0.1f, 0.08f, 0.13f, 0.9f);

            // Avatar Frame (pos 4, 0, size 60x60)
            var avatarFrame = new GameObject("AvatarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            avatarFrame.transform.SetParent(statusPanel.transform, false);
            var rtAvatar = (RectTransform)avatarFrame.transform;
            rtAvatar.anchorMin = rtAvatar.anchorMax = rtAvatar.pivot = new Vector2(0f, 0.5f);
            rtAvatar.anchoredPosition = new Vector2(4f, 0f);
            rtAvatar.sizeDelta = new Vector2(60f, 60f);
            var imgAvatarFrame = avatarFrame.GetComponent<Image>();
            imgAvatarFrame.sprite = slotSprite;
            imgAvatarFrame.type = Image.Type.Sliced;

            var avatarImgGo = new GameObject("AvatarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            avatarImgGo.transform.SetParent(avatarFrame.transform, false);
            var rtAvatarImg = (RectTransform)avatarImgGo.transform;
            rtAvatarImg.anchorMin = rtAvatarImg.anchorMax = rtAvatarImg.pivot = new Vector2(0.5f, 0.5f);
            rtAvatarImg.sizeDelta = new Vector2(44f, 44f);
            var avatarImg = avatarImgGo.GetComponent<Image>();
            avatarImg.preserveAspect = true;

            // Bars Column (pos 68, 0, size 176x84)
            var barsColumn = new GameObject("BarsColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            barsColumn.transform.SetParent(statusPanel.transform, false);
            var rtBars = (RectTransform)barsColumn.transform;
            rtBars.anchorMin = rtBars.anchorMax = rtBars.pivot = new Vector2(0f, 0.5f);
            rtBars.anchoredPosition = new Vector2(68f, 0f);
            rtBars.sizeDelta = new Vector2(176f, 84f);
            var vlg = barsColumn.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var hp = CreateBar(barsColumn.transform, "HpBar", heartSprite, new Color(0.9f, 0.2f, 0.2f, 1f), "100/100", fontAsset);
            var armor = CreateBar(barsColumn.transform, "ArmorBar", slotSprite, new Color(0.6f, 0.65f, 0.75f, 1f), "50/50", fontAsset);
            var mana = CreateBar(barsColumn.transform, "ManaBar", manaSprite, new Color(0.2f, 0.5f, 0.9f, 1f), "100/100", fontAsset);

            // 2. RunStatsCluster (top-left 10, -112, size 180x34)
            var runStats = new GameObject("RunStatsCluster", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            runStats.transform.SetParent(root.transform, false);
            var rtRunStats = (RectTransform)runStats.transform;
            rtRunStats.anchorMin = rtRunStats.anchorMax = rtRunStats.pivot = new Vector2(0f, 1f);
            rtRunStats.anchoredPosition = new Vector2(10f, -112f);
            rtRunStats.sizeDelta = new Vector2(180f, 34f);
            var hlgRun = runStats.GetComponent<HorizontalLayoutGroup>();
            hlgRun.spacing = 8f;
            hlgRun.childControlWidth = false;
            hlgRun.childControlHeight = false;

            // Coin Group
            var coinGroup = new GameObject("CoinGroup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
            coinGroup.transform.SetParent(runStats.transform, false);
            var rtCoinG = (RectTransform)coinGroup.transform;
            rtCoinG.sizeDelta = new Vector2(80f, 34f);
            var imgCoinG = coinGroup.GetComponent<Image>();
            imgCoinG.sprite = frameSprite;
            imgCoinG.type = Image.Type.Sliced;
            imgCoinG.color = new Color(0.1f, 0.08f, 0.13f, 0.9f);
            var hlgCoin = coinGroup.GetComponent<HorizontalLayoutGroup>();
            hlgCoin.padding = new RectOffset(6, 6, 4, 4);
            hlgCoin.spacing = 4f;
            hlgCoin.childAlignment = TextAnchor.MiddleLeft;

            var coinIconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coinIconGo.transform.SetParent(coinGroup.transform, false);
            var rtCoinIcon = (RectTransform)coinIconGo.transform;
            rtCoinIcon.sizeDelta = new Vector2(16f, 16f);
            var imgCoinIcon = coinIconGo.GetComponent<Image>();
            imgCoinIcon.sprite = coinSprite;
            imgCoinIcon.preserveAspect = true;

            var coinValGo = new GameObject("CoinText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            coinValGo.transform.SetParent(coinGroup.transform, false);
            var rtCoinVal = (RectTransform)coinValGo.transform;
            rtCoinVal.sizeDelta = new Vector2(48f, 26f);
            var coinText = coinValGo.GetComponent<TextMeshProUGUI>();
            coinText.font = fontAsset;
            coinText.fontSize = 16f;
            coinText.alignment = TextAlignmentOptions.MidlineLeft;
            coinText.color = new Color(0.95f, 0.75f, 0.25f, 1f);
            coinText.text = "0";

            // Floor Group
            var floorGroup = new GameObject("FloorGroup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
            floorGroup.transform.SetParent(runStats.transform, false);
            var rtFloorG = (RectTransform)floorGroup.transform;
            rtFloorG.sizeDelta = new Vector2(90f, 34f);
            var imgFloorG = floorGroup.GetComponent<Image>();
            imgFloorG.sprite = frameSprite;
            imgFloorG.type = Image.Type.Sliced;
            imgFloorG.color = new Color(0.1f, 0.08f, 0.13f, 0.9f);
            var hlgFloor = floorGroup.GetComponent<HorizontalLayoutGroup>();
            hlgFloor.padding = new RectOffset(6, 6, 4, 4);
            hlgFloor.childAlignment = TextAnchor.MiddleCenter;

            var floorValGo = new GameObject("FloorText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizedTMPText));
            floorValGo.transform.SetParent(floorGroup.transform, false);
            var rtFloorVal = (RectTransform)floorValGo.transform;
            rtFloorVal.sizeDelta = new Vector2(78f, 26f);
            var floorText = floorValGo.GetComponent<TextMeshProUGUI>();
            floorText.font = fontAsset;
            floorText.fontSize = 14f;
            floorText.alignment = TextAlignmentOptions.Center;
            floorText.color = Color.white;
            floorText.text = "FLOOR 1-1";
            var floorLocalized = floorValGo.GetComponent<LocalizedTMPText>();
            floorLocalized.SetKey("hud.floor");

            // 3. TopRightCluster (Minimap + PauseButton) (top-right -10, -10, size 244x130)
            var topRight = new GameObject("TopRightCluster", typeof(RectTransform));
            topRight.transform.SetParent(root.transform, false);
            var rtTopRight = (RectTransform)topRight.transform;
            rtTopRight.anchorMin = rtTopRight.anchorMax = rtTopRight.pivot = new Vector2(1f, 1f);
            rtTopRight.anchoredPosition = new Vector2(-10f, -10f);
            rtTopRight.sizeDelta = new Vector2(244f, 130f);

            var minimap = new GameObject("Minimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            minimap.transform.SetParent(topRight.transform, false);
            var rtMini = (RectTransform)minimap.transform;
            rtMini.anchorMin = rtMini.anchorMax = rtMini.pivot = new Vector2(0f, 1f);
            rtMini.anchoredPosition = Vector2.zero;
            rtMini.sizeDelta = new Vector2(176f, 130f);
            var imgMini = minimap.GetComponent<Image>();
            imgMini.sprite = frameSprite;
            imgMini.type = Image.Type.Sliced;
            imgMini.color = new Color(0.1f, 0.08f, 0.13f, 0.9f);

            var mapContent = new GameObject("MapContent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            mapContent.transform.SetParent(minimap.transform, false);
            var rtMapContent = (RectTransform)mapContent.transform;
            rtMapContent.anchorMin = Vector2.zero;
            rtMapContent.anchorMax = Vector2.one;
            rtMapContent.offsetMin = new Vector2(8f, 8f);
            rtMapContent.offsetMax = new Vector2(-8f, -8f);
            var imgMapContent = mapContent.GetComponent<Image>();
            imgMapContent.color = new Color(0.06f, 0.05f, 0.08f, 0.8f);

            var pauseBtnGo = new GameObject("PauseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualButton));
            pauseBtnGo.transform.SetParent(topRight.transform, false);
            var rtPause = (RectTransform)pauseBtnGo.transform;
            rtPause.anchorMin = rtPause.anchorMax = rtPause.pivot = new Vector2(1f, 1f);
            rtPause.anchoredPosition = Vector2.zero;
            rtPause.sizeDelta = new Vector2(60f, 60f);
            var imgPause = pauseBtnGo.GetComponent<Image>();
            imgPause.sprite = pauseSprite != null ? pauseSprite : frameSprite;
            imgPause.preserveAspect = true;
            var pauseBtn = pauseBtnGo.GetComponent<Button>();
            var vbPause = pauseBtnGo.GetComponent<VirtualButton>();
            var soVbPause = new SerializedObject(vbPause);
            soVbPause.FindProperty("action").enumValueIndex = (int)VirtualAction.Pause;
            soVbPause.ApplyModifiedPropertiesWithoutUndo();

            // 4. BossHealthBar (top-center 0, -16, size 480x40)
            var bossBarGo = new GameObject("BossHealthBar", typeof(RectTransform));
            bossBarGo.transform.SetParent(root.transform, false);
            var rtBoss = (RectTransform)bossBarGo.transform;
            rtBoss.anchorMin = rtBoss.anchorMax = rtBoss.pivot = new Vector2(0.5f, 1f);
            rtBoss.anchoredPosition = new Vector2(0f, -16f);
            rtBoss.sizeDelta = new Vector2(480f, 40f);

            var bossNameGo = new GameObject("BossName", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizedTMPText));
            bossNameGo.transform.SetParent(bossBarGo.transform, false);
            var rtBossName = (RectTransform)bossNameGo.transform;
            rtBossName.anchorMin = new Vector2(0f, 1f);
            rtBossName.anchorMax = new Vector2(1f, 1f);
            rtBossName.pivot = new Vector2(0.5f, 1f);
            rtBossName.anchoredPosition = Vector2.zero;
            rtBossName.sizeDelta = new Vector2(0f, 20f);
            var tmpBossName = bossNameGo.GetComponent<TextMeshProUGUI>();
            tmpBossName.font = fontAsset;
            tmpBossName.fontSize = 16f;
            tmpBossName.alignment = TextAlignmentOptions.Center;
            tmpBossName.color = new Color(1f, 0.85f, 0.4f, 1f);
            tmpBossName.text = "BOSS";
            var locBossName = bossNameGo.GetComponent<LocalizedTMPText>();

            var bossTrackGo = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bossTrackGo.transform.SetParent(bossBarGo.transform, false);
            var rtBossTrack = (RectTransform)bossTrackGo.transform;
            rtBossTrack.anchorMin = rtBossTrack.anchorMax = new Vector2(0f, 0f);
            rtBossTrack.pivot = new Vector2(0.5f, 0f);
            rtBossTrack.anchoredPosition = Vector2.zero;
            rtBossTrack.sizeDelta = new Vector2(480f, 18f);
            var imgBossTrack = bossTrackGo.GetComponent<Image>();
            imgBossTrack.color = new Color(0.1f, 0.08f, 0.12f, 1f);

            var bossFillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bossFillGo.transform.SetParent(bossTrackGo.transform, false);
            var rtBossFill = (RectTransform)bossFillGo.transform;
            rtBossFill.anchorMin = Vector2.zero;
            rtBossFill.anchorMax = Vector2.one;
            rtBossFill.pivot = new Vector2(0f, 0.5f);
            rtBossFill.offsetMin = rtBossFill.offsetMax = Vector2.zero;
            var imgBossFill = bossFillGo.GetComponent<Image>();
            imgBossFill.type = Image.Type.Filled;
            imgBossFill.fillMethod = Image.FillMethod.Horizontal;
            imgBossFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            imgBossFill.color = new Color(0.85f, 0.15f, 0.15f, 1f);
            imgBossFill.fillAmount = 1f;

            bossBarGo.SetActive(false);

            // 5. VirtualJoystick (bottom-left 48, 40, size 160x160)
            var joystickGo = new GameObject("VirtualJoystick", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UI.VirtualJoystick));
            joystickGo.transform.SetParent(root.transform, false);
            var rtJoy = (RectTransform)joystickGo.transform;
            rtJoy.anchorMin = rtJoy.anchorMax = rtJoy.pivot = new Vector2(0f, 0f);
            rtJoy.anchoredPosition = new Vector2(48f, 40f);
            rtJoy.sizeDelta = new Vector2(160f, 160f);
            var imgJoy = joystickGo.GetComponent<Image>();
            imgJoy.color = new Color(0, 0, 0, 0);
            imgJoy.raycastTarget = true;

            var joyBgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            joyBgGo.transform.SetParent(joystickGo.transform, false);
            var rtJoyBg = (RectTransform)joyBgGo.transform;
            rtJoyBg.anchorMin = rtJoyBg.anchorMax = rtJoyBg.pivot = new Vector2(0.5f, 0.5f);
            rtJoyBg.sizeDelta = new Vector2(160f, 160f);
            rtJoyBg.anchoredPosition = Vector2.zero;
            var imgJoyBg = joyBgGo.GetComponent<Image>();
            imgJoyBg.sprite = joyBase;
            imgJoyBg.color = new Color(1f, 1f, 1f, 0.9f);
            imgJoyBg.raycastTarget = false;

            var joyKnobGo = new GameObject("Knob", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            joyKnobGo.transform.SetParent(joyBgGo.transform, false);
            var rtJoyKnob = (RectTransform)joyKnobGo.transform;
            rtJoyKnob.anchorMin = rtJoyKnob.anchorMax = rtJoyKnob.pivot = new Vector2(0.5f, 0.5f);
            rtJoyKnob.sizeDelta = new Vector2(80f, 80f);
            rtJoyKnob.anchoredPosition = Vector2.zero;
            var imgJoyKnob = joyKnobGo.GetComponent<Image>();
            imgJoyKnob.sprite = joyKnob;
            imgJoyKnob.color = Color.white;
            imgJoyKnob.raycastTarget = false;

            var uiJoyComp = joystickGo.GetComponent<UI.VirtualJoystick>();
            var soUj = new SerializedObject(uiJoyComp);
            soUj.FindProperty("background").objectReferenceValue = rtJoyBg;
            soUj.FindProperty("knob").objectReferenceValue = rtJoyKnob;
            soUj.FindProperty("radius").floatValue = 64f;
            soUj.FindProperty("floating").boolValue = true;
            soUj.FindProperty("deadZone").floatValue = 0.12f;
            soUj.ApplyModifiedPropertiesWithoutUndo();

            // 6. ActionCluster (bottom-right -28, 28, size 330x250)
            var actionCluster = new GameObject("ActionCluster", typeof(RectTransform));
            actionCluster.transform.SetParent(root.transform, false);
            var rtAction = (RectTransform)actionCluster.transform;
            rtAction.anchorMin = rtAction.anchorMax = rtAction.pivot = new Vector2(1f, 0f);
            rtAction.anchoredPosition = new Vector2(-28f, 28f);
            rtAction.sizeDelta = new Vector2(330f, 250f);

            // AttackButton (pos 0, 0, size 132x132)
            var attackBtnGo = new GameObject("AttackButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualButton));
            attackBtnGo.transform.SetParent(actionCluster.transform, false);
            var rtAtk = (RectTransform)attackBtnGo.transform;
            rtAtk.anchorMin = rtAtk.anchorMax = rtAtk.pivot = new Vector2(1f, 0f);
            rtAtk.anchoredPosition = Vector2.zero;
            rtAtk.sizeDelta = new Vector2(132f, 132f);
            var imgAtk = attackBtnGo.GetComponent<Image>();
            imgAtk.sprite = buttonSquareSprite != null ? buttonSquareSprite : frameSprite;
            imgAtk.type = Image.Type.Sliced;
            imgAtk.color = new Color(0.85f, 0.25f, 0.25f, 1f);
            var atkBtn = attackBtnGo.GetComponent<Button>();
            var vbAtk = attackBtnGo.GetComponent<VirtualButton>();
            var soVbAtk = new SerializedObject(vbAtk);
            soVbAtk.FindProperty("action").enumValueIndex = (int)VirtualAction.Attack;
            soVbAtk.ApplyModifiedPropertiesWithoutUndo();

            var atkIconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            atkIconGo.transform.SetParent(attackBtnGo.transform, false);
            var rtAtkIcon = (RectTransform)atkIconGo.transform;
            rtAtkIcon.anchorMin = rtAtkIcon.anchorMax = rtAtkIcon.pivot = new Vector2(0.5f, 0.5f);
            rtAtkIcon.sizeDelta = new Vector2(64f, 64f);
            var imgAtkIcon = atkIconGo.GetComponent<Image>();
            imgAtkIcon.sprite = swordSprite;
            imgAtkIcon.preserveAspect = true;
            imgAtkIcon.raycastTarget = false;

            // SwapWeaponSlot (pos -138, 112, size 72x75)
            var swapGo = new GameObject("SwapWeaponSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(VirtualButton));
            swapGo.transform.SetParent(actionCluster.transform, false);
            var rtSwap = (RectTransform)swapGo.transform;
            rtSwap.anchorMin = rtSwap.anchorMax = rtSwap.pivot = new Vector2(1f, 0f);
            rtSwap.anchoredPosition = new Vector2(-138f, 112f);
            rtSwap.sizeDelta = new Vector2(72f, 75f);
            var imgSwap = swapGo.GetComponent<Image>();
            imgSwap.sprite = slotSprite;
            imgSwap.type = Image.Type.Sliced;
            var swapBtn = swapGo.GetComponent<Button>();
            var vbSwap = swapGo.GetComponent<VirtualButton>();
            var soVbSwap = new SerializedObject(vbSwap);
            soVbSwap.FindProperty("action").enumValueIndex = (int)VirtualAction.SwapWeapon;
            soVbSwap.ApplyModifiedPropertiesWithoutUndo();

            var weaponIconGo = new GameObject("WeaponIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            weaponIconGo.transform.SetParent(swapGo.transform, false);
            var rtWeapIcon = (RectTransform)weaponIconGo.transform;
            rtWeapIcon.anchorMin = rtWeapIcon.anchorMax = rtWeapIcon.pivot = new Vector2(0.5f, 0.5f);
            rtWeapIcon.sizeDelta = new Vector2(52f, 52f);
            var imgWeapIcon = weaponIconGo.GetComponent<Image>();
            imgWeapIcon.sprite = swordSprite;
            imgWeapIcon.preserveAspect = true;
            imgWeapIcon.raycastTarget = false;

            var swapArrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            swapArrowGo.transform.SetParent(swapGo.transform, false);
            var rtArrow = (RectTransform)swapArrowGo.transform;
            rtArrow.anchorMin = rtArrow.anchorMax = rtArrow.pivot = new Vector2(0f, 1f);
            rtArrow.anchoredPosition = new Vector2(-6f, 6f);
            rtArrow.sizeDelta = new Vector2(24f, 24f);
            var imgArrow = swapArrowGo.GetComponent<Image>();
            imgArrow.sprite = arrowSprite;
            imgArrow.preserveAspect = true;
            imgArrow.raycastTarget = false;

            // ActiveSkill1 (pos -142, 0, size 104x108)
            GameObject skill1Go;
            if (itemTilePrefab != null)
            {
                skill1Go = (GameObject)PrefabUtility.InstantiatePrefab(itemTilePrefab);
                skill1Go.name = "ActiveSkill1";
                skill1Go.transform.SetParent(actionCluster.transform, false);
            }
            else
            {
                skill1Go = new GameObject("ActiveSkill1", typeof(RectTransform), typeof(ItemTile));
                skill1Go.transform.SetParent(actionCluster.transform, false);
            }
            var rtSkill1 = (RectTransform)skill1Go.transform;
            rtSkill1.anchorMin = rtSkill1.anchorMax = rtSkill1.pivot = new Vector2(1f, 0f);
            rtSkill1.anchoredPosition = new Vector2(-142f, 0f);
            rtSkill1.sizeDelta = new Vector2(104f, 108f);
            var vbSkill1 = skill1Go.AddComponent<VirtualButton>();
            var soVbS1 = new SerializedObject(vbSkill1);
            soVbS1.FindProperty("action").enumValueIndex = (int)VirtualAction.ItemSkill1;
            soVbS1.ApplyModifiedPropertiesWithoutUndo();

            // ActiveSkill2 (pos -26, 150, size 104x108)
            GameObject skill2Go;
            if (itemTilePrefab != null)
            {
                skill2Go = (GameObject)PrefabUtility.InstantiatePrefab(itemTilePrefab);
                skill2Go.name = "ActiveSkill2";
                skill2Go.transform.SetParent(actionCluster.transform, false);
            }
            else
            {
                skill2Go = new GameObject("ActiveSkill2", typeof(RectTransform), typeof(ItemTile));
                skill2Go.transform.SetParent(actionCluster.transform, false);
            }
            var rtSkill2 = (RectTransform)skill2Go.transform;
            rtSkill2.anchorMin = rtSkill2.anchorMax = rtSkill2.pivot = new Vector2(1f, 0f);
            rtSkill2.anchoredPosition = new Vector2(-26f, 150f);
            rtSkill2.sizeDelta = new Vector2(104f, 108f);
            var vbSkill2 = skill2Go.AddComponent<VirtualButton>();
            var soVbS2 = new SerializedObject(vbSkill2);
            soVbS2.FindProperty("action").enumValueIndex = (int)VirtualAction.ItemSkill2;
            soVbS2.ApplyModifiedPropertiesWithoutUndo();

            // 7. Toast (top-center 0, -80, size 320x36)
            var toastGo = new GameObject("Toast", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizedTMPText));
            toastGo.transform.SetParent(root.transform, false);
            var rtToast = (RectTransform)toastGo.transform;
            rtToast.anchorMin = rtToast.anchorMax = rtToast.pivot = new Vector2(0.5f, 1f);
            rtToast.anchoredPosition = new Vector2(0f, -80f);
            rtToast.sizeDelta = new Vector2(320f, 36f);
            var tmpToast = toastGo.GetComponent<TextMeshProUGUI>();
            if (fontAsset != null) tmpToast.font = fontAsset;
            tmpToast.fontSize = 14f;
            tmpToast.alignment = TextAlignmentOptions.Center;
            tmpToast.color = Color.white;
            var locToast = toastGo.GetComponent<LocalizedTMPText>();
            toastGo.SetActive(false);

            // Wire up PlayHud component
            var playHudComp = root.AddComponent<PlayHud>();
            var soPlayHud = new SerializedObject(playHudComp);
            soPlayHud.FindProperty("avatarImage").objectReferenceValue = avatarImg;
            soPlayHud.FindProperty("hpFill").objectReferenceValue = hp.fill;
            soPlayHud.FindProperty("armorFill").objectReferenceValue = armor.fill;
            soPlayHud.FindProperty("manaFill").objectReferenceValue = mana.fill;
            soPlayHud.FindProperty("hpText").objectReferenceValue = hp.text;
            soPlayHud.FindProperty("armorText").objectReferenceValue = armor.text;
            soPlayHud.FindProperty("manaText").objectReferenceValue = mana.text;
            soPlayHud.FindProperty("coinText").objectReferenceValue = coinText;
            soPlayHud.FindProperty("floorLabel").objectReferenceValue = floorLocalized;
            soPlayHud.FindProperty("minimapFrame").objectReferenceValue = rtMini;
            soPlayHud.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            soPlayHud.FindProperty("bossBar").objectReferenceValue = bossBarGo;
            soPlayHud.FindProperty("bossFill").objectReferenceValue = imgBossFill;
            soPlayHud.FindProperty("bossName").objectReferenceValue = locBossName;
            soPlayHud.FindProperty("attackButton").objectReferenceValue = atkBtn;
            soPlayHud.FindProperty("swapWeaponButton").objectReferenceValue = swapBtn;
            soPlayHud.FindProperty("currentWeaponIcon").objectReferenceValue = imgWeapIcon;
            soPlayHud.FindProperty("skillSlot1").objectReferenceValue = skill1Go.GetComponent<ItemTile>();
            soPlayHud.FindProperty("skillSlot2").objectReferenceValue = skill2Go.GetComponent<ItemTile>();
            soPlayHud.FindProperty("toast").objectReferenceValue = locToast;
            soPlayHud.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, targetPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[PlayHudPrefabBuilder] Saved {targetPath}");
        }

        static (Image fill, TMP_Text text) CreateBar(Transform parent, string barName, Sprite iconSprite, Color fillColor, string defaultText, TMP_FontAsset font)
        {
            var barGo = new GameObject(barName, typeof(RectTransform), typeof(LayoutElement));
            barGo.transform.SetParent(parent, false);
            var rtBar = (RectTransform)barGo.transform;
            rtBar.sizeDelta = new Vector2(170f, 24f);
            var le = barGo.GetComponent<LayoutElement>();
            le.preferredHeight = 24f;
            le.preferredWidth = 170f;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(barGo.transform, false);
            var rtIcon = (RectTransform)iconGo.transform;
            rtIcon.anchorMin = rtIcon.anchorMax = rtIcon.pivot = new Vector2(0f, 0.5f);
            rtIcon.anchoredPosition = Vector2.zero;
            rtIcon.sizeDelta = new Vector2(16f, 16f);
            var iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;

            var trackGo = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackGo.transform.SetParent(barGo.transform, false);
            var rtTrack = (RectTransform)trackGo.transform;
            rtTrack.anchorMin = Vector2.zero;
            rtTrack.anchorMax = Vector2.one;
            rtTrack.pivot = new Vector2(0.5f, 0.5f);
            rtTrack.offsetMin = new Vector2(20f, 0f);
            rtTrack.offsetMax = Vector2.zero;
            var trackImg = trackGo.GetComponent<Image>();
            trackImg.color = new Color(0.12f, 0.1f, 0.15f, 1f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(trackGo.transform, false);
            var rtFill = (RectTransform)fillGo.transform;
            rtFill.anchorMin = Vector2.zero;
            rtFill.anchorMax = Vector2.one;
            rtFill.pivot = new Vector2(0f, 0.5f);
            rtFill.offsetMin = rtFill.offsetMax = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = fillColor;
            fillImg.fillAmount = 1f;

            var textGo = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(trackGo.transform, false);
            var rtText = (RectTransform)textGo.transform;
            rtText.anchorMin = Vector2.zero;
            rtText.anchorMax = Vector2.one;
            rtText.offsetMin = rtText.offsetMax = Vector2.zero;
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 12f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = defaultText;

            return (fillImg, tmp);
        }

        static void RegisterAddressables()
        {
            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;
            var group = settings.DefaultGroup;
            var guid = AssetDatabase.AssetPathToGUID(PlayHudPrefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            if (entry != null) entry.address = AddressKeys.UiHudPlay;
        }
    }
}
