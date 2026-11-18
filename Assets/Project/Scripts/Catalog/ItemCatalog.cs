using System.Collections.Generic;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>
    /// Danh mục id → ItemData/HeroDefinitionSO (address "data/catalog", group Catalog_Items).
    /// Item và catalog nằm chung một group (Pack Together) nên tham chiếu trực tiếp không tốn thêm tải;
    /// màn Lobby/Play tải catalog một lần qua <see cref="LoadAsync"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "Dungeon Gathering/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [Tooltip("Mọi vật phẩm (vũ khí, trang bị) tra theo id cho kho, save, shop, rơi đồ. Trùng id → cảnh báo, giữ cái đầu")] [SerializeField] List<ItemData> items = new();
        [Tooltip("Mọi hero tra theo characterId. Trùng id → cảnh báo, giữ cái đầu")] [SerializeField] List<HeroDefinitionSO> characters = new();

        [Header("Thư viện vũ khí (hệ mới)")]
        [Tooltip("Kiếm / giáo / dao: entry cùng id chứa mọi dữ liệu (chỉ số, hình, tên, bậc, icon, giá); WeaponData chỉ là id")]
        [SerializeField] SwordLibrarySO swordLibrary;
        [Tooltip("Súng / cung / trượng: entry cùng id chứa mọi dữ liệu (chỉ số, hình, tên, bậc, icon, giá); WeaponData chỉ là id")] [SerializeField] RangedLibrarySO rangedLibrary;
        [Tooltip("Các loại đạn (cả kiếm khí); vũ khí tham chiếu đạn theo id")] [SerializeField] ProjectileLibrarySO projectileLibrary;

        [Header("Campaign (thứ tự level)")]
        [Tooltip("Thứ tự level: số tầng tối đa + address config từng tầng. Trống → address mặc định theo theme / tầng")] [SerializeField] CampaignSO campaign;
        public CampaignSO Campaign => campaign;

        [Header("Thư viện skill")]
        [Tooltip("Thư viện skill, đăng ký vào SkillRegistry; hero / quái / boss chọn skill bằng enum SkillId")] [SerializeField] SkillLibrarySO skillLibrary;
        public SkillLibrarySO SkillLibrary => skillLibrary;

        public SwordLibrarySO SwordLibrary => swordLibrary;
        public RangedLibrarySO RangedLibrary => rangedLibrary;
        public ProjectileLibrarySO ProjectileLibrary => projectileLibrary;

        Dictionary<string, ItemData> itemById;
        Dictionary<string, HeroDefinitionSO> charById;

        public IReadOnlyList<ItemData> Items => items;
        public IReadOnlyList<HeroDefinitionSO> Characters => characters;

        static ItemCatalog current;
        /// <summary>Catalog đã tải gần nhất (null trước khi tải).</summary>
        public static ItemCatalog Current => current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => current = null;

        /// <summary>Tải catalog qua scope Global (chỉ nạp 1 lần, giữ sống suốt phiên) và đăng ký làm Current.</summary>
        public static async Awaitable<ItemCatalog> LoadAsync()
        {
            if (current != null) return current;
            var cat = await Services.Assets.Global.Load<ItemCatalog>(AddressKeys.DataCatalog);
            cat.Register();
            return cat;
        }

        /// <summary>Đăng ký thủ công (scene dev có tham chiếu trực tiếp).</summary>
        public void Register()
        {
            BuildIndex();
            current = this;
            WeaponLibrary.Register(swordLibrary, rangedLibrary, projectileLibrary);
            SkillRegistry.Register(skillLibrary);
            var weaponIds = new HashSet<string>();
            foreach (var it in items)
                if (it is WeaponData && !string.IsNullOrEmpty(it.id))
                    weaponIds.Add(it.id);
            ItemIdClassifier.WeaponPredicate = weaponIds.Contains;
            if (EditorUnlockAll.Active) EditorUnlockAll.Granter = GrantAll;
        }

        /// <summary>Chế độ test "mở full" (chỉ Editor): mở mọi hero, sở hữu mọi vũ khí / trang bị trong catalog.</summary>
        void GrantAll(SaveData data)
        {
            foreach (var c in characters)
                if (c != null && !string.IsNullOrEmpty(c.characterId) && !data.unlockedCharacters.Contains(c.characterId))
                    data.unlockedCharacters.Add(c.characterId);
            foreach (var it in items)
            {
                if (it == null || string.IsNullOrEmpty(it.id)) continue;
                var list = it is WeaponData ? data.ownedWeapons : data.ownedEquipment;
                if (!list.Contains(it.id)) list.Add(it.id);
                data.Discover(it.id);
            }
        }

        void BuildIndex()
        {
            itemById = new Dictionary<string, ItemData>();
            foreach (var it in items)
            {
                if (it == null || string.IsNullOrEmpty(it.id)) continue;
                if (!itemById.TryAdd(it.id, it)) Debug.LogWarning($"[Catalog] Trùng id item '{it.id}'.");
            }
            charById = new Dictionary<string, HeroDefinitionSO>();
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

        public HeroDefinitionSO GetCharacter(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (charById == null) BuildIndex();
            return charById.TryGetValue(id, out var c) ? c : null;
        }

        public IEnumerable<T> All<T>() where T : ItemData
        {
            foreach (var it in items) if (it is T t) yield return t;
        }


        /// <summary>
        /// Chọn ngẫu nhiên một vật phẩm đúng bậc; bậc đó không có món nào thì lùi dần xuống bậc thấp hơn, rồi mới thử bậc
        /// cao hơn. Null khi catalog không có vật phẩm phù hợp.
        /// </summary>
        public ItemData PickItem(Rarity rank, System.Random rng, bool weapons = true, bool equipment = true)
        {
            for (int r = (int)rank; r <= (int)Rarity.D; r++)
                if (PickExact(r, rng, weapons, equipment) is { } it) return it;
            for (int r = (int)rank - 1; r >= (int)Rarity.S; r--)
                if (PickExact(r, rng, weapons, equipment) is { } it) return it;
            return null;
        }

        ItemData PickExact(int rarity, System.Random rng, bool weapons, bool equipment)
        {
            int count = 0;
            foreach (var it in items) if (Matches(it, rarity, weapons, equipment)) count++;
            if (count == 0) return null;
            int pick = rng.Next(count);
            foreach (var it in items)
                if (Matches(it, rarity, weapons, equipment) && pick-- == 0) return it;
            return null;
        }


        static bool Matches(ItemData it, int rarity, bool weapons, bool equipment)
            => it != null && (int)it.Rank == rarity && ((weapons && it.IsWeapon) || (equipment && !it.IsWeapon));
    }

}
