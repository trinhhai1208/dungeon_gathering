using System;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Bảng rơi đồ (quái, rương). Prefab hạt loot do LootSpawner (Play) giữ; bảng chỉ chứa số liệu.</summary>
    [Serializable]
    public class LootTable
    {
        [Min(0)] public int minGold = 1;
        [Min(0)] public int maxGold = 3;
        [Range(0, 1)] public float manaOrbChance = 0.3f;
        [Min(0)] public int manaOrbs = 1;
        [Range(0, 1)] public float healthPotionChance = 0.02f;
        [Tooltip("Xác suất rơi vũ khí/trang bị (rarity theo tầng)")]
        [Range(0, 1)] public float itemChance;
        [Tooltip("Cho phép vật phẩm rơi là vũ khí.")]
        public bool weapons = true;
        [Tooltip("Cho phép vật phẩm rơi là trang bị.")]
        public bool equipment = true;

        public int RollGold(System.Random rng) => maxGold <= minGold ? minGold : rng.Next(minGold, maxGold + 1);
    }
}
