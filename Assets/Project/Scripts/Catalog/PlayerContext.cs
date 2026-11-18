using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Báo cáo một đòn trúng (passive dùng để làm chậm, hút máu, sét lan...).</summary>
    public struct HitReport
    {
        public GameObject Target;
        public Vector2 Point;
        public Vector2 Direction;
        public float Damage;
        public bool Crit;
        public bool Killed;
        public WeaponData Weapon;
    }

    /// <summary>Những gì kỹ năng/passive được phép làm với Player (Player triển khai, Catalog không biết Play).</summary>
    public interface IPlayerContext
    {
        GameObject GameObject { get; }
        Transform Transform { get; }
        HeroDefinitionSO Character { get; }
        PlayerCombatHooks Hooks { get; }
        Vector2 AimDirection { get; }
        Vector2 MoveInput { get; }
        bool IsDead { get; }
        float CurrentHp { get; }
        float CurrentArmor { get; }
        float CurrentMana { get; }

        float GetStat(StatType stat);
        void AddModifier(object source, StatModifier modifier);
        void RemoveModifiers(object source);
        /// <summary>Buff tạm thời, tự gỡ sau duration giây.</summary>
        void AddTimedModifier(StatModifier modifier, float duration);
        void Heal(float amount);
        void RestoreArmor(float amount);
        void RestoreMana(float amount);
        bool TrySpendMana(float amount);
        void SetInvulnerable(object source, bool on);
        /// <summary>Lướt tới vị trí (dash) trong duration giây.</summary>
        void Dash(Vector2 direction, float distance, float duration);
        /// <summary>Lần bắn kế tiếp được cường hóa (xuyên, x sát thương).</summary>
        void QueueEmpoweredAttack(float damageMultiplier, int extraPierce);
        /// <summary>Vũ khí gọi khi tung đòn: lấy cường hóa đang chờ (nếu có).</summary>
        bool TryConsumeEmpowered(out float damageMultiplier, out int extraPierce);
    }
}
