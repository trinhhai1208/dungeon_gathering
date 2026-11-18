using System.Collections.Generic;
using DungeonGathering.Enemies;
using DungeonGathering.Play;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Bảng sinh quái theo định nghĩa + prefab nền dùng chung.</summary>
    public class DefinitionTests
    {
        readonly List<Object> created = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) if (o != null) Object.DestroyImmediate(o);
            created.Clear();
        }

        Enemy MakeEnemy(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go.AddComponent<Enemy>();
        }

        MonsterDefinitionSO Def(int cost)
        {
            var d = ScriptableObject.CreateInstance<MonsterDefinitionSO>();
            d.spawnCost = cost;
            created.Add(d);
            return d;
        }

        [Test]
        public void Entry_UsesSharedBasePrefab()
        {
            var basePrefab = MakeEnemy("Enemy_Base");
            var def = Def(3);
            var e = new EnemySpawnEntry { monster = def };
            var u = e.Resolve(basePrefab);
            Assert.AreSame(basePrefab, u.Prefab);
            Assert.AreSame(def, u.Definition);
            Assert.AreEqual(3, e.Cost);
            Assert.IsFalse(u.Elite);
            Assert.IsTrue(u.AsElite(true).Elite);
        }

        [Test]
        public void Entry_NoBasePrefab_IsNotSpawnable()
        {
            var e = new EnemySpawnEntry { monster = Def(1) };
            Assert.IsTrue(e.IsValid);
            Assert.IsFalse(e.Resolve(null).IsValid);
        }

        [Test]
        public void Entry_Empty_IsInvalid()
        {
            var e = new EnemySpawnEntry();
            Assert.IsFalse(e.IsValid);
            Assert.AreEqual(1, e.Cost);
        }

        [Test]
        public void Monster_NoProfile_UsesDefaultStats()
        {
            var d = Def(1);
            var s = d.ResolveStats();
            Assert.Greater(s.maxHp, 0f);
            Assert.AreEqual(1f, s.attackDamage, 1e-4, "AttackDamage là hệ số nhân sát thương đòn — mặc định 1");
        }
    }
}
