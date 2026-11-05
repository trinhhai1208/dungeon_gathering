using DungeonGathering.Catalog;
using DungeonGathering.Combat;
using DungeonGathering.Enemies;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Prefab dùng chung của module Play (cùng group Mod_Play → tham chiếu trực tiếp an toàn).
    /// Gán vào LevelDirector trong Play.unity.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayPrefabSet", menuName = "Dungeon Gathering/Play Prefab Set")]
    public class PlayPrefabSet : ScriptableObject
    {
        [Header("Player")]
        public PlayerController player;

        [Header("Combat FX")]
        public DamagePopup damagePopup;
        public GameObject defaultHitVfx;
        public SpawnWarningMarker spawnMarker;

        [Header("Loot")]
        public LootDrop goldCoin;
        public LootDrop blueCoin;
        public LootDrop manaOrb;
        public LootDrop healthPotion;
        public ItemPickup itemPickup;

        [Header("Phòng")]
        public RoomDoor door;
        public Chest chest;
        public ShopStand shopStand;
        public Portal portal;

        [Header("Dev / Editor (khi chưa có Addressables)")]
        public ItemCatalog fallbackCatalog;
        public CharacterData fallbackCharacter;
        public DungeonConfig[] fallbackConfigs;

        [Header("Prewarm")]
        public int prewarmCoins = 20;
        public int prewarmPopups = 12;
    }
}
