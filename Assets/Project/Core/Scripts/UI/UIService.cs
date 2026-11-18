using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonGathering.Core
{
    public interface IUIService
    {
        /// <param name="cancellableByBack">false → Back/Esc không hủy được lần Open đang chờ này (màn bắt buộc như Victory/Defeat).</param>
        Awaitable<T> Open<T>(string address, object args = null, bool cancellableByBack = true) where T : UIScreen;
        void Close(UIScreen screen);
        /// <summary>Nút Back / Esc: hủy Open đang chờ hoặc đóng màn trên cùng (Popup → Panel → Screen), cuối cùng hỏi Hud.OnBack.</summary>
        bool Back();
        /// <summary>Đóng mọi màn + hủy mọi Open đang chờ thuộc scope này (SceneService gọi).</summary>
        void CloseModule(ModuleScope owner);
        void SetBackdrop(bool visible);
        /// <summary>Màn đang mở theo address (null nếu không có).</summary>
        UIScreen Find(string address);
        bool IsOpen(string address);
    }

    /// <summary>
    /// Triển khai IUIService (MODULES §7.5, R5): một lần Open/address, owner = scope module đang active,
    /// màn tải xong sau khi owner đã dispose bị hủy, Back hủy Open đang chờ.
    /// Mỗi address được tải vào scope riêng; releaseOnClose → dispose khi đóng, ngược lại giữ cache tới khi module đóng.
    /// </summary>
    public sealed class UIService : IUIService
    {
        sealed class Pending
        {
            public string Address;
            public ModuleScope Owner;
            public bool Cancelled;
            public bool CancellableByBack = true;
            public readonly List<AwaitableCompletionSource<UIScreen>> Waiters = new();
        }

        sealed class Cached
        {
            public ModuleScope Scope;      // giữ handle prefab
            public ModuleScope Owner;      // module sở hữu
        }

        readonly UIRoot root;
        readonly List<UIScreen> open = new();                       // theo thứ tự mở
        readonly Dictionary<string, Pending> pending = new();
        readonly List<Pending> pendingOrder = new();
        readonly Dictionary<string, Cached> cache = new();
        readonly HashSet<object> blockers = new();
        readonly HashSet<string> warnedMissing = new();

        public UIService(UIRoot root) => this.root = root;

        public UIRoot Root => root;

        ModuleScope CurrentOwner => Services.Scenes?.CurrentScope ?? Services.Assets.Global;

        public UIScreen Find(string address)
        {
            for (int i = open.Count - 1; i >= 0; i--)
                if (open[i] != null && open[i].Address == address) return open[i];
            return null;
        }

        public bool IsOpen(string address) => Find(address) != null;

        public async Awaitable<T> Open<T>(string address, object args = null, bool cancellableByBack = true) where T : UIScreen
        {
            if (string.IsNullOrEmpty(address) || !AddressablesUtil.KeyExists<GameObject>(address))
            {
                if (warnedMissing.Add(address ?? "<null>"))
                    Debug.LogWarning($"[UI] Chưa đăng ký Addressables cho '{address}' — bỏ qua Open<{typeof(T).Name}>.");
                return null;
            }

            // Đang tải HOẶC đang chờ OnOpen (màn đã nằm trong `open` nhưng chưa mở xong) → chờ lần đang chạy
            // (Awaitable mới cho mỗi người chờ), rồi mới Reopen — không gọi OnReopen chen vào giữa OnOpen.
            if (pending.TryGetValue(address, out var running) && !running.Cancelled)
            {
                var waiter = new AwaitableCompletionSource<UIScreen>();
                running.Waiters.Add(waiter);
                var r = await waiter.Awaitable;
                if (r != null && r.IsOpen) BringToFront(r, args);
                return r as T;
            }

            // Đã mở → đưa lên trên cùng.
            var existing = Find(address);
            if (existing != null)
            {
                BringToFront(existing, args);
                return existing as T;
            }

            var p = new Pending { Address = address, Owner = CurrentOwner, CancellableByBack = cancellableByBack };
            pending[address] = p;
            pendingOrder.Add(p);
            SetInputBlocked(p, true);
            UIScreen screen = null;
            try
            {
                if (!cache.TryGetValue(address, out var cached) || cached.Scope.IsDisposed || cached.Owner.IsDisposed)
                {
                    if (cached != null && !cached.Scope.IsDisposed) cached.Scope.Dispose();
                    cached = new Cached { Scope = Services.Assets.CreateScope("ui:" + address), Owner = p.Owner };
                    cache[address] = cached;
                }

                var prefab = await cached.Scope.Load<GameObject>(address);
                if (p.Cancelled || p.Owner.IsDisposed || prefab == null) return Finish(p, null) as T;

                var prefabScreen = prefab.GetComponent<UIScreen>();
                if (prefabScreen == null)
                {
                    Debug.LogError($"[UI] Prefab '{address}' thiếu component UIScreen.");
                    return Finish(p, null) as T;
                }

                var parent = root.GetLayer(prefabScreen.Layer);
                var go = Object.Instantiate(prefab, parent, false);
                screen = go.GetComponent<UIScreen>();
                screen.Address = address;
                screen.Owner = p.Owner;
                Stretch(go.transform as RectTransform);
                open.Add(screen);
                screen.IsOpen = true;
                RefreshDimmer();
                ButtonSfx.AttachAll(go);
                if (IsModalLayer(screen.Layer)) Sfx.Play(SfxId.UiPanelOpen);

                var a = screen.OnOpen(args);
                if (a != null) await a;

                if (p.Cancelled || p.Owner.IsDisposed)
                {
                    Close(screen);
                    return Finish(p, null) as T;
                }
                return Finish(p, screen) as T;
            }
            catch (OperationCanceledException)
            {
                if (screen != null) Close(screen);
                return Finish(p, null) as T;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (screen != null) Close(screen);
                return Finish(p, null) as T;
            }
        }

        void BringToFront(UIScreen screen, object args)
        {
            screen.transform.SetAsLastSibling();
            open.Remove(screen);
            open.Add(screen);
            try { screen.OnReopen(args); } catch (Exception e) { Debug.LogException(e); }
        }

        static bool IsModalLayer(UILayer layer) => layer is UILayer.Panel or UILayer.Popup;

        UIScreen Finish(Pending p, UIScreen result)
        {
            if (pending.TryGetValue(p.Address, out var cur) && cur == p) pending.Remove(p.Address);
            pendingOrder.Remove(p);
            SetInputBlocked(p, false);
            if (result == null && !pending.ContainsKey(p.Address) && Find(p.Address) == null
                && cache.TryGetValue(p.Address, out var cached) && (cached.Owner == p.Owner || cached.Owner.IsDisposed))
            {
                cache.Remove(p.Address);
                cached.Scope.Dispose();
            }
            foreach (var w in p.Waiters) w.TrySetResult(result);
            return result;
        }

        public void Close(UIScreen screen)
        {
            if (screen == null || !screen.IsOpen) return;
            screen.IsOpen = false;
            open.Remove(screen);
            try { screen.OnClose(); } catch (Exception e) { Debug.LogException(e); }
            if (IsModalLayer(screen.Layer)) Sfx.Play(SfxId.UiPanelClose);
            string address = screen.Address;
            bool release = screen.ReleaseOnClose;
            Object.Destroy(screen.gameObject);
            if (release && address != null && cache.TryGetValue(address, out var cached) && Find(address) == null)
            {
                cache.Remove(address);
                cached.Scope.Dispose();
            }
            RefreshDimmer();
        }

        public bool Back()
        {
            // Hủy Open đang chờ ở Screen/Popup/Panel trước (không đóng màn bên dưới — R5).
            if (pendingOrder.Count > 0)
            {
                for (int i = pendingOrder.Count - 1; i >= 0; i--)
                {
                    if (!pendingOrder[i].CancellableByBack) continue;
                    pendingOrder[i].Cancelled = true;
                    return true;
                }
                return true; // chỉ còn màn bắt buộc đang tải (Victory/Defeat) → nuốt Back, không đóng màn bên dưới
            }
            foreach (var layer in new[] { UILayer.Popup, UILayer.Panel, UILayer.Screen })
            {
                for (int i = open.Count - 1; i >= 0; i--)
                {
                    var s = open[i];
                    if (s == null || s.Layer != layer) continue;
                    if (s.OnBack()) return true;
                    if (!s.CloseOnBack) return false;
                    Close(s);
                    return true;
                }
            }
            // Cuối cùng mới tới lớp Hud (vd LobbyHud.OnBack → về Menu). Hud thường trực: chỉ gọi OnBack, không tự đóng.
            for (int i = open.Count - 1; i >= 0; i--)
            {
                var s = open[i];
                if (s == null || s.Layer != UILayer.Hud) continue;
                if (s.OnBack()) return true;
            }
            return false;
        }

        public void CloseModule(ModuleScope owner)
        {
            if (owner == null) return;
            foreach (var p in pendingOrder)
                if (p.Owner == owner) p.Cancelled = true;
            for (int i = open.Count - 1; i >= 0; i--)
                if (open[i] != null && open[i].Owner == owner) Close(open[i]);
            open.RemoveAll(s => s == null);

            var drop = new List<string>();
            foreach (var kv in cache)
                if (kv.Value.Owner == owner && Find(kv.Key) == null && !pending.ContainsKey(kv.Key)) drop.Add(kv.Key);
            foreach (var key in drop)
            {
                cache[key].Scope.Dispose();
                cache.Remove(key);
            }
        }

        public void SetBackdrop(bool visible) => root.SetBackdrop(visible);

        /// <summary>Bật/tắt lớp chặn input theo "token" (đếm theo người yêu cầu).</summary>
        public void SetInputBlocked(object token, bool blocked)
        {
            if (blocked) blockers.Add(token);
            else blockers.Remove(token);
            root.SetInputBlocker(blockers.Count > 0);
        }

        void RefreshDimmer()
        {
            bool popup = false;
            foreach (var s in open)
                if (s != null && s.Layer == UILayer.Popup) { popup = true; break; }
            root.SetPopupDimmer(popup);
        }

        static void Stretch(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
