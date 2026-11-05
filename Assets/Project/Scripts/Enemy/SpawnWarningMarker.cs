using DG.Tweening;
using DungeonGathering.Core;
using UnityEngine;

namespace DungeonGathering.Enemies
{
    /// <summary>Vòng cảnh báo trên sàn trước khi quái xuất hiện (pool). Nhấp nháy bằng DOTween.</summary>
    public sealed class SpawnWarningMarker : MonoBehaviour, IPoolable
    {
        [SerializeField] SpriteRenderer ring;
        [SerializeField] float pulseScale = 1.2f;

        void Awake() { if (ring == null) ring = GetComponentInChildren<SpriteRenderer>(); }

        public void OnSpawn()
        {
            transform.localScale = Vector3.one * 0.6f;
            Sfx.PlayAt(SfxId.SpawnWarning, transform.position);
            transform.DOScale(pulseScale, 0.25f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        public void OnDespawn()
        {
            DOTween.Kill(transform);
            transform.localScale = Vector3.one;
        }
    }
}
