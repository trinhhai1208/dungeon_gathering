using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace DungeonGathering.Tests
{
    /// <summary>Giai đoạn 9: InventoryModel (xem không ghi save, đổi qua LoadoutRules), định danh vật phẩm, Save v2.</summary>
    public class InventoryTests
    {
        readonly List<Object> created = new();
        T Make<T>() where T : ScriptableObject { var o = ScriptableObject.CreateInstance<T>(); created.Add(o); return o; }

        ItemCatalog catalog;
        HeroDefinitionSO knight, ranger;
        WeaponData sword, bow;
        EquipmentSO helmet;

        [SetUp]
        public void Setup()
        {
            sword = Make<WeaponData>(); sword.id = "sword-iron";
            bow = Make<WeaponData>(); bow.id = "bow-wood";
            helmet = Make<EquipmentSO>(); helmet.id = "helm"; helmet.slot = EquipmentSlotType.Helmet;
            knight = Make<HeroDefinitionSO>(); knight.characterId = "knight"; knight.startingWeapon = sword;
            ranger = Make<HeroDefinitionSO>(); ranger.characterId = "ranger"; ranger.startingWeapon = bow;
            catalog = Make<ItemCatalog>();
            var f = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(ItemCatalog).GetField("items", f).SetValue(catalog, new List<ItemData> { sword, bow, helmet });
            typeof(ItemCatalog).GetField("characters", f).SetValue(catalog, new List<HeroDefinitionSO> { knight, ranger });
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var o in created) Object.DestroyImmediate(o);
            created.Clear();
        }

        [Test]
        public void Viewing_DoesNotWriteSave()
        {
            var save = new SaveData { ownedWeapons = new List<string>() };
            var inv = new InventoryModel("knight", save, catalog);
            Assert.AreEqual("sword-iron", inv.Loadout.GetWeapon(0), "Hiển thị vũ khí khởi đầu");
            Assert.IsTrue(inv.GetOwnedWeapons().Contains("sword-iron"), "Vũ khí khởi đầu tính là có");
            Assert.AreEqual(0, save.characters.Count, "Chỉ xem: không tạo tiến trình");
            Assert.AreEqual(0, save.ownedWeapons.Count, "Chỉ xem: không thêm vào kho");
        }

        [Test]
        public void Equip_CreatesProgress_AndPersists()
        {
            var save = new SaveData { ownedEquipment = new List<string> { "helm" } };
            var inv = new InventoryModel("knight", save, catalog);
            int changed = 0;
            inv.Changed += () => changed++;
            Assert.IsTrue(inv.EquipItem("helm"));
            Assert.AreEqual(1, changed);
            var prog = save.GetCharacter("knight", create: false);
            Assert.IsNotNull(prog);
            Assert.AreEqual("helm", prog.loadout.GetEquipment(EquipmentSlotType.Helmet));
            Assert.AreEqual("sword-iron", prog.loadout.GetWeapon(0), "Loadout tạm được lưu cùng");
            Assert.Contains("sword-iron", save.ownedWeapons, "Lần ghi đầu: vũ khí khởi đầu vào kho");
        }

        [Test]
        public void CannotEquip_NotOwned_OrLockedHero()
        {
            var save = new SaveData();
            var inv = new InventoryModel("knight", save, catalog);
            Assert.IsFalse(inv.EquipItem("helm"), "Chưa sở hữu");
            var locked = new InventoryModel("ranger", save, catalog);
            Assert.IsFalse(locked.IsUnlocked);
            Assert.IsFalse(locked.EquipWeapon(0, "bow-wood"), "Hero chưa mở khóa");
            Assert.IsNull(save.GetCharacter("ranger", create: false));
        }

        [Test]
        public void ItemDefinition_ReadsWeaponLibraryEntry()
        {
            var lib = Make<SwordLibrarySO>();
            lib.baseConfig = Make<SwordBaseSO>();
            var icon = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            created.Add(icon);
            lib.swords.Add(new SwordEntry { id = "sword-iron", nameKey = "item.lib_name", rank = Rarity.A, icon = icon, shopPrice = 77 });
            sword.nameKey = "item.old"; sword.rarity = Rarity.D; sword.shopPrice = 5;
            WeaponLibrary.Register(lib, null, null);
            IItemDefinition def = sword;
            Assert.AreEqual("item.lib_name", def.NameKey);
            Assert.AreEqual(Rarity.A, def.Rank);
            Assert.AreSame(icon, def.Icon);
            Assert.AreEqual(77, def.Price);
            Assert.AreEqual(ItemKind.Weapon, def.Kind);
            WeaponLibrary.Register(null, null, null);
            Assert.AreEqual("item.old", def.NameKey, "Không có entry → field cũ");
            Assert.AreEqual(ItemKind.Equipment, ((IItemDefinition)helmet).Kind);
        }

        [Test]
        public void SaveV1_IsResetButKeepsSettings()
        {
            var save = new SaveData
            {
                version = 1, coins = 999,
                unlockedCharacters = new List<string> { "knight", "ranger" },
                ownedWeapons = new List<string> { "iron", "gun-pistol" },
                settings = new SettingsData { bgmVolume = 3, language = "vi" },
            };
            save.Upgrade();
            Assert.AreEqual(SaveData.CurrentVersion, save.version);
            Assert.AreEqual(0, save.coins);
            CollectionAssert.AreEqual(new[] { "knight" }, save.unlockedCharacters);
            CollectionAssert.AreEqual(new[] { SaveData.DefaultWeaponId }, save.ownedWeapons);
            Assert.AreEqual(3, save.settings.bgmVolume, "Giữ cài đặt");
            Assert.AreEqual("vi", save.settings.language);
        }

        [Test]
        public void SaveFromNewerVersion_IsNotReset()
        {
            var save = new SaveData { version = SaveData.CurrentVersion + 1, coins = 50 };
            save.Upgrade();
            Assert.IsTrue(save.FromNewerVersion);
            Assert.AreEqual(50, save.coins);
        }
    }
}
