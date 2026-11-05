using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DungeonGathering.Core
{
    /// <summary>Object sinh nhiều lần: OnSpawn reset trạng thái, OnDespawn dọn (DOTween.Kill, StopAllCoroutines, velocity 0).</summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>
    /// Pool tập trung (MODULES §7.7). Key = (owner.InstanceId, prefab.GetInstanceID()).
    /// Không tự load prefab; Return idempotent; ClearOwner hủy object của đúng scope (R2).
    /// </summary>
    public sealed class PoolManager
    {
        internal sealed class Pool
        {
            public readonly GameObject Prefab;
            public readonly ModuleScope Owner;
            public readonly Stack<PooledObject> Free = new();
            public readonly HashSet<PooledObject> All = new();
            public Pool(GameObject prefab, ModuleScope owner) { Prefab = prefab; Owner = owner; }
        }

        readonly Dictionary<(int owner, int prefab), Pool> pools = new();
        readonly Transform root;

        public PoolManager(Transform root) => this.root = root;

        static ModuleScope DefaultOwner => Services.Scenes?.CurrentScope ?? Services.Assets?.Global;

        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            => Get(prefab, DefaultOwner, position, rotation, parent);

        public T Get<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (prefab == null) return null;
            var go = Get(prefab.gameObject, DefaultOwner, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        public GameObject Get(GameObject prefab, ModuleScope owner, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) { Debug.LogWarning("[Pool] Get(null)"); return null; }
            if (owner == null || owner.IsDisposed)
            {
                Debug.LogWarning($"[Pool] Owner {owner} đã dispose — không spawn {prefab.name}.");
                return null;
            }
            var pool = GetPool(prefab, owner);
            PooledObject po = null;
            while (pool.Free.Count > 0 && po == null)
            {
                po = pool.Free.Pop();
                if (po == null) continue; // bị hủy bên ngoài
            }
            if (po == null) po = Create(pool);

            var t = po.transform;
            t.SetParent(parent, false);
            if (parent == null && owner.Scene.IsValid() && owner.Scene.isLoaded && po.gameObject.scene != owner.Scene)
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(po.gameObject, owner.Scene);
            t.SetPositionAndRotation(position, rotation);
            po.InPool = false;
            po.SpawnVersion++;
            po.gameObject.SetActive(true);
            foreach (var p in po.Poolables)
            {
                try { p.OnSpawn(); } catch (Exception e) { Debug.LogException(e, po); }
            }
            return po.gameObject;
        }

        /// <summary>Tạo trước count instance (không kích hoạt).</summary>
        public void Prewarm(GameObject prefab, ModuleScope owner, int count)
        {
            if (prefab == null || owner == null || owner.IsDisposed) return;
            var pool = GetPool(prefab, owner);
            for (int i = pool.All.Count; i < count; i++)
            {
                var po = Create(pool);
                po.InPool = true;
                pool.Free.Push(po);
            }
        }

        /// <summary>Trả về pool. Idempotent: object đã trong pool → bỏ qua; pool đã ClearOwner / không phải pool → Destroy.</summary>
        public void Return(GameObject go)
        {
            if (go == null) return;
            var po = go.GetComponent<PooledObject>();
            if (po == null)
            {
                Object.Destroy(go);
                return;
            }
            if (po.InPool)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"[Pool] Return 2 lần: {go.name}", go);
#endif
                return;
            }
            po.InPool = true;
            foreach (var p in po.Poolables)
            {
                try { p.OnDespawn(); } catch (Exception e) { Debug.LogException(e, go); }
            }
            var pool = po.Pool;
            if (pool == null || pool.Owner.IsDisposed)
            {
                Object.Destroy(go);
                return;
            }
            pool.Free.Push(po);
            if (!go.activeInHierarchy)
            {
                // Có thể đang trong OnDisable (cha bị tắt / scene đang unload): Unity cấm SetParent/SetActive lúc này
                // → dời sang frame sau. Get trước đó vẫn dùng được (tự SetParent + SetActive).
                _ = ParkLater(po, po.SpawnVersion);
                return;
            }
            go.SetActive(false);
            go.transform.SetParent(root, false);
        }

        async Awaitable ParkLater(PooledObject po, int version)
        {
            try { await Awaitable.NextFrameAsync(Services.AppToken); }
            catch (OperationCanceledException) { return; }
            // Bị hủy cùng scene → Get sẽ bỏ qua phần tử null trong Free. Đã được Get lại → không đụng tới.
            if (po == null || !po.InPool || po.SpawnVersion != version) return;
            if (po.Pool == null || po.Pool.Owner.IsDisposed) { Object.Destroy(po.gameObject); return; }
            po.gameObject.SetActive(false);
            po.transform.SetParent(root, false);
        }

        /// <summary>Trả về pool sau delay nếu vẫn là lần spawn đó (spawnVersion khớp).</summary>
        public async void ReturnAfter(GameObject go, float delay)
        {
            if (go == null) return;
            var po = go.GetComponent<PooledObject>();
            int version = po != null ? po.SpawnVersion : 0;
            try { await Awaitable.WaitForSecondsAsync(delay, Services.AppToken); }
            catch (OperationCanceledException) { return; }
            if (go == null) return;
            if (po == null) { Object.Destroy(go); return; }
            if (po.InPool || po.SpawnVersion != version) return;
            Return(go);
        }

        /// <summary>Hủy mọi object (đang dùng + trong pool) của các pool thuộc owner. SceneService gọi trước khi Dispose scope.</summary>
        public void ClearOwner(ModuleScope owner)
        {
            if (owner == null) return;
            var remove = new List<(int, int)>();
            foreach (var kv in pools)
            {
                if (kv.Key.owner != owner.InstanceId) continue;
                foreach (var po in kv.Value.All)
                {
                    if (po == null) continue;
                    po.Pool = null;
                    Object.Destroy(po.gameObject);
                }
                kv.Value.All.Clear();
                kv.Value.Free.Clear();
                remove.Add(kv.Key);
            }
            foreach (var k in remove) pools.Remove(k);
        }

        public int CountFree(GameObject prefab, ModuleScope owner)
            => pools.TryGetValue((owner.InstanceId, prefab.GetInstanceID()), out var p) ? p.Free.Count : 0;

        Pool GetPool(GameObject prefab, ModuleScope owner)
        {
            var key = (owner.InstanceId, prefab.GetInstanceID());
            if (!pools.TryGetValue(key, out var pool))
            {
                pool = new Pool(prefab, owner);
                pools[key] = pool;
                owner.Disposed += ClearOwner;
            }
            return pool;
        }

        PooledObject Create(Pool pool)
        {
            bool wasActive = pool.Prefab.activeSelf;
            // Tạo ở trạng thái tắt để Awake/OnEnable chạy đúng lúc Get.
            if (wasActive) pool.Prefab.SetActive(false);
            GameObject go;
            try { go = Object.Instantiate(pool.Prefab, root, false); }
            finally { if (wasActive) pool.Prefab.SetActive(true); }
            go.name = pool.Prefab.name;
            var po = go.GetComponent<PooledObject>();
            if (po == null) po = go.AddComponent<PooledObject>();
            po.Pool = pool;
            po.Poolables = go.GetComponentsInChildren<IPoolable>(true);
            po.InPool = true;
            pool.All.Add(po);
            return po;
        }
    }
}
