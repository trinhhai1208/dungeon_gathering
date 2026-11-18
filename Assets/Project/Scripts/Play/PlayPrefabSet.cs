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
        [Tooltip("Prefab người chơi sinh khi vào màn. Trống → lỗi, không sinh người chơi")] public PlayerController player;

        [Header("Combat FX")]
        [Tooltip("Số sát thương bay lên khi trúng đòn (lấy từ pool, tạo sẵn prewarmPopups cái)")] public DamagePopup damagePopup;
        [Tooltip("VFX trúng dự phòng khi đạn / tia không có hitVfx riêng. Trống → không phát")] public GameObject defaultHitVfx;
        [Tooltip("Dấu cảnh báo tại chỗ quái sắp sinh ra (pool, tạo sẵn 6). Trống → không hiện dấu")] public SpawnWarningMarker spawnMarker;

        [Header("Loot")]
        [Tooltip("Prefab đồng vàng rơi ra; tổng vàng chia cho nhiều đồng. Trống → không rơi vàng")] public LootDrop goldCoin;
        [Tooltip("Prefab cầu mana rơi ra, mỗi cầu hồi 4 mana")] public LootDrop manaOrb;
        [Tooltip("Prefab bình máu rơi ra, mỗi bình hồi 2 HP")] public LootDrop healthPotion;
        [Tooltip("Vật phẩm nằm trên đất (vũ khí / trang bị rơi ra). Trống → không rơi vật phẩm")] public ItemPickup itemPickup;

        [Header("Phòng")]
        [Tooltip("Cửa đặt ở mỗi lối nối đang dùng giữa các phòng khi dựng dungeon")] public RoomDoor door;
        [Tooltip("Rương ở phòng Treasure và rương có thể rơi khi dọn xong phòng Combat / Elite. Trống → không có rương")] public Chest chest;
        [Tooltip("Quầy hàng đặt ở phòng Shop. Trống → phòng Shop không có quầy")] public ShopStand shopStand;
        [Tooltip("Cổng sang tầng kế tiếp, hiện khi hạ boss. Trống → hạ boss là kết thúc màn ngay")] public Portal portal;

        [Header("Quái")]
        [Tooltip("Prefab nền dùng chung cho mọi quái thường — định nghĩa (MonsterDefinitionSO) áp hình / chỉ số lúc sinh")]
        public Enemy monsterBase;

        [Header("Cấu hình chỉ số")]
        [Tooltip("Hệ số theo bậc quái Normal / Elite / Boss")]
        public DungeonGathering.Catalog.MonsterTierConfigSO monsterTiers;

        [Header("Rơi đồ")]
        [Tooltip("Rơi đồ theo bậc quái + level, trọng số bậc D…S")]
        public DungeonGathering.Catalog.LootConfigSO lootConfig;

        [Header("Prewarm")]
        [Tooltip("Số đồng vàng tạo sẵn trong pool lúc vào màn (tránh giật khi rơi nhiều)")] public int prewarmCoins = 20;
        [Tooltip("Số popup sát thương tạo sẵn trong pool lúc vào màn")] public int prewarmPopups = 12;
    }
}
