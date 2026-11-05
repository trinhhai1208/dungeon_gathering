using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Catalog
{
    /// <summary>Sự kiện chiến đấu của riêng Player — passive đăng ký trong OnEquip, hủy trong OnUnequip.</summary>
    public sealed class PlayerCombatHooks
    {
        public event Action<HitReport> HitDealt;
        public event Action<HitReport> Killed;
        public event Action<float> DamageTaken;
        public event Action ArmorBroken;
        public event Action<WeaponData> Attacked;

        public void RaiseHit(in HitReport r) { Safe(HitDealt, r); if (r.Killed) Safe(Killed, r); }
        public void RaiseDamageTaken(float amount) { try { DamageTaken?.Invoke(amount); } catch (Exception e) { Debug.LogException(e); } }
        public void RaiseArmorBroken() { try { ArmorBroken?.Invoke(); } catch (Exception e) { Debug.LogException(e); } }
        public void RaiseAttacked(WeaponData w) { try { Attacked?.Invoke(w); } catch (Exception e) { Debug.LogException(e); } }

        public void ClearAll() { HitDealt = null; Killed = null; DamageTaken = null; ArmorBroken = null; Attacked = null; }

        static void Safe(Action<HitReport> a, HitReport r)
        {
            if (a == null) return;
            foreach (Action<HitReport> h in a.GetInvocationList())
            {
                try { h(r); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
