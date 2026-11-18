using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace DungeonGathering.Core
{
    public interface ISceneService
    {
        string ActiveModule { get; }
        bool IsTransitioning { get; }
        /// <summary>Scope của module đang chạy (null trước khi vào module đầu tiên).</summary>
        ModuleScope CurrentScope { get; }
        /// <summary>
        /// Tải trước label của module đích (có loading), load scene ADDITIVE, đặt active,
        /// unload scene cũ + Dispose scope cũ, rồi gọi IModuleEntry.Begin. Gọi khi đang chuyển → bỏ qua (R1).
        /// </summary>
        Awaitable Go(string sceneAddress, object args = null);
    }

    /// <summary>Màn loading mà SceneService điều khiển (LoadingOverlay trong Bootstrap triển khai).</summary>
    public interface ILoadingView
    {
        void Show(string stepKey);
        void SetStep(string stepKey);
        void SetProgress(float progress01);
        /// <summary>Hiện trạng thái lỗi + THỬ LẠI; Awaitable xong khi người chơi bấm THỬ LẠI.</summary>
        Awaitable WaitRetry(string messageKey);
        void Hide();
    }

    public sealed class SceneService : ISceneService
    {
        readonly ILoadingView loading;
        readonly UIService ui;

        AsyncOperationHandle<SceneInstance> currentHandle;
        Scene currentScene;        // scene module đang active (kể cả scene "nhận nuôi" khi Play từ Editor)
        ModuleScope currentScope;

        public string ActiveModule { get; private set; }
        public bool IsTransitioning { get; private set; }
        public ModuleScope CurrentScope => currentScope;

        public SceneService(ILoadingView loading, UIService ui)
        {
            this.loading = loading;
            this.ui = ui;
        }

        static ModuleDefinition Resolve(string sceneAddress)
            => Services.Modules != null ? Services.Modules.FindByScene(sceneAddress) ?? ModuleRegistry.CreateFallback(sceneAddress)
                                        : ModuleRegistry.CreateFallback(sceneAddress);

        public async Awaitable Go(string sceneAddress, object args = null)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning($"[Scene] Đang chuyển scene — bỏ qua Go('{sceneAddress}').");
                return;
            }
            IsTransitioning = true;
            var token = Services.AppToken;
            var def = Resolve(sceneAddress);
            // Tài nguyên của module mới chưa "chốt" vào currentScope/currentHandle — lỗi/hủy giữa chừng phải tự dọn.
            ModuleScope newScope = null;
            AsyncOperationHandle<SceneInstance> handle = default;
            bool committed = false;
            try
            {
                // (1) overlay chặn input
                loading?.Show("common.loading");
                ui?.SetBackdrop(true);
                ui?.SetInputBlocked(this, true);
                Services.Audio?.StopBgm(0.3f);

                // (2) đóng UI của module cũ + hủy Open đang chờ
                if (currentScope != null) ui?.CloseModule(currentScope);

                // (3) Ensure label (có THỬ LẠI)
                while (true)
                {
                    try
                    {
                        await Services.Downloads.Ensure(def.requiredLabels, new Progress01(p => loading?.SetProgress(p * 0.7f)));
                        break;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                        if (loading == null) throw;
                        await loading.WaitRetry("loading.error");
                        loading.Show("common.loading");
                    }
                }
                token.ThrowIfCancellationRequested();

                // (4) scope mới — Adopt PHẢI chạy trước khi Dispose scope cũ (bước 7): cùng module (Play → Play)
                // thì label đã tải nằm trong scope cũ, Adopt Acquire thêm tham chiếu để bundle không bị unload.
                newScope = Services.Assets.CreateScope(def.moduleId);
                Services.Downloads.Adopt(def.requiredLabels, newScope);

                // (5) load scene additive
                while (true)
                {
                    handle = Addressables.LoadSceneAsync(sceneAddress, LoadSceneMode.Additive, true);
                    while (!handle.IsDone)
                    {
                        loading?.SetProgress(0.7f + handle.PercentComplete * 0.25f);
                        await Awaitable.NextFrameAsync(token);
                    }
                    if (handle.Status == AsyncOperationStatus.Succeeded) break;
                    Debug.LogError($"[Scene] Không load được '{sceneAddress}': {handle.OperationException?.Message}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    handle = default;
                    if (loading == null) return; // finally dọn newScope
                    await loading.WaitRetry("loading.error");
                    loading.Show("common.loading");
                }

                // (6) active scene
                var scene = handle.Result.Scene;
                SceneManager.SetActiveScene(scene);
                newScope.Scene = scene;

                // (7) unload scene cũ → dọn pool theo owner → dispose scope cũ
                var oldScope = currentScope;
                await UnloadCurrent(token);
                if (oldScope != null)
                {
                    Services.Pool?.ClearOwner(oldScope);
                    oldScope.Dispose();
                }

                currentHandle = handle;
                currentScene = scene;
                currentScope = newScope;
                committed = true;
                ActiveModule = def.moduleId;
                Services.Game?.SetState(def.gameState);
                loading?.SetProgress(1f);

                // (8) báo scene mới bắt đầu
                await BeginEntry(scene, new ModuleContext(newScope, def, args));

                ui?.SetBackdrop(def.showBackdrop);
                if (!string.IsNullOrEmpty(def.bgmAddress)) Services.Audio?.PlayBgm(def.bgmAddress);
                Services.Downloads.ReleaseUnadopted(def.prefetchLabels);
                _ = Services.Downloads.Prefetch(def.prefetchLabels);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                if (!committed) DiscardUncommitted(newScope, handle, token);
                // (9) ẩn overlay
                loading?.Hide();
                ui?.SetInputBlocked(this, false);
                IsTransitioning = false;
            }
        }

        /// <summary>
        /// Go thất bại/bị hủy trước khi chốt: Dispose scope mới, unload scene mới (nếu đã load) và trả active scene
        /// về scene module hiện tại (nếu vẫn còn).
        /// </summary>
        void DiscardUncommitted(ModuleScope newScope, AsyncOperationHandle<SceneInstance> handle, System.Threading.CancellationToken token)
        {
            try
            {
                if (newScope != null)
                {
                    Services.Pool?.ClearOwner(newScope);
                    newScope.Dispose();
                }
                if (handle.IsValid())
                {
                    if (currentScene.IsValid() && currentScene.isLoaded) SceneManager.SetActiveScene(currentScene);
                    // Đang thoát game: không cần unload, chỉ Release để không rò handle.
                    if (token.IsCancellationRequested) Addressables.Release(handle);
                    else Addressables.UnloadSceneAsync(handle);
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }

        /// <summary>
        /// Editor: Play từ một scene module (vd Play.unity) — scene đã được Unity load sẵn (không qua Addressables).
        /// SceneService nhận nuôi scene đó rồi gọi Begin như bình thường.
        /// </summary>
        public async Awaitable Adopt(Scene scene, string sceneAddress, object args = null)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            try
            {
                var def = Resolve(sceneAddress);
                var scope = Services.Assets.CreateScope(def.moduleId);
                scope.Scene = scene;
                SceneManager.SetActiveScene(scene);
                currentScene = scene;
                currentScope = scope;
                currentHandle = default;
                ActiveModule = def.moduleId;
                Services.Game?.SetState(def.gameState);
                await Services.Downloads.Ensure(def.requiredLabels, null);
                Services.Downloads.Adopt(def.requiredLabels, scope);
                await BeginEntry(scene, new ModuleContext(scope, def, args));
                ui?.SetBackdrop(def.showBackdrop);
                if (!string.IsNullOrEmpty(def.bgmAddress)) Services.Audio?.PlayBgm(def.bgmAddress);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogException(e); }
            finally
            {
                loading?.Hide();
                IsTransitioning = false;
            }
        }

        async Awaitable UnloadCurrent(System.Threading.CancellationToken token)
        {
            if (currentHandle.IsValid())
            {
                var op = Addressables.UnloadSceneAsync(currentHandle);
                await AddressablesUtil.Wait(op, token);
            }
            else if (currentScene.IsValid() && currentScene.isLoaded)
            {
                var op = SceneManager.UnloadSceneAsync(currentScene);
                while (op != null && !op.isDone) await Awaitable.NextFrameAsync(token);
            }
            currentHandle = default;
            currentScene = default;
        }

        static async Awaitable BeginEntry(Scene scene, ModuleContext ctx)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var entry = root.GetComponentInChildren<IModuleEntry>(true);
                if (entry == null) continue;
                var a = entry.Begin(ctx);
                if (a != null) await a;
                return;
            }
            Debug.LogWarning($"[Scene] Scene '{scene.name}' không có IModuleEntry.");
        }

        sealed class Progress01 : IProgress<float>
        {
            readonly Action<float> cb;
            public Progress01(Action<float> cb) => this.cb = cb;
            public void Report(float value) => cb?.Invoke(value);
        }
    }
}
