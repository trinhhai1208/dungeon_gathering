using System;
using DungeonGathering.Localization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DungeonGathering.Core
{
    /// <summary>
    /// Gốc của Bootstrap (Systems.prefab). Awake (order -1000) tạo + gán mọi service (R6),
    /// Start chạy chuỗi khởi động §7.4: Addressables → Save → Localization → Ready → core/shared-ui → Menu.
    /// Khi Play từ một scene module trong Editor, scene đó được "nhận nuôi" thay vì vào Menu.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class Systems : MonoBehaviour
    {
        public const string BootstrapSceneName = "Bootstrap";

        [SerializeField] UIRoot uiRoot;
        [SerializeField] LoadingOverlay loadingOverlay;
        [SerializeField] AudioManager audioManager;
        [SerializeField] ModuleRegistry moduleRegistry;
        [SerializeField] Transform poolRoot;
        [SerializeField] string firstSceneAddress = AddressKeys.SceneMenu;

        static Systems instance;
        public static Systems Instance => instance;

        UIService ui;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (uiRoot == null) uiRoot = GetComponentInChildren<UIRoot>(true).OrNull() ?? CreateFallbackUIRoot();
            if (loadingOverlay == null) loadingOverlay = GetComponentInChildren<LoadingOverlay>(true);
            if (audioManager == null) audioManager = GetComponentInChildren<AudioManager>(true).OrNull() ?? gameObject.AddComponent<AudioManager>();
            if (poolRoot == null)
            {
                poolRoot = new GameObject("PoolRoot").transform;
                poolRoot.SetParent(transform, false);
            }
            EnsureEventSystem();

            ui = new UIService(uiRoot);
            Services.Assets = new AssetService();
            Services.Downloads = new DownloadService();
            Services.UI = ui;
            Services.Scenes = new SceneService(loadingOverlay, ui);
            Services.Pool = new PoolManager(poolRoot);
            Services.Save = new SaveSystem();
            Services.Session = new SessionData();
            Services.Game = new GameManager();
            Services.Audio = audioManager;
            Services.Modules = moduleRegistry;
        }

        async void Start()
        {
            if (instance != this) return;
            var token = Services.AppToken;
            try
            {
                loadingOverlay?.Show("loading.step_core");

                var init = Addressables.InitializeAsync(false);
                await AddressablesUtil.Wait(init, token);
                bool initOk = init.IsValid() && init.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded;
                if (!initOk) Debug.LogError($"[Boot] Addressables.InitializeAsync lỗi: {(init.IsValid() ? init.OperationException?.Message : "handle không hợp lệ")}");
                if (init.IsValid()) Addressables.Release(init);
                if (!initOk)
                {
                    // Addressables không hỗ trợ khởi tạo lại trong cùng phiên → THỬ LẠI = tải lại trang (WebGL).
                    if (loadingOverlay == null) return;
                    await loadingOverlay.WaitRetry("loading.error");
                    ReloadPage();
                    return;
                }
                loadingOverlay?.SetProgress(0.15f);

                if (Services.Modules == null && AddressablesUtil.KeyExists(AddressKeys.CoreModuleRegistry))
                    Services.Modules = await Services.Assets.Global.Load<ModuleRegistry>(AddressKeys.CoreModuleRegistry);

                if (AddressablesUtil.KeyExists(AddressKeys.CoreFontConfig))
                {
                    var fontCfg = await Services.Assets.Global.Load<LocalizationFontConfig>(AddressKeys.CoreFontConfig);
                    LocalizationFontConfig.SetInstance(fontCfg);
                }

                if (AddressablesUtil.KeyExists<SfxLibrary>(AddressKeys.CoreSfxLibrary))
                {
                    var sfxLib = await Services.Assets.Global.Load<SfxLibrary>(AddressKeys.CoreSfxLibrary);
                    if (sfxLib != null) audioManager?.SetLibrary(sfxLib);
                }

                await Services.Save.LoadAsync();
                ApplySettings();
                loadingOverlay?.SetProgress(0.3f);

                var locInit = LocalizationSettings.InitializationOperation;
                while (!locInit.IsDone) await Awaitable.NextFrameAsync(token);
                // Ngôn ngữ: SaveData.settings.language là nguồn duy nhất; PlayerPrefs cũ chỉ để migrate một lần.
                string lang = Services.Save.Data.settings.language;
                string legacy = GameLanguage.LegacyPref;
                if (!string.IsNullOrEmpty(lang)) GameLanguage.Apply(lang, save: false);
                else if (!string.IsNullOrEmpty(legacy)) GameLanguage.Apply(legacy, save: true);
                else GameLanguage.ApplySaved(); // rỗng = theo ngôn ngữ máy, giữ rỗng trong save
                loadingOverlay?.SetProgress(0.45f);

                Services.MarkReady();

                // Save chỉ đọc (tab khác giữ khóa / save bản mới hơn): báo cho người chơi biết tiến trình sẽ không được lưu.
                if (!Services.Save.CanWrite) SaveReadOnlyBanner.Show(uiRoot, Services.Save.Data.FromNewerVersion);
                EventBus<SaveLockLostEvent>.Subscribe(OnSaveLockLost);

                loadingOverlay?.SetStep("loading.step_ui");
                // Mạng chập lúc tải lần đầu → hiện lỗi + THỬ LẠI thay vì treo màn loading.
                while (true)
                {
                    try
                    {
                        await Services.Downloads.Ensure(new[] { AddressKeys.LabelCore, AddressKeys.LabelSharedUi },
                            new ProgressCallback(p => loadingOverlay?.SetProgress(0.45f + p * 0.4f)));
                        break;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Boot] Tải core/shared-ui lỗi: {e.Message}");
                        if (loadingOverlay == null) throw;
                        await loadingOverlay.WaitRetry("loading.error");
                        loadingOverlay.Show("loading.step_ui");
                    }
                }
                Services.Downloads.Adopt(new[] { AddressKeys.LabelCore, AddressKeys.LabelSharedUi }, Services.Assets.Global);
                token.ThrowIfCancellationRequested();

                var started = FindStartedModuleScene();
                if (started.IsValid())
                {
                    Debug.Log($"[Boot] Editor: nhận scene đang mở '{started.name}'.");
                    await ((SceneService)Services.Scenes).Adopt(started, "scene/" + started.name.ToLowerInvariant());
                }
                else
                {
                    loadingOverlay?.SetStep("loading.step_menu");
                    await Services.Scenes.Go(firstSceneAddress);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void ApplySettings() => audioManager?.ApplySettings(Services.Save.Data.settings);

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void DG_ReloadPage();
        static void ReloadPage() => DG_ReloadPage();
#else
        static void ReloadPage() => Debug.LogError("[Boot] Không khởi tạo được Addressables — hãy khởi động lại game.");
#endif

        /// <summary>UI Settings gọi sau khi đổi giá trị (lưu có gộp).</summary>
        public static void SettingsChanged()
        {
            if (Services.Save == null) return;
            Services.Audio?.ApplySettings(Services.Save.Data.settings);
            Services.Save.RequestSave();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && Services.Ready && !Services.Scenes.IsTransitioning)
            {
                if (!Services.UI.Back() && Services.Game.State == GameState.Playing) VirtualInput.PressPause();
            }
        }

        void OnApplicationFocus(bool focus)
        {
            // Mất focus thì không nhận được pointer-up → xóa trạng thái giữ (Move/AttackHeld) để khỏi kẹt.
            if (!focus) VirtualInput.ResetState();
#if !UNITY_EDITOR
            if (!focus && Services.Ready && Services.Game != null && Services.Game.State == GameState.Playing) VirtualInput.PressPause();
#endif
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Services.Save?.FlushNow();
        }

        void OnApplicationQuit()
        {
            HandleAppExit();
        }

        public static void HandleAppExit()
        {
            var s = Services.Session;
            if (s != null && s.runActive && !s.settled)
            {
                // Nếu đã hạ Boss của tầng hiện tại (lastRunVictory == true) thì giữ túi đồ;
                // ngược lại nếu thoát khi đang đánh dở thì xóa túi đồ theo luật Abandon.
                if (s.lastRunVictory) RunFlow.Settle();
                else RunFlow.Abandon();
            }
            Services.Save?.FlushNow();
        }

        /// <summary>
        /// Rời trang trên WebGL (pagehide): chỉ chốt run đã thắng tầng hiện tại. Không Abandon run đang đánh dở,
        /// vì trang có thể được khôi phục (bfcache) và run vẫn tiếp tục.
        /// </summary>
        public static void SettleWonRunOnPageHide()
        {
            var s = Services.Session;
            if (s != null && s.runActive && !s.settled && s.lastRunVictory) RunFlow.Settle();
        }

        void OnSaveLockLost(SaveLockLostEvent e) => SaveReadOnlyBanner.Show(uiRoot, e.NewerSaveVersion);

        void OnDestroy()
        {
            if (instance != this) return;
            EventBus<SaveLockLostEvent>.Unsubscribe(OnSaveLockLost);
            Services.CancelApp();
            (Services.Downloads as DownloadService)?.ReleaseAll();
            instance = null;
        }

        static Scene FindStartedModuleScene()
        {
#if UNITY_EDITOR
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded && s.name != BootstrapSceneName && s.name != "DontDestroyOnLoad") return s;
            }
#endif
            return default;
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(transform, false);
        }

        /// <summary>UIRoot tối thiểu khi Bootstrap/UIRoot.prefab chưa dựng — chỉ để dev chạy được.</summary>
        UIRoot CreateFallbackUIRoot()
        {
            Debug.LogWarning("[Boot] Không có UIRoot — tạo UIRoot tạm thời.");
            var go = new GameObject("UIRoot (fallback)", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<PixelSnapCanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            return go.AddComponent<UIRoot>();
        }

        sealed class ProgressCallback : IProgress<float>
        {
            readonly Action<float> cb;
            public ProgressCallback(Action<float> cb) => this.cb = cb;
            public void Report(float v) => cb(v);
        }
    }

    /// <summary>Đảm bảo có Bootstrap khi bấm Play ở bất kỳ scene nào (ARCHITECTURE §1).</summary>
    static class BootstrapLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsureBootstrap()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).name == Systems.BootstrapSceneName) return;

            if (SceneUtility.GetBuildIndexByScenePath(Systems.BootstrapSceneName) >= 0 || BootstrapInBuild())
            {
                SceneManager.LoadScene(Systems.BootstrapSceneName, LoadSceneMode.Additive);
                return;
            }
            Debug.LogWarning("[Boot] Bootstrap chưa có trong Build Settings — tạo Systems tạm thời.");
            new GameObject("Systems (fallback)").AddComponent<Systems>();
        }

        static bool BootstrapInBuild()
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == Systems.BootstrapSceneName) return true;
            }
            return false;
        }
    }
}
