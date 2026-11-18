using System.Collections.Generic;
using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Sàn nguyên tố (Độc / Lửa / Băng): gây lại các hiệu ứng (StatusEffectRef — vd Burn + Slow) mỗi 0.25s khi đứng trên;
    /// rời sàn thì hiệu ứng còn vương theo thời gian của ref. Số liệu lấy từ TrapDataSO.zoneEffects.
    /// </summary>
    public class HazardZone : TrapBase
    {
        [Tooltip("Dùng khi không có TrapDataSO")]
        [SerializeField] List<DungeonGathering.Catalog.StatusEffectRef> effects = new();

        readonly Dictionary<StatusEffectController, int> insideCounts = new();
        readonly List<StatusEffectController> stale = new();
        float refresh;

        protected override void ApplyData()
        {
            base.ApplyData();
            if (data == null || data.zoneEffects == null) return;
            effects = new List<DungeonGathering.Catalog.StatusEffectRef>(data.zoneEffects);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            refresh = 0f;
            SetPhase(TrapPhase.Active);
        }

        protected override void OnDisable()
        {
            insideCounts.Clear();
            base.OnDisable();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Disabled || !IsTarget(other)) return;
            var s = other.GetComponentInParent<StatusEffectController>();
            if (s != null)
                insideCounts[s] = insideCounts.TryGetValue(s, out var count) ? count + 1 : 1;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var s = other.GetComponentInParent<StatusEffectController>();
            if (s == null || !insideCounts.TryGetValue(s, out var count)) return;
            if (count <= 1) insideCounts.Remove(s);
            else insideCounts[s] = count - 1;
        }

        void Update()
        {
            if (Disabled) { insideCounts.Clear(); return; }
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.25f;

            stale.Clear();
            foreach (var s in insideCounts.Keys)
            {
                if (s == null || !s.isActiveAndEnabled) stale.Add(s);
            }
            for (int i = 0; i < stale.Count; i++) insideCounts.Remove(stale[i]);

            foreach (var s in insideCounts.Keys)
                foreach (var e in effects) s.Apply(e, gameObject);
        }
    }
}
