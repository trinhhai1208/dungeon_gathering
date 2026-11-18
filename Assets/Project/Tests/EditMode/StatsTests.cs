using DungeonGathering.Catalog;
using DungeonGathering.Core;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 1: StatSheet, StatBlock (profile + ghi đè), ActorVitals, hệ số bậc quái.</summary>
    public class StatsTests
    {
        static BaseStats Hero => new()
        {
            maxHp = 6, maxArmor = 4, maxMana = 180, attackDamage = 1, attackSpeed = 1, moveSpeed = 5,
            critChance = 0.05f, critDamage = 2, cooldownReduction = 0, manaRegen = 2,
        };

        [Test]
        public void StatSheet_FlatThenPercent()
        {
            var s = new StatSheet(Hero);
            var src = new object();
            s.Add(src, new StatModifier(StatType.MaxHp, 2f));          // 6 + 2
            s.Add(src, new StatModifier(StatType.MaxHp, 0f, 0.5f));    // × 1.5
            Assert.AreEqual(12f, s.Get(StatType.MaxHp), 1e-4);
            s.RemoveSource(src);
            Assert.AreEqual(6f, s.Get(StatType.MaxHp), 1e-4);
        }

        [Test]
        public void StatSheet_ClampsSpecialStats()
        {
            var s = new StatSheet(Hero);
            s.Add(this, new StatModifier(StatType.CritChance, 3f));
            s.Add(this, new StatModifier(StatType.CooldownReduction, 2f));
            s.Add(this, new StatModifier(StatType.KnockbackResist, 5f));
            Assert.AreEqual(1f, s.Get(StatType.CritChance), 1e-4);
            Assert.AreEqual(0.5f, s.Get(StatType.CooldownReduction), 1e-4);
            Assert.AreEqual(1f, s.Get(StatType.KnockbackResist), 1e-4);
        }

        [Test]
        public void StatBlock_ProfilePlusOverrides()
        {
            var profile = ScriptableObject.CreateInstance<StatProfileSO>();
            profile.stats = Hero;
            var block = new StatBlock { profile = profile };
            block.overrides.Add(new StatOverride(StatType.MaxHp, 9f));
            var r = block.Resolve(default);
            Assert.AreEqual(9f, r.maxHp);
            Assert.AreEqual(5f, r.moveSpeed);
            Assert.AreEqual(6f, profile.stats.maxHp, "Resolve không được ghi vào SO");
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void StatBlock_NoProfile_UsesFallback()
        {
            var block = new StatBlock();
            block.overrides.Add(new StatOverride(StatType.MoveSpeed, 3f));
            var r = block.Resolve(Hero);
            Assert.AreEqual(6f, r.maxHp);
            Assert.AreEqual(3f, r.moveSpeed);
        }

        [Test]
        public void Vitals_ArmorAbsorbsFirst_ThenHp()
        {
            var v = new ActorVitals(Hero);
            var hit = v.ApplyDamage(5f);
            Assert.AreEqual(4f, hit.Absorbed);
            Assert.AreEqual(1f, hit.HpLost);
            Assert.IsTrue(hit.ArmorBroken);
            Assert.AreEqual(5f, v.Hp);
            Assert.IsFalse(hit.Fatal);
        }

        [Test]
        public void Vitals_DiesOnce()
        {
            var v = new ActorVitals(Hero);
            Assert.IsTrue(v.ApplyDamage(100f).Fatal);
            Assert.IsTrue(v.IsDead);
            Assert.IsFalse(v.ApplyDamage(100f).Applied, "Đã chết thì không nhận thêm");
            Assert.IsFalse(v.Kill());
        }

        [Test]
        public void Vitals_BatchKeepsCarriedValuesUntilModifiersApplied()
        {
            // Mang sang tầng: HP 8 khi trang bị cho +2 MaxHp. Không được bị cắt về 6 trong lúc đang cộng trang bị.
            var v = new ActorVitals(Hero);
            v.Reset(Hero);
            v.BeginBatch();
            v.SetCurrent(8f, 4f, 100f);
            v.Sheet.Add(this, new StatModifier(StatType.MaxArmor, 1f));  // modifier khác tới trước
            v.Sheet.Add(this, new StatModifier(StatType.MaxHp, 2f));
            v.EndBatch();
            Assert.AreEqual(8f, v.Hp, 1e-4);
        }

        [Test]
        public void Vitals_ClampsWhenMaxDrops()
        {
            var v = new ActorVitals(Hero);
            var src = new object();
            v.Sheet.Add(src, new StatModifier(StatType.MaxHp, 4f));
            v.FillAll();
            Assert.AreEqual(10f, v.Hp, 1e-4);
            v.Sheet.RemoveSource(src);  // tháo trang bị
            Assert.AreEqual(6f, v.Hp, 1e-4);
        }

        [Test]
        public void Vitals_ArmorRegenAfterDelay_ManaRegen()
        {
            var v = new ActorVitals(Hero);
            v.ApplyDamage(3f);                      // giáp 4 → 1
            v.TrySpendMana(100f);                   // mana 80
            v.Tick(1f, sinceLastHit: 1f, armorRegenDelay: 3f, armorRegenRate: 1f);
            Assert.AreEqual(1f, v.Armor, 1e-4, "Chưa hết delay thì chưa hồi giáp");
            Assert.AreEqual(82f, v.Mana, 1e-4);
            v.Tick(2f, sinceLastHit: 4f, armorRegenDelay: 3f, armorRegenRate: 1f);
            Assert.AreEqual(3f, v.Armor, 1e-4);
        }

        [Test]
        public void TierModifiers_ScaleStats()
        {
            var cfg = ScriptableObject.CreateInstance<MonsterTierConfigSO>();
            var elite = cfg.Get(MonsterTier.Elite);
            var s = new StatSheet(new BaseStats { maxHp = 40, attackDamage = 1, moveSpeed = 2, critDamage = 2 });
            s.AddRange(cfg, MonsterTierConfigSO.Modifiers(elite));
            Assert.AreEqual(40f * elite.hpMultiplier, s.Get(StatType.MaxHp), 1e-3);
            Assert.AreEqual(elite.damageMultiplier, s.Get(StatType.AttackDamage), 1e-3);
            Assert.AreEqual(2f * elite.moveSpeedMultiplier, s.Get(StatType.MoveSpeed), 1e-3);

            var normal = new StatSheet(new BaseStats { maxHp = 40, attackDamage = 1 });
            normal.AddRange(cfg, MonsterTierConfigSO.Modifiers(cfg.Get(MonsterTier.Normal)));
            Assert.AreEqual(40f, normal.Get(StatType.MaxHp), 1e-4);
            Object.DestroyImmediate(cfg);
        }
    }
}
