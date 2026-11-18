using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGathering.Combat
{
    public enum AnimKey { Idle, Move, Attack, Hurt, Death }

    /// <summary>
    /// Điều khiển Animator dạng "chỉ có state" (controller của asset pack không có parameter):
    /// tìm state theo từ khóa trong tên clip (idle / run|move|walk / attack / hurt|damage / death|die) rồi Play.
    /// Có thể ghi đè tên state trong Inspector. Hurt/Attack là one-shot: hết clip tự về Idle/Move.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpriteAnimDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Ghi đè tên state (để trống = tự dò theo tên clip)")]
        [SerializeField] string idleState, moveState, attackState, hurtState, deathState;
        [Tooltip("Sprite gốc nhìn sang phải?")]
        [SerializeField] bool facesRight = true;

        readonly Dictionary<AnimKey, int> states = new();
        readonly Dictionary<AnimKey, float> lengths = new();
        RuntimeAnimatorController builtFor;
        AnimKey current = AnimKey.Idle;
        float oneShotUntil;
        bool locked; // Death
        Color restColor = Color.white;
        Color? statusTint;
        Color tierTint = Color.clear;   // alpha = độ đậm pha vào màu nghỉ (quái Elite)
        bool refsReady;
        Color ActiveRestColor => statusTint ?? (tierTint.a > 0f ? Color.Lerp(restColor, new Color(tierTint.r, tierTint.g, tierTint.b, restColor.a), tierTint.a) : restColor);

        public Animator Animator { get { EnsureRefs(); return animator; } }
        public SpriteRenderer Renderer { get { EnsureRefs(); return spriteRenderer; } }

        static readonly (AnimKey key, string[] words)[] Keywords =
        {
            (AnimKey.Death, new[] { "death", "die", "dead" }),
            (AnimKey.Hurt, new[] { "hurt", "take_damage", "damage", "hit" }),
            (AnimKey.Attack, new[] { "attack", "atk", "shoot" }),
            (AnimKey.Move, new[] { "run", "move", "movemen", "walk" }),
            (AnimKey.Idle, new[] { "idle" }),
        };

        void EnsureRefs()
        {
            if (refsReady) return;
            refsReady = true;
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (spriteRenderer == null) spriteRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) restColor = spriteRenderer.color;
        }

        void Awake() => EnsureRefs();

        public void SetController(RuntimeAnimatorController controller)
        {
            if (animator == null || controller == null) return;
            animator.runtimeAnimatorController = controller;
            builtFor = null;
        }

        void Build()
        {
            states.Clear();
            lengths.Clear();
            builtFor = animator != null ? animator.runtimeAnimatorController : null;
            if (builtFor == null) return;
            Override(AnimKey.Idle, idleState);
            Override(AnimKey.Move, moveState);
            Override(AnimKey.Attack, attackState);
            Override(AnimKey.Hurt, hurtState);
            Override(AnimKey.Death, deathState);
            foreach (var clip in builtFor.animationClips)
            {
                if (clip == null) continue;
                string n = clip.name.ToLowerInvariant();
                foreach (var (key, words) in Keywords)
                {
                    if (states.ContainsKey(key)) continue;
                    bool match = false;
                    foreach (var w in words) if (n.Contains(w)) { match = true; break; }
                    if (!match) continue;
                    int hash = Animator.StringToHash(clip.name);
                    if (animator.HasState(0, hash)) { states[key] = hash; lengths[key] = clip.length; }
                    break;
                }
            }
        }

        void Override(AnimKey key, string state)
        {
            if (string.IsNullOrEmpty(state)) return;
            int hash = Animator.StringToHash(state);
            if (animator.HasState(0, hash)) states[key] = hash;
        }

        public void Play(AnimKey key, bool force = false)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            if (builtFor != animator.runtimeAnimatorController) Build();
            if (locked && key != AnimKey.Death) return;
            if (!force && key == current && key != AnimKey.Hurt && key != AnimKey.Attack) return;
            // One-shot đang chạy: chỉ Hurt/Attack/Death được cắt ngang.
            if (!force && Time.time < oneShotUntil && (key == AnimKey.Idle || key == AnimKey.Move)) return;

            AnimKey use = key;
            if (!states.ContainsKey(use) && key == AnimKey.Move) use = AnimKey.Idle;
            if (!states.TryGetValue(use, out int hash)) return;
            animator.Play(hash, 0, 0f);
            current = key;
            if (key is AnimKey.Hurt or AnimKey.Attack)
                oneShotUntil = Time.time + (lengths.TryGetValue(use, out var l) ? Mathf.Min(l, 0.6f) : 0.25f);
            if (key == AnimKey.Death) locked = true;
        }

        /// <summary>Idle/Move theo vận tốc (gọi mỗi frame).</summary>
        public void Locomotion(Vector2 velocity) => Play(velocity.sqrMagnitude > 0.01f ? AnimKey.Move : AnimKey.Idle);

        public void Face(float x)
        {
            if (spriteRenderer == null || Mathf.Abs(x) < 0.01f) return;
            spriteRenderer.flipX = facesRight ? x < 0f : x > 0f;
        }

        public float Length(AnimKey key)
        {
            if (builtFor != animator?.runtimeAnimatorController) Build();
            return lengths.TryGetValue(key, out var l) ? l : 0f;
        }

        public void ResetState()
        {
            EnsureRefs();
            CancelInvoke(nameof(EndFlash));
            statusTint = null;
            tierTint = Color.clear;
            if (spriteRenderer != null) spriteRenderer.color = restColor;
            locked = false;
            oneShotUntil = 0f;
            current = (AnimKey)(-1);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            Play(AnimKey.Idle, true);
        }

        /// <summary>Màu nhận diện theo bậc quái (alpha = độ đậm; Color.clear = bỏ). Hiệu ứng trạng thái vẫn đè lên.</summary>
        public void SetTierTint(Color tint)
        {
            EnsureRefs();
            tierTint = tint;
            if (spriteRenderer != null && !IsInvoking(nameof(EndFlash)))
                spriteRenderer.color = ActiveRestColor;
        }

        public void SetStatusTint(Color? tint)
        {
            EnsureRefs();
            statusTint = tint;
            if (spriteRenderer != null && !IsInvoking(nameof(EndFlash)))
                spriteRenderer.color = ActiveRestColor;
        }

        public void Flash(Color color, float duration = 0.08f)
        {
            EnsureRefs();
            if (spriteRenderer == null) return;
            CancelInvoke(nameof(EndFlash));
            spriteRenderer.color = color;
            Invoke(nameof(EndFlash), duration);
        }

        void EndFlash() { if (spriteRenderer != null) spriteRenderer.color = ActiveRestColor; }

        void OnDisable() => CancelInvoke(nameof(EndFlash));
    }
}
