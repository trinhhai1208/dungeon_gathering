using DG.Tweening;
using DungeonGathering.Core;
using TMPro;
using UnityEngine;

namespace DungeonGathering.Combat
{
    /// <summary>Số sát thương world-space (TMP), bay lên rồi tự trả pool.</summary>
    public sealed class DamagePopup : MonoBehaviour, IPoolable
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Color normalColor = Color.white;
        [SerializeField] Color critColor = new(1f, 0.75f, 0.2f);
        [SerializeField] Color hurtColor = new(1f, 0.3f, 0.3f);
        [SerializeField] float rise = 0.8f;
        [SerializeField] float lifetime = 0.6f;

        void Awake() { if (label == null) label = GetComponentInChildren<TMP_Text>(); }

        public void Show(float amount, bool crit, bool playerHurt)
        {
            if (label != null)
            {
                label.text = Mathf.CeilToInt(amount).ToString();
                label.color = playerHurt ? hurtColor : crit ? critColor : normalColor;
                label.alpha = 1f;
            }
            transform.localScale = Vector3.one * (crit ? 1.3f : 1f);
            transform.DOMoveY(transform.position.y + rise, lifetime).SetEase(Ease.OutQuad);
            if (label != null) DOTween.To(() => label.alpha, a => label.alpha = a, 0f, lifetime * 0.5f).SetDelay(lifetime * 0.5f).SetTarget(transform);
            Services.Pool.ReturnAfter(gameObject, lifetime);
        }

        public void OnSpawn() { }
        public void OnDespawn() => DOTween.Kill(transform);
    }
}
