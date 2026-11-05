using System.Collections.Generic;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    public enum TrapPhase { Inactive, Warning, Active }

    /// <summary>
    /// Base bẫy sàn (ARCHITECTURE §7). targetMask quyết định chỉ hại Player hay cả quái.
    /// Mỗi mục tiêu bị đánh tối đa một lần mỗi chu kỳ Active. Run kết thúc → DisableAll.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class TrapBase : MonoBehaviour
    {
        static readonly List<TrapBase> all = new();

        [SerializeField] protected float damage = 1f;
        [SerializeField] protected float knockback = 2f;
        [Tooltip("Để trống (0) → chỉ Player")]
        [SerializeField] protected LayerMask targetMask;
        [SerializeField] protected SpriteRenderer visual;
        [SerializeField] protected Sprite inactiveSprite;
        [SerializeField] protected Sprite warningSprite;
        [SerializeField] protected Sprite activeSprite;

        protected Collider2D area;
        protected readonly HashSet<IDamageable> hitThisCycle = new();
        static readonly Collider2D[] buffer = new Collider2D[16];
        bool disabled;

        public TrapPhase Phase { get; private set; } = TrapPhase.Inactive;
        protected int Mask => targetMask.value != 0 ? targetMask.value : CombatLayers.PlayerTargets;
        protected bool Disabled => disabled;

        protected virtual void Awake()
        {
            area = GetComponent<Collider2D>();
            area.isTrigger = true;
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        }

        protected virtual void OnEnable() => all.Add(this);
        protected virtual void OnDisable() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => all.Clear();

        /// <summary>Tắt mọi bẫy (LevelDirector chốt run — không có sát thương đến sau).</summary>
        public static void DisableAll()
        {
            foreach (var t in all.ToArray()) if (t != null) t.SetDisabled(true);
        }

        public void SetDisabled(bool value)
        {
            disabled = value;
            if (value) SetPhase(TrapPhase.Inactive);
        }

        protected void SetPhase(TrapPhase p)
        {
            Phase = p;
            if (p == TrapPhase.Active) hitThisCycle.Clear();
            if (visual == null) return;
            var s = p switch { TrapPhase.Warning => warningSprite, TrapPhase.Active => activeSprite, _ => inactiveSprite };
            if (s != null) visual.sprite = s;
        }

        /// <summary>Gây sát thương cho mọi mục tiêu đang đứng trong vùng bẫy (mỗi mục tiêu 1 lần/chu kỳ).</summary>
        protected void DamageOverlapping()
        {
            if (disabled) return;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = Mask, useTriggers = true };
            int n = area.Overlap(filter, buffer);
            for (int i = 0; i < n; i++)
            {
                var d = CombatUtil.Resolve(buffer[i]);
                if (d == null || d.IsDead || !hitThisCycle.Add(d)) continue;
                Vector2 dir = ((Vector2)d.transform.position - (Vector2)transform.position).normalized;
                d.TakeDamage(new DamageInfo(damage, knockback, dir, 0f, false, gameObject, Team.Neutral));
            }
        }

        protected bool IsTarget(Collider2D c) => ((1 << c.gameObject.layer) & Mask) != 0;
    }
}
