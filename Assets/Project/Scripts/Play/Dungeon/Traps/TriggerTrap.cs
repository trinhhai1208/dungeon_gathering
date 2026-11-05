using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Bẫy cảm biến: có đối tượng dẫm lên → Warning ngắn → đâm lên → nghỉ.</summary>
    public class TriggerTrap : TrapBase
    {
        [SerializeField] float warningTime = 0.35f;
        [SerializeField] float activeTime = 0.5f;
        [SerializeField] float cooldown = 1f;

        float timer;
        bool cooling;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Disabled || cooling || Phase != TrapPhase.Inactive || !IsTarget(other)) return;
            timer = 0f;
            SetPhase(TrapPhase.Warning);
        }

        void Update()
        {
            if (Disabled) return;
            timer += Time.deltaTime;
            switch (Phase)
            {
                case TrapPhase.Warning when timer >= warningTime:
                    timer = 0f; SetPhase(TrapPhase.Active); break;
                case TrapPhase.Active:
                    DamageOverlapping();
                    if (timer >= activeTime) { timer = 0f; cooling = true; SetPhase(TrapPhase.Inactive); }
                    break;
                case TrapPhase.Inactive when cooling && timer >= cooldown:
                    cooling = false; break;
            }
        }
    }
}
