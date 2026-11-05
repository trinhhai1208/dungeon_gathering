using DungeonGathering.Catalog;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Hạ quái hồi mana.</summary>
    [CreateAssetMenu(menuName = "Dungeon Gathering/Passives/Weapon Mana On Kill", fileName = "WP_ManaOnKill")]
    public class ManaOnKillPassive : HookWeaponPassive
    {
        public float mana = 3f;
        protected override void OnHit(IPlayerContext player, PassiveState state, in HitReport hit)
        {
            if (hit.Killed) player.RestoreMana(mana);
        }
    }
}
