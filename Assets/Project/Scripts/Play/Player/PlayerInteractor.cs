using System;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>Quét IInteractable gần nhất trong bán kính (r=1.2) mỗi 0.1s; HUD hiện prompt; bấm tương tác gọi Interact.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float radius = 1.2f;
        [SerializeField] LayerMask mask = ~0;
        [SerializeField] float scanInterval = 0.1f;

        static readonly Collider2D[] buffer = new Collider2D[16];
        float timer;

        public IInteractable Current { get; private set; }
        public event Action<IInteractable> CurrentChanged;

        void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = scanInterval;
            Scan();
        }

        void Scan()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = mask, useTriggers = true };
            int n = Physics2D.OverlapCircle(transform.position, radius, filter, buffer);
            IInteractable best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var it = buffer[i].GetComponentInParent<IInteractable>();
                if (it == null || !it.CanInteract(gameObject)) continue;
                float sq = ((Vector2)buffer[i].transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sq < bestSqr) { bestSqr = sq; best = it; }
            }
            if (!ReferenceEquals(best, Current))
            {
                Current = best;
                CurrentChanged?.Invoke(best);
            }
        }

        public bool TryInteract()
        {
            Scan();
            if (Current == null || !Current.CanInteract(gameObject)) return false;
            Current.Interact(gameObject);
            Scan();
            return true;
        }
    }
}
