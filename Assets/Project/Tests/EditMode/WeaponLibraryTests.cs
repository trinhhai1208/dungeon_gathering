using System.Collections.Generic;
using DungeonGathering.Catalog;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 4: resolve vũ khí SO gốc ← đạn ← entry, thứ tự ưu tiên, dữ liệu cũ, không ghi vào SO.</summary>
    public class WeaponLibraryTests
    {
        readonly List<Object> created = new();
        T Make<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); created.Add(o); return o; }
        GameObject Go(string n) { var g = new GameObject(n); created.Add(g); return g; }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
            WeaponLibrary.Register(null, null, null);
        }

        [Test]
        public void Sword_DefaultsFromBase_OverridesFromEntry()
        {
            var b = Make<SwordBaseSO>();
            b.damage = 15f; b.fireRate = 1.5f; b.swingArc = 110f; b.spriteForwardAngle = 135f;
            var e = new SwordEntry { id = "sword-x" };
            e.damage = new Overridable<float>(30f);
            var s = WeaponLibrary.Build(null, b, e);
            Assert.AreEqual(30f, s.damage);
            Assert.AreEqual(1.5f, s.fireRate);
            Assert.AreEqual(110f, s.swingArc);
            Assert.AreEqual(135f, s.GetForwardAngle(0f, 0f));
            Assert.AreEqual(WeaponType.Melee, s.weaponType);
            Assert.AreEqual(15f, b.damage, "Resolve không ghi vào SO gốc");
        }

        [Test]
        public void Ranged_ProjectileDefaults_ThenEntryWins()
        {
            var proj = Make<ProjectileLibrarySO>();
            var projVfx = Go("projHit");
            var entryVfx = Go("entryHit");
            proj.projectiles.Add(new ProjectileEntry { id = "plasma", prefab = Go("plasma"), speed = 20f, pierce = 2, hitVfx = projVfx });
            WeaponLibrary.Register(null, null, proj);

            var b = Make<RangedBaseSO>();
            b.burstCount = 1;
            var e = new RangedEntry { id = "gun-x", projectileId = "plasma", hitVfx = entryVfx };
            var s = WeaponLibrary.Build(null, b, e);
            Assert.AreEqual(20f, s.projectileSpeed, "Tốc độ lấy từ đạn");
            Assert.AreEqual(2, s.pierce);
            Assert.AreSame(entryVfx, s.hitImpactVfxPrefab, "VFX của entry thắng VFX của đạn");

            e.projectileSpeed = new Overridable<float>(9f);
            e.burstCount = new Overridable<int>(3);
            s = WeaponLibrary.Build(null, b, e);
            Assert.AreEqual(9f, s.projectileSpeed, "Ghi đè của entry thắng đạn");
            Assert.AreEqual(3, s.burstCount);
        }

        [Test]
        public void OnHitStatus_BaseAndEntryAreCombined()
        {
            var stun = Make<StunEffectSO>();
            var slow = Make<SlowEffectSO>();
            var b = Make<SwordBaseSO>();
            b.onHitStatus.Add(new StatusEffectRef(slow));
            var e = new SwordEntry { id = "s" };
            e.statusEffects.Add(new StatusEffectRef(stun, duration: 1.5f));
            var s = WeaponLibrary.Build(null, b, e);
            Assert.AreEqual(2, s.onHitStatus.Count);
            Assert.AreEqual(1.5f, s.onHitStatus[1].Duration, 1e-4);
        }

        [Test]
        public void Hitbox_OverrideReplacesAuto()
        {
            var b = Make<SwordBaseSO>();
            var e = new SwordEntry { id = "spear" };
            Assert.AreEqual(HitboxShape.Auto, WeaponLibrary.Build(null, b, e).hitbox.shape);
            e.hitbox = new Overridable<MeleeHitbox>(new MeleeHitbox { shape = HitboxShape.Box, size = new Vector2(2.2f, 0.6f) });
            var s = WeaponLibrary.Build(null, b, e);
            Assert.AreEqual(HitboxShape.Box, s.hitbox.shape);
            Assert.AreEqual(2.2f, s.hitbox.size.x, 1e-4);
        }

        [Test]
        public void WeaponWithoutEntry_GetsMinimalSpec()
        {
            var w = Make<WeaponData>();
            w.id = "missing-entry";
            WeaponLibrary.Register(Make<SwordLibrarySO>(), Make<RangedLibrarySO>(), Make<ProjectileLibrarySO>());
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("chưa có entry"));
            var s = w.Spec;
            Assert.IsFalse(s.fromLibrary);
            Assert.AreSame(w, s.Item);
            Assert.Greater(s.damage, 0f);
        }

        [Test]
        public void Library_EntryMatchedById()
        {
            var w = Make<WeaponData>();
            w.id = "sword-y";
            var lib = Make<SwordLibrarySO>();
            lib.baseConfig = Make<SwordBaseSO>();
            lib.baseConfig.damage = 15f;
            lib.swords.Add(new SwordEntry { id = "sword-y" });
            WeaponLibrary.Register(lib, null, null);
            var s = w.Spec;
            Assert.IsTrue(s.fromLibrary);
            Assert.AreEqual(15f, s.damage, "Có entry → chỉ số từ thư viện, không từ field cũ");
            Assert.AreSame(w, s.Item);
        }

        [Test]
        public void Dps_CountsProjectilesAndBursts()
        {
            var s = new WeaponSpec { damage = 10f, fireRate = 2f, projectileCount = 3, burstCount = 2 };
            Assert.AreEqual(120f, s.Dps, 1e-4);
        }
    }
}
