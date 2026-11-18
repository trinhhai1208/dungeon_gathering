using System;
using System.Collections.Generic;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Quản lý túi đồ run (RunBag) và liên kết vật phẩm runtime trên Player (SYS-01-INV §4.2).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInventory : MonoBehaviour
    {
        readonly List<ItemData> runBagItems = new();

        public IReadOnlyList<ItemData> RunBag => runBagItems;
        public event Action<ItemData> ItemAddedToBag;

        public void Init(IPlayerContext ctx, PlayerStats stats, HeroDefinitionSO character, RunStats carried)
        {
            runBagItems.Clear();
            var session = Services.Session;
            var catalog = ItemCatalog.Current;
            if (session != null && catalog != null)
            {
                foreach (var id in session.runBag)
                {
                    var item = catalog.GetItem(id);
                    if (item != null) runBagItems.Add(item);
                }
            }
        }

        public void AddToRunBag(ItemData item)
        {
            if (item == null) return;
            runBagItems.Add(item);

            // Ủy quyền vào SessionData làm nguồn ghi duy nhất
            Services.Session?.AddToBag(item.id);

            // Bắn event runtime nội bộ
            ItemAddedToBag?.Invoke(item);
        }
    }
}
