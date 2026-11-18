using System.Collections.Generic;
using DungeonGathering.Catalog;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 5: SkillRegistry tra theo SkillId / theo SO, ghi đè cooldown + mana, chọn đúng loại skill.</summary>
    public class SkillLibraryTests
    {
        /// <summary>Skill hero giả để test (ActiveSkillSO là abstract).</summary>
        public sealed class FakeHeroSkill : ActiveSkillSO { public override void Activate(IPlayerContext ctx) { } }
        public sealed class FakeOtherSkill : SkillBaseSO { }

        readonly List<Object> created = new();
        T Make<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); created.Add(o); return o; }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
            SkillRegistry.Register(null);
        }

        SkillLibrarySO Lib(params SkillEntry[] entries)
        {
            var lib = Make<SkillLibrarySO>();
            lib.skills.AddRange(entries);
            SkillRegistry.Register(lib);
            return lib;
        }

        [Test]
        public void Get_ByEnumValue()
        {
            var hero = Make<FakeHeroSkill>();
            Lib(new SkillEntry { key = "shadow-step", value = 13, skill = hero });
            Assert.AreSame(hero, SkillRegistry.Get<ActiveSkillSO>((SkillId)13));
            Assert.IsNull(SkillRegistry.Get<ActiveSkillSO>((SkillId)999));
            Assert.IsNull(SkillRegistry.Get<ActiveSkillSO>(SkillId.None));
        }

        [Test]
        public void Get_WrongKind_ReturnsNull()
        {
            var other = Make<FakeOtherSkill>();
            Lib(new SkillEntry { key = "atk-bite", value = 18, skill = other });
            Assert.IsNull(SkillRegistry.Get<ActiveSkillSO>((SkillId)18), "Chọn đòn quái cho hero → null, không lỗi ép kiểu");
            Assert.IsNotNull(SkillRegistry.Get<SkillBaseSO>((SkillId)18));
        }

        [Test]
        public void Cooldown_AndMana_UseEntryOverride()
        {
            var hero = Make<FakeHeroSkill>();
            hero.cooldown = 12f; hero.manaCost = 30f;
            Assert.AreEqual(12f, SkillRegistry.Cooldown(hero), "Chưa có trong thư viện → giá trị của SO");
            Lib(new SkillEntry { key = "x", value = 1, skill = hero, cooldown = new Overridable<float>(6f) });
            Assert.AreEqual(6f, SkillRegistry.Cooldown(hero));
            Assert.AreEqual(30f, SkillRegistry.ManaCost(hero), "Không ghi đè mana → giá trị của SO");
            Assert.AreEqual(12f, hero.cooldown, "Không ghi vào SO");
        }

        [Test]
        public void GetAll_SkipsMissing_KeepsOrder()
        {
            var a = Make<FakeOtherSkill>();
            var b = Make<FakeOtherSkill>();
            Lib(new SkillEntry { key = "a", value = 1, skill = a }, new SkillEntry { key = "b", value = 2, skill = b });
            var list = SkillRegistry.GetAll<SkillBaseSO>(new[] { (SkillId)2, (SkillId)77, (SkillId)1 });
            CollectionAssert.AreEqual(new[] { b, a }, list);
        }

        [Test]
        public void EntryWithoutValue_IsIgnored()
        {
            var a = Make<FakeOtherSkill>();
            Lib(new SkillEntry { key = "new-skill", value = 0, skill = a });
            Assert.IsNull(SkillRegistry.Entry(a), "Entry chưa được cấp số (chưa chạy Generate SkillId) không được dùng");
        }
    }
}
