using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Enemies;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Sinh loot quanh một điểm: vàng, mana, bình máu, vũ khí / trang bị. Tính toán (lượng, xác suất, bậc D…S) do
    /// <see cref="DropResolver"/> + <see cref="LootConfigSO"/>; lớp này chỉ biến kết quả thành object từ pool của Play.
    /// </summary>
    public sealed class LootSpawner
    {
        public const int ManaPerOrb = 4;
        public const int HealthPerPotion = 2;
        const int MaxCoinObjects = 8;

        readonly PlayPrefabSet prefabs;
        readonly ModuleScope scope;
        readonly ItemCatalog catalog;
        readonly LootConfigSO config;
        readonly System.Random rng;
        readonly int level;
        readonly float lootLuck;

        public LootSpawner(PlayPrefabSet prefabs, ModuleScope scope, ItemCatalog catalog, LootConfigSO config,
                           System.Random rng, int level, float lootLuck = 0f)
        {
            this.prefabs = prefabs;
            this.scope = scope;
            this.catalog = catalog;
            this.config = config;
            this.rng = rng;
            this.level = level;
            this.lootLuck = lootLuck;
        }

        /// <summary>Quái chết: rơi đồ theo bậc quái + level (hoặc bảng riêng nếu định nghĩa bật customLoot).</summary>
        public void DropFromMonster(MonsterDefinitionSO def, MonsterTier tier, Vector2 at)
        {
            if (prefabs == null) return;
            if (def != null && def.customLoot)
            {
                DropTable(def.lootTable, at, config != null ? config.Get(tier).rankShift : 0f);
                return;
            }
            if (config == null)
            {
                if (def != null) DropTable(def.lootTable, at, 0f); // chưa có LootConfig → dữ liệu cũ
                return;
            }
            var r = DropResolver.ForMonster(config, tier, level, lootLuck, rng);
            Spawn(r, at, true, true);
        }

        void Spawn(DropResult r, Vector2 at, bool weapons, bool equipment)
        {
            DropGold(r.Gold, at);
            for (int i = 0; i < r.ManaOrbs; i++) Spawn(prefabs.manaOrb, at, ManaPerOrb);
            for (int i = 0; i < r.Potions; i++) Spawn(prefabs.healthPotion, at, HealthPerPotion);
            foreach (var rank in r.ItemRanks) DropItem(at, rank, weapons, equipment);
        }

        /// <summary>Bảng rơi riêng của từng quái (LootTable cũ). Bậc vật phẩm vẫn theo level + dời bậc.</summary>
        void DropTable(LootTable table, Vector2 at, float rankShift)
        {
            if (table == null) return;
            float goldMul = config != null ? 1f + Mathf.Max(0, level - 1) * config.goldPerLevel : 1f;
            DropGold(Mathf.RoundToInt(table.RollGold(rng) * goldMul), at);
            if (rng.NextDouble() < table.manaOrbChance)
                for (int i = 0; i < table.manaOrbs; i++) Spawn(prefabs.manaOrb, at, ManaPerOrb);
            if (rng.NextDouble() < table.healthPotionChance) Spawn(prefabs.healthPotion, at, HealthPerPotion);
            if (table.itemChance > 0f && rng.NextDouble() < table.itemChance)
                DropItem(at, RollRank(rankShift), table.weapons, table.equipment);
        }

        public void DropGold(int total, Vector2 at)
        {
            if (total <= 0 || prefabs.goldCoin == null) return;
            int objects = Mathf.Min(total, MaxCoinObjects);
            int baseValue = total / objects, rest = total % objects;
            for (int i = 0; i < objects; i++) Spawn(prefabs.goldCoin, at, baseValue + (i < rest ? 1 : 0));
        }

        /// <summary>Bậc một món theo level + dời bậc (rương, shop, bảng riêng).</summary>
        public Rarity RollRank(float rankShift) =>
            config != null ? DropResolver.RollRank(config, level, rankShift + lootLuck, rng) : Rarity.D;

        /// <summary>Rương: một món theo bậc của level dời thêm chestRankShift.</summary>
        public ItemData DropChestItem(Vector2 at) => DropItem(at, RollRank(config != null ? config.chestRankShift : 0f));

        /// <summary>Một món cho quầy shop trong run.</summary>
        public ItemData RollShopItem() => catalog != null ? catalog.PickItem(RollRank(0f), rng) : null;

        public ItemData DropItem(Vector2 at, Rarity rank, bool weapons = true, bool equipment = true)
        {
            if (catalog == null || prefabs.itemPickup == null) return null;
            var item = catalog.PickItem(rank, rng, weapons, equipment);
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
