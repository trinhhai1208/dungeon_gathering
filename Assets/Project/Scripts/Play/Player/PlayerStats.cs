using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// HP / Giáp / Mana của Player (ENTITIES §2.2). Giáp kiểu Soul Knight: trừ giáp trước, dư mới trừ máu;
    /// không bị đánh armorRegenDelay giây → giáp hồi dần. Bỏ qua đòn khi IsDead / bất tử. Die() một lần.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour, IDamageable
    {
        [SerializeField] float hurtInvulnerability = 0.6f;
        [SerializeField] Color hurtFlash = new(1f, 0.4f, 0.4f);

        StatSheet sheet = new(BaseStats.Default);
        CharacterData character;
        PlayerController controller;
        PlayerMovement movement;
        float hp, armor, mana;
        float lastHitTime = -999f;
        float armorRegenAcc;
        bool isDead;
        readonly HashSet<object> invulnerableSources = new();
        int lastHp = -1, lastArmor = -1, lastMana = -1, lastMaxHp = -1, lastMaxArmor = -1, lastMaxMana = -1;

        public StatSheet Sheet => sheet;
        public bool IsDead => isDead;
        public Team Team => Team.Player;
        public float Hp => hp;
        public float Armor => armor;
        public float Mana => mana;
        public float MaxHp => sheet.Get(StatType.MaxHp);
        public float MaxArmor => sheet.Get(StatType.MaxArmor);
        public float MaxMana => sheet.Get(StatType.MaxMana);
        public bool Invulnerable => invulnerableSources.Count > 0 || Time.time < lastHitTime + hurtInvulnerability;

        public event Action Died;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            movement = GetComponent<PlayerMovement>();
        }

        public float Get(StatType t) => sheet.Get(t);

        public void Init(CharacterData data, RunStats carried)
        {
            character = data;
            sheet = new StatSheet(data != null ? data.baseStats : BaseStats.Default);
            sheet.Changed += OnSheetChanged;
            isDead = false;
            invulnerableSources.Clear();
            if (carried != null && carried.hasValues)
            {
                hp = carried.hp; armor = carried.armor; mana = carried.mana;
            }
            else
            {
                hp = MaxHp; armor = MaxArmor; mana = MaxMana;
            }
            ClampAll();
            Publish(true);
        }

        /// <summary>Gọi sau khi trang bị cộng chỉ số lúc bắt đầu tầng (giữ đầy nếu run mới).</summary>
        public void FillIfNew(bool newRun)
        {
            if (!newRun) { ClampAll(); Publish(true); return; }
            hp = MaxHp; armor = MaxArmor; mana = MaxMana;
            Publish(true);
        }

        public RunStats Snapshot(int activeWeaponSlot) => new()
        {
            hasValues = true, hp = hp, armor = armor, mana = mana, activeWeaponSlot = activeWeaponSlot,
        };

        void OnSheetChanged() { ClampAll(); Publish(false); }

        void ClampAll()
        {
            hp = Mathf.Clamp(hp, isDead ? 0f : Mathf.Min(1f, MaxHp), MaxHp);
            armor = Mathf.Clamp(armor, 0f, MaxArmor);
            mana = Mathf.Clamp(mana, 0f, MaxMana);
        }

        public void SetInvulnerable(object source, bool on)
        {
            if (source == null) return;
            if (on) invulnerableSources.Add(source); else invulnerableSources.Remove(source);
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (isDead || Invulnerable) return false;
            if (info.Team == Team.Player) return false;
            if (movement != null && movement.IsDashing) return false;
            float amount = Mathf.Max(0f, info.Amount);
            if (amount <= 0f) return false;

            lastHitTime = Time.time;
            armorRegenAcc = 0f;
            bool hadArmor = armor > 0f;
            float absorbed = Mathf.Min(armor, amount);
            armor -= absorbed;
            float rest = amount - absorbed;
            hp -= rest;

            CombatFx.DamageNumber(transform.position, amount, false, true);
            controller?.Anim?.Flash(hurtFlash);
            controller?.Anim?.Play(AnimKey.Hurt);
            Sfx.Play(SfxId.PlayerHurt);
            if (info.Knockback > 0f && info.Direction != Vector2.zero) movement?.Knockback(info.Direction.normalized * info.Knockback);
            controller?.Hooks.RaiseDamageTaken(amount);
            if (hadArmor && armor <= 0f) controller?.Hooks.RaiseArmorBroken();

            if (hp <= 0f)
            {
                hp = 0f;
                isDead = true;     // đặt trước khi gọi bất cứ gì khác (R12)
                Publish(true);
                Die();
                return true;
            }
            Publish(false);
            return true;
        }

        void Die()
        {
            controller?.Anim?.Play(AnimKey.Death, true);
            try { Died?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent { Position = transform.position });
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0f) return;
            hp = Mathf.Min(MaxHp, hp + amount);
            Publish(false);
        }

        public void RestoreArmor(float amount)
        {
            if (isDead || amount <= 0f) return;
            armor = Mathf.Min(MaxArmor, armor + amount);
            Publish(false);
        }

        public void RestoreMana(float amount)
        {
            if (isDead || amount <= 0f) return;
            mana = Mathf.Min(MaxMana, mana + amount);
            Publish(false);
        }

        public bool TrySpendMana(float amount)
        {
            if (amount <= 0f) return true;
            if (isDead || mana < amount) return false;
            mana -= amount;
            Publish(false);
            return true;
        }

        void Update()
        {
            if (isDead || character == null) return;
            float dt = Time.deltaTime;
            if (armor < MaxArmor && Time.time >= lastHitTime + character.armorRegenDelay)
            {
                armorRegenAcc += character.armorRegenRate * dt;
                if (armorRegenAcc >= 1f)
                {
                    armorRegenAcc -= 1f;
                    armor = Mathf.Min(MaxArmor, armor + 1f);
                    Publish(false);
                }
            }
            if (character.manaRegen > 0f && mana < MaxMana)
            {
                mana = Mathf.Min(MaxMana, mana + character.manaRegen * dt);
                Publish(false);
            }
        }

        /// <summary>Phát PlayerStatsChangedEvent khi giá trị nguyên đổi.</summary>
        void Publish(bool force)
        {
            int h = Mathf.CeilToInt(hp), a = Mathf.FloorToInt(armor), m = Mathf.FloorToInt(mana);
            int mh = Mathf.RoundToInt(MaxHp), ma = Mathf.RoundToInt(MaxArmor), mm = Mathf.RoundToInt(MaxMana);
            if (!force && h == lastHp && a == lastArmor && m == lastMana && mh == lastMaxHp && ma == lastMaxArmor && mm == lastMaxMana) return;
            lastHp = h; lastArmor = a; lastMana = m; lastMaxHp = mh; lastMaxArmor = ma; lastMaxMana = mm;
            EventBus<PlayerStatsChangedEvent>.Raise(Current);
        }

        public PlayerStatsChangedEvent Current => new()
        {
            Hp = Mathf.CeilToInt(hp), MaxHp = Mathf.RoundToInt(MaxHp),
            Armor = Mathf.FloorToInt(armor), MaxArmor = Mathf.RoundToInt(MaxArmor),
            Mana = Mathf.FloorToInt(mana), MaxMana = Mathf.RoundToInt(MaxMana),
        };
    }
}
