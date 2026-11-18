using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Đệ tử / Xương được triệu hồi: tự tìm quái, tấn công cận chiến và khiêu khích quái lân cận.</summary>
    [DisallowMultipleComponent]
    public sealed class CompanionActor : MonoBehaviour, ITargetable, IDamageable, IPoolable
    {
        static readonly Collider2D[] buffer = new Collider2D[24];
        readonly List<ITauntableTargetAI> taunted = new();

        SpriteRenderer sr;
        CircleCollider2D col;
        IPlayerContext owner;
        float hp;
        float damage;
        float moveSpeed;
        float expireAt;
        float tauntRadius;
        float attackRange = 1.1f;
        float attackInterval = 0.7f;
        float nextAttack;
        float nextTaunt;
        bool isDead;

        public Transform Transform => transform;
        public bool IsAlive => this != null && gameObject.activeInHierarchy && !isDead;
        public IDamageable Damageable => this;
        public bool IsDead => isDead;
        public Team Team => Team.Player;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;

            col = GetComponent<CircleCollider2D>();
            if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            int layer = CombatLayers.Layer(CombatLayers.Player);
            if (layer >= 0) gameObject.layer = layer;
        }

        public void Init(IPlayerContext ctx, Sprite sprite, float maxHp, float atkDamage, float speed, float duration, float tauntRad)
        {
            owner = ctx;
            if (sr == null) Awake();
            if (sprite != null) { sr.sprite = sprite; sr.color = Color.white; }
            else
            {
                // Chưa gán sprite cho đệ tử → mượn hình của hero (mờ, ánh tím) thay vì để đệ tử vô hình
                var ownerSr = ctx != null && ctx.Transform != null ? ctx.Transform.GetComponentInChildren<SpriteRenderer>() : null;
                sr.sprite = ownerSr != null ? ownerSr.sprite : null;
                sr.color = new Color(0.75f, 0.6f, 1f, 0.7f);
            }
            hp = Mathf.Max(1f, maxHp);
            damage = Mathf.Max(1f, atkDamage);
            moveSpeed = Mathf.Max(0.5f, speed);
            tauntRadius = Mathf.Max(0f, tauntRad);
            expireAt = Time.time + Mathf.Max(1f, duration);
            nextAttack = 0f;
            nextTaunt = 0f;
            isDead = false;
            col.enabled = true;
        }

        void Update()
        {
            if (isDead) return;
            if (Time.time >= expireAt || (owner != null && owner.IsDead))
            {
                Despawn();
                return;
            }

            if (tauntRadius > 0f && Time.time >= nextTaunt)
            {
                nextTaunt = Time.time + 0.4f;
                TauntNearby();
            }

            var target = CombatUtil.FindNearest(transform.position, 12f, CombatLayers.EnemyTargets, CombatLayers.Walls, false);
            if (target == null || target.IsDead) return;

            Vector2 to = (Vector2)target.transform.position - (Vector2)transform.position;
            float dist = to.magnitude;
            if (Mathf.Abs(to.x) > 0.05f && sr != null) sr.flipX = to.x < 0f;

            if (dist > attackRange * 0.85f)
            {
                float step = moveSpeed * Time.deltaTime;
                Vector2 dir = to / Mathf.Max(0.001f, dist);
                if (Physics2D.Raycast(transform.position, dir, step + 0.2f, CombatLayers.Walls).collider == null)
                    transform.position += (Vector3)(dir * step);
                return;
            }

            if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + attackInterval;
                Vector2 dir = dist > 0.001f ? to / dist : Vector2.right;
                if (target.TakeDamage(new DamageInfo(damage, 2f, dir, 0f, false, gameObject, Team.Player)))
                {
                    Sfx.PlayAt(SfxId.SwordHit, target.transform.position);
                    CombatFx.Hit(null, target.transform.position);
                }
            }
        }

        void TauntNearby()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = CombatLayers.EnemyTargets, useTriggers = true };
            int n = Physics2D.OverlapCircle(transform.position, tauntRadius, filter, buffer);
            for (int i = 0; i < n; i++)
            {
                var c = buffer[i];
                buffer[i] = null;
                if (c == null) continue;
                if (Physics2D.Linecast(transform.position, c.transform.position, CombatLayers.Walls).collider != null) continue;
                var ai = c.GetComponentInParent<ITauntableTargetAI>();
                if (ai == null) continue;
                if (ai.OverrideTarget == null || !ai.OverrideTarget.IsAlive)
                {
                    ai.OverrideTarget = this;
                    if (!taunted.Contains(ai)) taunted.Add(ai);
                }
            }
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (isDead || info.Team == Team.Player) return false;
            float amount = Mathf.Max(0f, info.Amount);
            if (amount <= 0f) return false;
            hp -= amount;
            CombatFx.DamageNumber(transform.position, amount, false, false);
            if (hp <= 0f)
            {
                hp = 0f;
                Despawn();
            }
            return true;
        }

        void ReleaseTaunts()
        {
            for (int i = 0; i < taunted.Count; i++)
            {
                var ai = taunted[i];
                if (ai == null || (ai is Object uo && uo == null)) continue;
                if (ai.OverrideTarget == (ITargetable)this)
                    ai.OverrideTarget = null;
            }
            taunted.Clear();
        }

        void Despawn()
        {
            if (isDead) return;
            isDead = true;
            CombatFx.Hit(null, transform.position);
            if (col != null) col.enabled = false;
            ReleaseTaunts();
            if (Services.Pool != null)
                Services.Pool.Return(gameObject);
            else
                Destroy(gameObject);
        }

        public void OnSpawn()
        {
            isDead = false;
            taunted.Clear();
            if (col != null) col.enabled = true;
        }

        public void OnDespawn()
        {
            isDead = true;
            ReleaseTaunts();
            owner = null;
        }

        void OnDisable() => ReleaseTaunts();
    }
}
