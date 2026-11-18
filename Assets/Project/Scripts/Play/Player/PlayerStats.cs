using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// HP / Giáp / Mana của Player. Logic tài nguyên nằm ở <see cref="ActorVitals"/> (dùng chung với quái, có test);
    /// component này chỉ lo i-frame, lướt, hiệu ứng hình/âm thanh, hook passive và sự kiện HUD.
    /// Giáp kiểu Soul Knight: trừ giáp trước, dư mới trừ máu; không bị đánh một lúc → giáp hồi dần.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStats : MonoBehaviour, IDamageable, IStatSheetProvider
    {
        StatSheet IStatSheetProvider.Stats => Sheet;

        [SerializeField] float hurtInvulnerability = 0.6f;
        [SerializeField] Color hurtFlash = new(1f, 0.4f, 0.4f);

        readonly ActorVitals vitals = new(BaseStats.Default);
        HeroDefinitionSO character;
        PlayerController controller;
        PlayerMovement movement;
        float lastHitTime = -999f;
        float armorRegenDelay = 4f, armorRegenRate = 1f;
        bool initialized;
        readonly HashSet<object> invulnerableSources = new();
        int lastHp = -1, lastArmor = -1, lastMana = -1, lastMaxHp = -1, lastMaxArmor = -1, lastMaxMana = -1;

        public ActorVitals Vitals => vitals;
        public StatSheet Sheet => vitals.Sheet;
        public bool IsDead => vitals.IsDead;
        public Team Team => Team.Player;
        public float Hp => vitals.Hp;
        public float Armor => vitals.Armor;
        public float Mana => vitals.Mana;
        public float MaxHp => vitals.MaxHp;
        public float MaxArmor => vitals.MaxArmor;
        public float MaxMana => vitals.MaxMana;
        public bool Invulnerable => invulnerableSources.Count > 0 || Time.time < lastHitTime + hurtInvulnerability;

        public event Action Died;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            movement = GetComponent<PlayerMovement>();
            vitals.Changed += () => Publish(false);
        }

        public float Get(StatType t) => vitals.Sheet.Get(t);

        public void Init(HeroDefinitionSO data, RunStats carried) => Init(data, carried, false);

        /// <param name="deferClamp">
        /// true: trang bị / set / innate / passive vũ khí còn đang cộng Max* → chưa kẹp HP/giáp/mana mang sang
        /// (nếu không sẽ bị cắt về Max gốc mỗi tầng). Gọi <see cref="FillIfNew"/> khi cộng xong.
        /// </param>
        public void Init(HeroDefinitionSO data, RunStats carried, bool deferClamp)
        {
            character = data;
            armorRegenDelay = data != null ? data.ResolveArmorRegenDelay() : 4f;
            armorRegenRate = data != null ? data.ResolveArmorRegenRate() : 1f;
            invulnerableSources.Clear();
            lastHitTime = -999f;
            initialized = true;

            vitals.Reset(data != null ? data.ResolveStats() : BaseStats.Default);
            if (deferClamp) vitals.BeginBatch();
            if (carried != null && carried.hasValues) vitals.SetCurrent(carried.hp, carried.armor, carried.mana);
            if (!deferClamp) Publish(true);
        }

        /// <summary>Gọi sau khi mọi nguồn modifier lúc vào tầng đã cộng xong (run mới → làm đầy).</summary>
        public void FillIfNew(bool newRun)
        {
            vitals.EndBatch();
            if (newRun) vitals.FillAll();
            Publish(true);
        }

        public RunStats Snapshot(int activeWeaponSlot) => new()
        {
            hasValues = true, hp = vitals.Hp, armor = vitals.Armor, mana = vitals.Mana, activeWeaponSlot = activeWeaponSlot,
        };

        public void SetInvulnerable(object source, bool on)
        {
            if (source == null) return;
            if (on) invulnerableSources.Add(source); else invulnerableSources.Remove(source);
        }

        public bool TakeDamage(in DamageInfo info)
        {
            if (IsDead) return false;
            // DoT: bỏ qua i-frame sau đòn đánh (nếu không, nhịp 0.5s bị i-frame 0.6s chặn một nửa) nhưng vẫn tôn trọng
            // bất tử chủ động (skill, chuyển tầng) và lướt.
            bool dot = info.IsDoT;
            if (dot ? invulnerableSources.Count > 0 : Invulnerable) return false;
            if (info.Team == Team.Player) return false;
            if (movement != null && movement.IsDashing) return false;
            float amount = Mathf.Max(0f, info.Amount);
            if (amount <= 0f) return false;

            if (!dot) lastHitTime = Time.time;   // DoT không đặt lại i-frame / thời gian chờ hồi giáp
            var hit = vitals.ApplyDamage(amount, info.IgnoresArmor);
            if (!hit.Applied) return false;

            CombatFx.DamageNumber(transform.position, amount, false, true);
            if (!dot && info.Source != null && info.Source.TryGetComponent<IHitVfxSource>(out var vfxSrc) && vfxSrc.HitVfx != null)
                CombatFx.Play(vfxSrc.HitVfx, transform.position, Quaternion.identity);
            controller?.Anim?.Flash(hurtFlash);
            if (!dot)
            {
                controller?.Anim?.Play(AnimKey.Hurt);
                Sfx.Play(SfxId.PlayerHurt);
            }
            if (!dot && !hit.Fatal && info.Knockback > 0f && info.Direction != Vector2.zero)
                movement?.Knockback(info.Direction.normalized * info.Knockback * (1f - Get(StatType.KnockbackResist)));
            controller?.Hooks.RaiseDamageTaken(amount);
            if (!hit.Fatal && hit.ArmorBroken) controller?.Hooks.RaiseArmorBroken();

            if (hit.Fatal)
            {
                Publish(true);
                Die();
            }
            return true;
        }

        void Die()
        {
            controller?.Anim?.Play(AnimKey.Death, true);
            try { Died?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            EventBus<PlayerDiedEvent>.Raise(new PlayerDiedEvent { Position = transform.position });
        }

        public void Heal(float amount) => vitals.Heal(amount);
        public void RestoreArmor(float amount) => vitals.RestoreArmor(amount);
        public void RestoreMana(float amount) => vitals.RestoreMana(amount);
        public bool TrySpendMana(float amount) => vitals.TrySpendMana(amount);

        void Update()
        {
            if (!initialized || IsDead || character == null) return;
            vitals.Tick(Time.deltaTime, Time.time - lastHitTime, armorRegenDelay, armorRegenRate);
        }

        /// <summary>Phát PlayerStatsChangedEvent khi giá trị nguyên đổi.</summary>
        void Publish(bool force)
        {
            int h = Mathf.CeilToInt(Hp), a = Mathf.FloorToInt(Armor), m = Mathf.FloorToInt(Mana);
            int mh = Mathf.RoundToInt(MaxHp), ma = Mathf.RoundToInt(MaxArmor), mm = Mathf.RoundToInt(MaxMana);
            if (!force && h == lastHp && a == lastArmor && m == lastMana && mh == lastMaxHp && ma == lastMaxArmor && mm == lastMaxMana) return;
            lastHp = h; lastArmor = a; lastMana = m; lastMaxHp = mh; lastMaxArmor = ma; lastMaxMana = mm;
            EventBus<PlayerStatsChangedEvent>.Raise(Current);
        }

        public PlayerStatsChangedEvent Current => new()
        {
            Hp = Mathf.CeilToInt(Hp), MaxHp = Mathf.RoundToInt(MaxHp),
            Armor = Mathf.FloorToInt(Armor), MaxArmor = Mathf.RoundToInt(MaxArmor),
            Mana = Mathf.FloorToInt(Mana), MaxMana = Mathf.RoundToInt(MaxMana),
        };
    }
}
