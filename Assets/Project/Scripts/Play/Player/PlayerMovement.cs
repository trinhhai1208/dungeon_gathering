using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Rigidbody2D top-down: tốc độ từ PlayerStats (MoveSpeed) × làm chậm, đẩy lùi, lướt.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] float knockbackDecay = 18f;

        Rigidbody2D rb;
        PlayerStats stats;
        StatusEffectController status;
        Vector2 desired;
        Vector2 knock;
        Vector2 dashVelocity;
        float dashUntil;

        public Vector2 Velocity => rb.linearVelocity;
        public bool IsDashing => Time.time < dashUntil;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stats = GetComponent<PlayerStats>();
            status = GetComponent<StatusEffectController>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void SetMove(Vector2 dir) => desired = Vector2.ClampMagnitude(dir, 1f);

        public void Knockback(Vector2 impulse) => knock += impulse;

        public void Dash(Vector2 dir, float distance, float duration)
        {
            duration = Mathf.Max(0.05f, duration);
            dashVelocity = dir.normalized * (distance / duration);
            dashUntil = Time.time + duration;
        }

        public void CancelDash() => dashUntil = 0f;

        public void Stop()
        {
            desired = knock = Vector2.zero;
            dashUntil = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        void FixedUpdate()
        {
            if (stats != null && stats.IsDead) { rb.linearVelocity = Vector2.zero; return; }
            if (IsDashing) { rb.linearVelocity = dashVelocity; return; }
            float speed = stats != null ? stats.Get(StatType.MoveSpeed) : 5f;
            float slow = status != null ? status.SpeedMultiplier : 1f;
            rb.linearVelocity = desired * speed * slow + knock;
            knock = Vector2.MoveTowards(knock, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
        }
    }
}
