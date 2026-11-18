using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 2: cộng dồn hiệu ứng, Slow là modifier, DoT tick + bỏ qua i-frame, dọn khi chết.</summary>
    public class StatusEffectTests
    {
        sealed class FakeTarget : IStatusTarget
        {
            public readonly StatSheet Sheet = new(new BaseStats { maxHp = 10, moveSpeed = 4, attackDamage = 1, critDamage = 2 });
            public float Damage;
            public float Hp = 10f;
            public bool Dead;
            public System.Action OnDeath;
            public bool IsDead => Dead;
            public StatSheet Stats => Sheet;
            public void ApplyStatusDamage(float amount, GameObject source)
            {
                Damage += amount;
                Hp -= amount;
                if (Hp <= 0f && !Dead) { Dead = true; OnDeath?.Invoke(); }
            }
        }

        readonly List<Object> created = new();

        T Make<T>(StackPolicy stacking, float dur = 2f, float mag = 1f, int maxStacks = 1) where T : StatusEffectSO
        {
            var e = ScriptableObject.CreateInstance<T>();
            e.stacking = stacking; e.defaultDuration = dur; e.defaultMagnitude = mag; e.maxStacks = maxStacks;
            created.Add(e);
            return e;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Ref_UsesDefaultsUnlessOverridden()
        {
            var stun = Make<StunEffectSO>(StackPolicy.KeepStrongest, dur: 1f);
            var a = new StatusEffectRef(stun);
            var b = new StatusEffectRef(stun, duration: 3f);
            Assert.AreEqual(1f, a.Duration);
            Assert.AreEqual(3f, b.Duration);
            Assert.AreEqual(1f, b.Chance, "chance 0 (asset cũ) hiểu là 1");
        }

        [Test]
        public void Slow_IsMoveSpeedModifier_RemovedOnExpire()
        {
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var slow = Make<SlowEffectSO>(StackPolicy.KeepStrongest, dur: 1f, mag: 0.5f);
            set.Apply(slow, 1f, 0.5f);
            Assert.AreEqual(2f, t.Sheet.Get(StatType.MoveSpeed), 1e-4);
            set.Tick(1.1f);
            Assert.AreEqual(4f, t.Sheet.Get(StatType.MoveSpeed), 1e-4);
            Assert.AreEqual(0, set.Active.Count);
        }

        [Test]
        public void KeepStrongest_WeakLongSlowDoesNotExtendStrongSlow()
        {
            // Bug cũ: đang chậm 50% còn 0.5s, dính chậm 10% trong 3s → thành chậm 50% trong 3s.
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var slow = Make<SlowEffectSO>(StackPolicy.KeepStrongest);
            set.Apply(slow, 0.5f, 0.5f);
            set.Apply(slow, 3f, 0.1f);
            Assert.AreEqual(0.5f, set.Find(slow).Remaining, 1e-4);
            Assert.AreEqual(0.5f, set.Find(slow).Magnitude, 1e-4);
            set.Apply(slow, 1f, 0.8f); // mạnh hơn → thay cả cặp
            Assert.AreEqual(1f, set.Find(slow).Remaining, 1e-4);
            Assert.AreEqual(4f * 0.2f, t.Sheet.Get(StatType.MoveSpeed), 1e-4, "Modifier cập nhật theo độ mạnh mới");
        }

        [Test]
        public void Slow_TwoDifferentAssets_StrongestWins_NotSummed()
        {
            // Bug cũ: chậm 0.6 (băng) + 0.5 (bẫy) cộng thành −110% → MoveSpeed = 0.
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var frost = Make<SlowEffectSO>(StackPolicy.KeepStrongest);
            var trap = Make<SlowEffectSO>(StackPolicy.KeepStrongest);
            set.Apply(frost, 1f, 0.6f);
            set.Apply(trap, 3f, 0.5f);
            Assert.AreEqual(4f * 0.4f, t.Sheet.Get(StatType.MoveSpeed), 1e-4);
            set.Tick(1.1f); // băng hết → còn bẫy
            Assert.AreEqual(4f * 0.5f, t.Sheet.Get(StatType.MoveSpeed), 1e-4);
            set.Clear();
            Assert.AreEqual(4f, t.Sheet.Get(StatType.MoveSpeed), 1e-4);
        }

        [Test]
        public void Stack_AddsStacksUpToMax()
        {
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var dot = Make<DamageOverTimeEffectSO>(StackPolicy.Stack, maxStacks: 3);
            for (int i = 0; i < 5; i++) set.Apply(dot, 2f, 1f);
            Assert.AreEqual(3, set.Find(dot).Stacks);
            Assert.AreEqual(3f, set.Find(dot).Magnitude, 1e-4);
        }

        [Test]
        public void Extend_AddsDuration()
        {
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var e = Make<StunEffectSO>(StackPolicy.Extend);
            set.Apply(e, 1f, 1f);
            set.Apply(e, 1.5f, 1f);
            Assert.AreEqual(2.5f, set.Find(e).Remaining, 1e-4);
        }

        [Test]
        public void Refresh_ReplacesDurationAndMagnitude()
        {
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var e = Make<SlowEffectSO>(StackPolicy.Refresh);
            set.Apply(e, 3f, 0.5f);
            set.Apply(e, 1f, 0.2f);
            Assert.AreEqual(1f, set.Find(e).Remaining, 1e-4);
            Assert.AreEqual(0.2f, set.Find(e).Magnitude, 1e-4);
        }

        [Test]
        public void Dot_TicksEveryInterval_FullDamage()
        {
            // 2 dps trong 2.5s, nhịp 0.5s → 5 nhịp × 1 = 5 sát thương (trước đây i-frame chặn còn 2).
            var t = new FakeTarget { Hp = 100f };
            var set = new StatusEffectSet(t);
            var dot = Make<DamageOverTimeEffectSO>(StackPolicy.KeepStrongest);
            dot.tickInterval = 0.5f;
            set.Apply(dot, 2.5f, 2f);
            for (int i = 0; i < 30; i++) set.Tick(0.1f);
            Assert.AreEqual(5f, t.Damage, 1e-3);
            Assert.AreEqual(0, set.Active.Count);
        }

        [Test]
        public void Dot_KillingTarget_ClearsSafely()
        {
            var t = new FakeTarget { Hp = 1f };
            var set = new StatusEffectSet(t);
            t.OnDeath = set.Clear;   // luồng chết thật gọi ClearAll ngay trong lúc đang tick
            var dot = Make<DamageOverTimeEffectSO>(StackPolicy.KeepStrongest);
            var slow = Make<SlowEffectSO>(StackPolicy.KeepStrongest);
            set.Apply(slow, 5f, 0.5f);
            set.Apply(dot, 5f, 10f);
            Assert.DoesNotThrow(() => set.Tick(0.6f));
            Assert.IsTrue(t.Dead);
            Assert.AreEqual(0, set.Active.Count);
            Assert.AreEqual(4f, t.Sheet.Get(StatType.MoveSpeed), 1e-4, "Clear phải gỡ modifier");
        }

        [Test]
        public void CrowdControl_Flag()
        {
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var stun = Make<StunEffectSO>(StackPolicy.KeepStrongest);
            Assert.IsFalse(set.IsCrowdControlled);
            set.Apply(stun, 1f, 1f);
            Assert.IsTrue(set.IsCrowdControlled);
            set.Tick(1.01f);
            Assert.IsFalse(set.IsCrowdControlled);
        }

        [Test]
        public void StatModifierEffect_BuffScalesWithMagnitude()
        {
            var t = new FakeTarget();
            var set = new StatusEffectSet(t);
            var buff = Make<StatModifierEffectSO>(StackPolicy.Refresh);
            buff.modifiers.Add(new StatModifier(StatType.AttackDamage, 0f, 0.25f));
            set.Apply(buff, 5f, 2f);
            Assert.AreEqual(1.5f, t.Sheet.Get(StatType.AttackDamage), 1e-4);
            set.Remove(buff);
            Assert.AreEqual(1f, t.Sheet.Get(StatType.AttackDamage), 1e-4);
        }

        [Test]
        public void DeadTarget_IgnoresNewEffects()
        {
            var t = new FakeTarget { Dead = true };
            var set = new StatusEffectSet(t);
            Assert.IsNull(set.Apply(Make<StunEffectSO>(StackPolicy.KeepStrongest), 1f, 1f));
        }
    }
}
