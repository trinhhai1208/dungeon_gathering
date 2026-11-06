using DG.Tweening;
using DungeonGathering.Catalog;
using DungeonGathering.Core;
using DungeonGathering.Inventory;
using UnityEngine;

namespace DungeonGathering.Play
{
    /// <summary>
    /// Một prefab cho mọi vũ khí/trang bị rơi (ENTITIES §1.2).
    /// Chạm → RunBag.Add(id) MỘT lần duy nhất (cờ collected — R11)
    /// → EventBus<ItemPickedUpEvent>. Không popup, không thay đồ trong trận.
    /// Quản lý tái sử dụng qua PoolManager (IPoolable).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ItemPickup : MonoBehaviour, IPoolable
    {
        [Header("Renderers")]
        [SerializeField] SpriteRenderer icon;
        [SerializeField] SpriteRenderer rarityGlow;

        [Header("Audio")]
        [SerializeField] AudioClip pickupSfx;

        [Header("Settings")]
        [SerializeField] float pickupDelay = 0.4f;
        [Tooltip("Cạnh dài nhất của PHẦN CÓ HÌNH trên icon (world units). Nhân vật cao ~1.15, 1 ô = 1.")]
        [SerializeField] float iconMaxSize = 0.8f;
        [SerializeField] Color[] rarityColors =
        {
            new(0.98f, 0.45f, 0.09f), // S cam
            new(0.66f, 0.33f, 0.97f), // A tím
            new(0.23f, 0.51f, 0.96f), // B xanh dương
            new(0.13f, 0.77f, 0.37f), // C xanh lá
            new(0.42f, 0.45f, 0.50f), // F xám
        };

        ItemData item;
        bool collected;
        float readyAt;

        public ItemData Item => item;

        public void Init(ItemData data)
        {
            item = data;
            if (icon != null)
            {
                icon.sprite = data != null ? data.icon : null;
                // Co icon theo phần có hình (sheet kiếm 32px = 2 ô → quá to), canh tâm phần có hình vào tâm pickup
                float k = 1f;
                Vector2 contentOffset = Vector2.zero;
                if (icon.sprite != null)
                {
                    var content = UI.SpriteContentFit.ContentPixels(icon.sprite);
                    float longest = Mathf.Max(content.width, content.height) / icon.sprite.pixelsPerUnit;
                    if (longest > 0.01f) k = Mathf.Min(1f, iconMaxSize / longest);
                    contentOffset = (content.center - icon.sprite.pivot) / icon.sprite.pixelsPerUnit * k;
                }
                icon.transform.DOKill();
                icon.transform.localScale = new Vector3(k, k, 1f);
                float baseY = 0.2f - contentOffset.y;
                icon.transform.localPosition = new Vector3(-contentOffset.x, baseY, 0f);
                icon.transform.DOLocalMoveY(baseY + 0.15f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }

            if (rarityGlow != null && data != null && rarityColors != null && (int)data.rarity < rarityColors.Length)
            {
                rarityGlow.color = rarityColors[(int)data.rarity];
            }

            readyAt = Time.time + pickupDelay;
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            // Kiểm tra cờ đồng bộ trước tiên chống double pickup (R11)
            if (collected || item == null || Time.time < readyAt) return;

            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead) return;

            collected = true; // Đặt true đồng bộ NGAY LẬP TỨC trước khi tác động dữ liệu

            // 1. Thêm vào túi đồ run hiện tại
            if (player.Inventory != null) player.Inventory.AddToRunBag(item);
            else Services.Session?.AddToBag(item.id);







            // 4. Phát âm thanh nhặt đồ (nếu có)
            Sfx.PlayOverride(pickupSfx, item.rarity <= Rarity.A ? SfxId.RareItemPickup : SfxId.ItemPickup);

            // 5. Thu hồi về Pool tập trung
            if (Services.Pool != null)
                Services.Pool.Return(gameObject);
            else
                Destroy(gameObject);
        }

        public void OnSpawn()
        {
            collected = false;
            item = null;
            readyAt = 0f;
        }

        public void OnDespawn()
        {
            DOTween.Kill(transform);
            if (icon != null)
            {
                DOTween.Kill(icon.transform);
                icon.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                icon.sprite = null;
            }

            if (rarityGlow != null)
            {
                DOTween.Kill(rarityGlow.transform);
                rarityGlow.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            }
        }
    }
}
