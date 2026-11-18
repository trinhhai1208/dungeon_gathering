using DG.Tweening;
using DungeonGathering.Core;
using DungeonGathering.VFX;
using TMPro;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>
    /// Điểm spawn VFX/số sát thương dùng chung. Prefab (DamagePopup, hit mặc định) do LevelDirector cấu hình khi Begin.
    /// Mọi thứ đi qua PoolManager.
    /// </summary>
    public static class CombatFx
    {
        static DamagePopup popupPrefab;
        static GameObject defaultHitVfx;

        public static void Configure(DamagePopup popup, GameObject defaultHit)
        {
            popupPrefab = popup;
            defaultHitVfx = defaultHit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { popupPrefab = null; defaultHitVfx = null; }

        const float ReferenceBodyHeight = 1.15f;

        /// <summary>Spawn VFX one-shot qua pool; prefab không có AutoDestroyEffect sẽ được trả sau fallbackDuration.</summary>
        public static GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float fallbackDuration = 1f)
        {
            if (prefab == null || Services.Pool == null) return null;
            var go = Services.Pool.Get(prefab, position, rotation, parent);
            if (go == null) return null;
            ResetPooledVisuals(go, prefab);
            EnsureCombatSorting(go);
            if (go.GetComponent<AutoDestroyEffect>() == null && fallbackDuration > 0f)
                Services.Pool.ReturnAfter(go, fallbackDuration);
            return go;
        }

        /// <summary>
        /// Gắn VFX bám theo thực thể (status, khiên bảo vệ, aura): căn tâm/đỉnh đầu theo SpriteRenderer thực tế
        /// và bù lossyScale của parent để Boss không làm VFX phình quá cỡ so với Player.
        /// </summary>
        public static GameObject AttachScaled(
            GameObject prefab, Transform targetRoot, bool overhead = false, float fallbackDuration = -1f,
            float minScale = 0.85f, float maxScale = 1.85f)
        {
            if (prefab == null || targetRoot == null || Services.Pool == null) return null;
            GetTargetBodyInfo(targetRoot, overhead, out Vector3 worldPos, out float bodyHeight);
            var go = Services.Pool.Get(prefab, worldPos, Quaternion.identity, targetRoot);
            if (go == null) return null;

            ResetPooledVisuals(go, prefab);
            EnsureCombatSorting(go);

            float bodyRatio = Mathf.Clamp(bodyHeight / ReferenceBodyHeight, minScale, maxScale);
            Vector3 pScale = targetRoot.lossyScale;
            float sx = Mathf.Abs(pScale.x) > 0.01f ? pScale.x : 1f;
            float sy = Mathf.Abs(pScale.y) > 0.01f ? pScale.y : 1f;
            Vector3 baseScale = prefab.transform.localScale;
            go.transform.localScale = new Vector3(
                baseScale.x * bodyRatio / sx,
                baseScale.y * bodyRatio / sy,
                baseScale.z);
            go.transform.position = worldPos;

            if (go.GetComponent<AutoDestroyEffect>() == null && fallbackDuration > 0f)
                Services.Pool.ReturnAfter(go, fallbackDuration);
            return go;
        }

        static void GetTargetBodyInfo(Transform root, bool overhead, out Vector3 worldPos, out float bodyHeight)
        {
            var anim = root.GetComponentInChildren<SpriteAnimDriver>(true);
            var sr = anim != null && anim.Renderer != null
                ? anim.Renderer
                : root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null)
            {
                var b = sr.bounds;
                bodyHeight = b.size.y > 0.05f ? b.size.y : ReferenceBodyHeight;
                worldPos = overhead
                    ? new Vector3(b.center.x, b.max.y + 0.15f, root.position.z)
                    : new Vector3(b.center.x, b.center.y, root.position.z);
                return;
            }
            bodyHeight = ReferenceBodyHeight;
            worldPos = overhead ? root.position + new Vector3(0f, 0.7f, 0f) : root.position;
        }

        /// <summary>Khôi phục scale gốc và khởi động lại Animator/Particle cho VFX lặp không có AutoDestroyEffect.</summary>
        public static void ResetPooledVisuals(GameObject go, GameObject prefab)
        {
            if (go == null || prefab == null) return;
            go.transform.localScale = prefab.transform.localScale;
            if (go.GetComponent<AutoDestroyEffect>() != null) return;
            foreach (var a in go.GetComponentsInChildren<Animator>(true))
                if (a != null && a.runtimeAnimatorController != null) { a.Rebind(); a.Update(0f); }
            foreach (var p in go.GetComponentsInChildren<ParticleSystem>(true))
                if (p != null) { p.Clear(true); p.Play(true); }
        }

        /// <summary>Đưa VFX chưa cấu hình sorting layer (Default/0) lên lớp Characters/15 để không bị nền/nhân vật che.</summary>
        public static void EnsureCombatSorting(GameObject go, string layerName = "Characters", int order = 15)
        {
            if (go == null) return;
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr != null && sr.sortingLayerName == "Default" && sr.sortingOrder == 0)
                {
                    sr.sortingLayerName = layerName;
                    sr.sortingOrder = order;
                }
        }

        public static void Hit(GameObject prefab, Vector3 position) => Play(prefab != null ? prefab : defaultHitVfx, position, Quaternion.identity);

        public static void DamageNumber(Vector3 position, float amount, bool crit, bool playerHurt = false)
        {
            if (popupPrefab == null || Services.Pool == null || amount <= 0f) return;
            var p = Services.Pool.Get(popupPrefab, position + new Vector3(Random.Range(-0.2f, 0.2f), 0.4f, 0f), Quaternion.identity);
            if (p != null) p.Show(amount, crit, playerHurt);
        }
    }
}
