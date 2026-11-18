using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Bẫy theo chu kỳ (gai thò thụt, phun lửa): Inactive → Warning (0.5s) → Active → lặp.</summary>
    public class TimedTrap : TrapBase
    {
        [SerializeField] float inactiveTime = 1.5f;
        [SerializeField] float warningTime = 0.5f;
        [SerializeField] float activeTime = 0.6f;
        [SerializeField] float startOffset;

        float timer;

        protected override void ApplyData()
        {
            base.ApplyData();
            if (data == null) return;
            inactiveTime = data.inactiveTime;
            warningTime = data.warningTime;
            activeTime = data.activeTime;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            timer = -startOffset;
            SetPhase(TrapPhase.Inactive);
        }

        void Update()
        {
            if (Disabled) return;
            timer += Time.deltaTime;
            switch (Phase)
            {
                case TrapPhase.Inactive when timer >= inactiveTime:
                    timer = 0f; SetPhase(TrapPhase.Warning); break;
                case TrapPhase.Warning when timer >= warningTime:
                    timer = 0f; SetPhase(TrapPhase.Active); break;
                case TrapPhase.Active:
                    DamageOverlapping();
                    if (timer >= activeTime) { timer = 0f; SetPhase(TrapPhase.Inactive); }
                    break;
            }
        }
    }
}
