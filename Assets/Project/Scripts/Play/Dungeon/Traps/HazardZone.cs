using System.Collections.Generic;
using DungeonGathering.Combat;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Sàn nguyên tố (Độc / Lửa / Băng): làm chậm + DoT khi đứng trên (qua StatusEffectController).</summary>
    public class HazardZone : TrapBase
    {
        [SerializeField] string dotId = "hazard";
        [SerializeField] float dotDps = 1f;
        [SerializeField] float lingerDuration = 1f;
        [SerializeField, Range(0, 1)] float slowPercent = 0.3f;
        [SerializeField] GameObject loopVfx;

        readonly HashSet<StatusEffectController> inside = new();
        float refresh;

        protected override void OnEnable()
        {
            base.OnEnable();
            SetPhase(TrapPhase.Active);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsTarget(other)) return;
            var s = other.GetComponentInParent<StatusEffectController>();
            if (s != null) inside.Add(s);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var s = other.GetComponentInParent<StatusEffectController>();
            if (s != null) inside.Remove(s);
        }

        void Update()
        {
            if (Disabled) { inside.Clear(); return; }
            refresh -= Time.deltaTime;
            if (refresh > 0f) return;
            refresh = 0.25f;
            inside.RemoveWhere(s => s == null || !s.isActiveAndEnabled);
            foreach (var s in inside)
            {
                if (slowPercent > 0f) s.ApplySlow(slowPercent, lingerDuration);
                if (dotDps > 0f) s.ApplyDot(dotId, dotDps, lingerDuration, gameObject, loopVfx);
            }
        }
    }
}
