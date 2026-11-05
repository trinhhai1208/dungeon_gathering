using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Danh mục id → ItemData/CharacterData (address "data/catalog", group Catalog_Items).
    /// Item và catalog nằm chung một group (Pack Together) nên tham chiếu trực tiếp không tốn thêm tải;
    /// màn Lobby/Play tải catalog một lần qua <see cref="LoadAsync"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Dungeon Gathering/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [SerializeField] List<ItemData> items = new();
        [SerializeField] List<CharacterData> characters = new();
        [Tooltip("Tỉ lệ rơi rarity theo tầng (index 0 = tầng 1)")]
        [SerializeField] List<RarityWeights> dropRatesByFloor = new()
        {
            new RarityWeights { F = 60, C = 35, B = 5 },
            new RarityWeights { C = 50, B = 35, A = 15 },
            new RarityWeights { B = 40, A = 45, S = 15 },
        };

        Dictionary<string, ItemData> itemById;
        Dictionary<string, CharacterData> charById;

        public IReadOnlyList<ItemData> Items => items;
        public IReadOnlyList<CharacterData> Characters => characters;

        static ItemCatalog current;
        /// <summary>Catalog đã tải gần nhất (null trước khi tải).</summary>
        public static ItemCatalog Current => current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => current = null;

        /// <summary>Tải catalog qua scope của module và đăng ký làm Current.</summary>
        public static async Awaitable<ItemCatalog> LoadAsync(ModuleScope scope)
        {
            var cat = await scope.Load<ItemCatalog>(AddressKeys.DataCatalog);
            cat.Register();
            return cat;
        }

        /// <summary>Đăng ký thủ công (scene dev có tham chiếu trực tiếp).</summary>
        public void Register()
        {
            BuildIndex();
            current = this;
            ItemIdClassifier.WeaponPredicate = id => GetItem(id) is WeaponData;
        }

        void BuildIndex()
        {
            itemById = new Dictionary<string, ItemData>();
            foreach (var it in items)
            {
                if (it == null || string.IsNullOrEmpty(it.id)) continue;
                if (!itemById.TryAdd(it.id, it)) Debug.LogWarning($"[Catalog] Trùng id item '{it.id}'.");
            }
            charById = new Dictionary<string, CharacterData>();
            foreach (var c in characters)
            {
                if (c == null || string.IsNullOrEmpty(c.characterId)) continue;
                if (!charById.TryAdd(c.characterId, c)) Debug.LogWarning($"[Catalog] Trùng id nhân vật '{c.characterId}'.");
            }
        }

        public ItemData GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (itemById == null) BuildIndex();
            return itemById.TryGetValue(id, out var it) ? it : null;
        }

        public T Get<T>(string id) where T : ItemData => GetItem(id) as T;

        public CharacterData GetCharacter(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (charById == null) BuildIndex();
            return charById.TryGetValue(id, out var c) ? c : null;
        }

        public IEnumerable<T> All<T>() where T : ItemData
        {
            foreach (var it in items) if (it is T t) yield return t;
        }

        public Rarity RollRarity(int floor, System.Random rng)
        {
            if (dropRatesByFloor.Count == 0) return Rarity.F;
            var w = dropRatesByFloor[Mathf.Clamp(floor - 1, 0, dropRatesByFloor.Count - 1)];
            return w.Roll(rng);
        }

        /// <summary>Chọn ngẫu nhiên một item theo rarity của tầng (lùi dần rarity nếu không có món nào).</summary>
        public ItemData RollItem(int floor, System.Random rng, bool weapons = true, bool equipment = true)
        {
            var rarity = RollRarity(floor, rng);
            for (int r = (int)rarity; r <= (int)Rarity.F; r++)
            {
                // Hai lượt (đếm rồi chọn) thay vì dựng list tạm mỗi lần roll.
                int count = 0;
                foreach (var it in items) if (Matches(it, r, weapons, equipment)) count++;
                if (count == 0) continue;
                int pick = rng.Next(count);
                foreach (var it in items)
                    if (Matches(it, r, weapons, equipment) && pick-- == 0) return it;
            }
            return null;
        }

        static bool Matches(ItemData it, int rarity, bool weapons, bool equipment)
            => it != null && (int)it.rarity == rarity && ((weapons && it.IsWeapon) || (equipment && !it.IsWeapon));
    }

    [System.Serializable]
    public struct RarityWeights
    {
        public int S, A, B, C, F;

        public Rarity Roll(System.Random rng)
        {
            int total = S + A + B + C + F;
            if (total <= 0) return Rarity.F;
            int r = rng.Next(total);
            if ((r -= S) < 0) return Rarity.S;
            if ((r -= A) < 0) return Rarity.A;
            if ((r -= B) < 0) return Rarity.B;
            if ((r -= C) < 0) return Rarity.C;
            return Rarity.F;
        }
    }
}
