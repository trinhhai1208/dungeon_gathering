using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Kết quả một lần nhận sát thương (để component hiển thị hiệu ứng, hook passive).</summary>
    public readonly struct VitalsHit
    {
        public readonly float Amount;        // tổng sát thương đã nhận (sau mọi giảm trừ)
        public readonly float Absorbed;      // phần giáp đỡ
        public readonly float HpLost;
        public readonly bool Fatal;
        public readonly bool ArmorBroken;    // giáp vừa về 0 ở đòn này

        public VitalsHit(float amount, float absorbed, float hpLost, bool fatal, bool armorBroken)
        {
            Amount = amount; Absorbed = absorbed; HpLost = hpLost; Fatal = fatal; ArmorBroken = armorBroken;
        }

        public bool Applied => Amount > 0f;
    }

    /// <summary>
    /// Tài nguyên sống (HP / giáp / mana) + StatSheet, dùng chung cho hero và quái. Thuần C# (không MonoBehaviour)
    /// để EditMode test được; component (PlayerStats, EnemyHealth) chỉ bọc lại và lo hiệu ứng hình/âm thanh.
    /// Quy tắc: giáp đỡ trước, dư mới trừ máu; chết → đặt IsDead TRƯỚC khi báo ra ngoài (R12), chỉ một lần.
    /// </summary>
    public sealed class ActorVitals
    {
        readonly StatSheet sheet;
        float hp, armor, mana;
        float armorRegenAcc;
        bool clampSuspended;

        public ActorVitals(BaseStats baseStats)
        {
            sheet = new StatSheet(baseStats);
            sheet.Changed += OnSheetChanged;
            FillAll();
        }

        public StatSheet Sheet => sheet;
        public float Hp => hp;
        public float Armor => armor;
        public float Mana => mana;
        public float MaxHp => sheet.Get(StatType.MaxHp);
        public float MaxArmor => sheet.Get(StatType.MaxArmor);
        public float MaxMana => sheet.Get(StatType.MaxMana);
        public bool IsDead { get; private set; }

        /// <summary>Max* thay đổi (trang bị, buff, bậc) → kẹp lại tài nguyên hiện có. Phát sau khi kẹp.</summary>
        public event Action Changed;

        /// <summary>Khởi tạo lại cho lượt sống mới (pool quái / Player vào tầng).</summary>
        public void Reset(BaseStats baseStats)
        {
            clampSuspended = true;           // tránh kẹp giữa chừng khi đang dựng lại
            sheet.Clear();
            sheet.SetBase(baseStats);
            IsDead = false;
            armorRegenAcc = 0f;
            clampSuspended = false;
            FillAll();
        }

        /// <summary>
        /// Tạm dừng kẹp khi đang cộng hàng loạt modifier (trang bị, set, innate…) để giá trị mang sang tầng không bị
        /// cắt về Max gốc. Gọi <see cref="EndBatch"/> khi cộng xong.
        /// </summary>
        public void BeginBatch() => clampSuspended = true;

        public void EndBatch()
        {
            clampSuspended = false;
            ClampAll();
            Changed?.Invoke();
        }

        public void FillAll()
        {
            hp = MaxHp; armor = MaxArmor; mana = MaxMana;
            Changed?.Invoke();
        }

        /// <summary>Đặt giá trị cụ thể (chỉ số mang sang tầng). Không kẹp nếu đang trong batch.</summary>
        public void SetCurrent(float newHp, float newArmor, float newMana)
        {
            hp = newHp; armor = newArmor; mana = newMana;
            if (!clampSuspended) ClampAll();
            Changed?.Invoke();
        }

        public VitalsHit ApplyDamage(float amount, bool ignoreArmor = false)
        {
            if (IsDead || amount <= 0f) return default;
            armorRegenAcc = 0f;
            bool hadArmor = armor > 0f;
            float absorbed = ignoreArmor ? 0f : Mathf.Min(armor, amount);
            armor -= absorbed;
            float rest = amount - absorbed;
            hp -= rest;
            bool fatal = hp <= 0f;
            if (fatal)
            {
                hp = 0f;
                IsDead = true;   // đặt trước khi báo ra ngoài
            }
            Changed?.Invoke();
            return new VitalsHit(amount, absorbed, rest, fatal, hadArmor && armor <= 0f);
        }

        /// <summary>Giết ngay (dọn quái con khi boss chết…). Trả false nếu đã chết.</summary>
        public bool Kill()
        {
            if (IsDead) return false;
            hp = 0f;
            IsDead = true;
            Changed?.Invoke();
            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            hp = Mathf.Min(MaxHp, hp + amount);
            Changed?.Invoke();
        }

        public void RestoreArmor(float amount)
        {
            if (IsDead || amount <= 0f) return;
            armor = Mathf.Min(MaxArmor, armor + amount);
            Changed?.Invoke();
        }

        public void RestoreMana(float amount)
        {
            if (IsDead || amount <= 0f) return;
            mana = Mathf.Min(MaxMana, mana + amount);
            Changed?.Invoke();
        }

        public bool TrySpendMana(float amount)
        {
            if (amount <= 0f) return true;
            if (IsDead || mana < amount) return false;
            mana -= amount;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Hồi theo thời gian. <paramref name="sinceLastHit"/> = giây kể từ lần bị đánh cuối; giáp hồi theo bước 1 điểm
        /// sau <paramref name="armorRegenDelay"/>; mana hồi liên tục theo chỉ số ManaRegen.
        /// </summary>
        public void Tick(float dt, float sinceLastHit, float armorRegenDelay, float armorRegenRate)
        {
            if (IsDead || dt <= 0f) return;
            bool changed = false;
            if (armorRegenRate > 0f && armor < MaxArmor && sinceLastHit >= armorRegenDelay)
            {
                armorRegenAcc += armorRegenRate * dt;
                while (armorRegenAcc >= 1f && armor < MaxArmor)
                {
                    armorRegenAcc -= 1f;
                    armor = Mathf.Min(MaxArmor, armor + 1f);
                    changed = true;
                }
            }
            float regen = sheet.Get(StatType.ManaRegen);
            if (regen > 0f && mana < MaxMana)
            {
                mana = Mathf.Min(MaxMana, mana + regen * dt);
                changed = true;
            }
            if (changed) Changed?.Invoke();
        }

        void OnSheetChanged()
        {
            if (clampSuspended) return;
            ClampAll();
            Changed?.Invoke();
        }

        void ClampAll()
        {
            hp = Mathf.Clamp(hp, 0f, MaxHp);
            armor = Mathf.Clamp(armor, 0f, MaxArmor);
            mana = Mathf.Clamp(mana, 0f, MaxMana);
        }
    }
}
