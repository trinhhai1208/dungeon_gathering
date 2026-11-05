using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Base chung cho passive vũ khí và trang bị.</summary>
    public abstract class ItemPassiveSO : ScriptableObject
    {
        [TextArea] public string description;
        public string descKey;
        public abstract void OnEquip(IPlayerContext player);
        public abstract void OnUnequip(IPlayerContext player);
    }
}
