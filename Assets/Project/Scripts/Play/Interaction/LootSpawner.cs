using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Tung loot quanh một điểm (ENTITIES §8): vàng, mana, bình máu, vũ khí/trang bị (rarity theo tầng).
    /// Mọi hạt lấy từ PoolManager theo scope của Play.
    /// </summary>
    public sealed class LootSpawner
    {
        public const int ManaPerOrb = 4;
        public const int HealthPerPotion = 2;
        const int MaxCoinObjects = 8;

        readonly PlayPrefabSet prefabs;
        readonly ModuleScope scope;
        readonly ItemCatalog catalog;
        readonly System.Random rng;
        readonly int floor;

        public LootSpawner(PlayPrefabSet prefabs, ModuleScope scope, ItemCatalog catalog, System.Random rng, int floor)
        {
            this.prefabs = prefabs;
            this.scope = scope;
            this.catalog = catalog;
            this.rng = rng;
            this.floor = floor;
        }

        public void Drop(LootTable table, Vector2 at, float goldMultiplier = 1f)
        {
            if (table == null || prefabs == null) return;
            int gold = Mathf.RoundToInt(table.RollGold(rng) * goldMultiplier);
            DropGold(gold, at);
            if (rng.NextDouble() < table.manaOrbChance)
                for (int i = 0; i < table.manaOrbs; i++) Spawn(prefabs.manaOrb, at, ManaPerOrb);
            if (rng.NextDouble() < table.healthPotionChance) Spawn(prefabs.healthPotion, at, HealthPerPotion);
            if (table.itemChance > 0f && rng.NextDouble() < table.itemChance) DropItem(at, table.weapons, table.equipment);
        }

        public void DropGold(int total, Vector2 at)
        {
            if (total <= 0 || prefabs.goldCoin == null) return;
            int objects = Mathf.Min(total, MaxCoinObjects);
            int baseValue = total / objects, rest = total % objects;
            for (int i = 0; i < objects; i++) Spawn(prefabs.goldCoin, at, baseValue + (i < rest ? 1 : 0));
        }

        public ItemData DropItem(Vector2 at, bool weapons = true, bool equipment = true)
        {
            if (catalog == null || prefabs.itemPickup == null) return null;
            var item = catalog.RollItem(floor, rng, weapons, equipment);
            if (item == null) return null;
            var go = Services.Pool.Get(prefabs.itemPickup.gameObject, scope, at, Quaternion.identity);
            if (go != null) go.GetComponent<ItemPickup>().Init(item);
            return item;
        }

        void Spawn(LootDrop prefab, Vector2 at, int amount)
        {
            if (prefab == null) return;
            var go = Services.Pool.Get(prefab.gameObject, scope, at, Quaternion.identity);
            if (go == null) return;
            var drop = go.GetComponent<LootDrop>();
            drop.SetAmount(amount);
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = 0.4f + (float)rng.NextDouble() * 0.9f;
            drop.Scatter(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }
    }
}
