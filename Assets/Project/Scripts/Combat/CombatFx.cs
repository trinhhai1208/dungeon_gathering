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

        /// <summary>Spawn VFX one-shot qua pool; prefab không có AutoDestroyEffect sẽ được trả sau fallbackDuration.</summary>
        public static GameObject Play(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float fallbackDuration = 1f)
        {
            if (prefab == null || Services.Pool == null) return null;
            var go = Services.Pool.Get(prefab, position, rotation, parent);
            if (go != null && go.GetComponent<AutoDestroyEffect>() == null && fallbackDuration > 0f)
                Services.Pool.ReturnAfter(go, fallbackDuration);
            return go;
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
